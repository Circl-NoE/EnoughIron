using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alta;
using Alta.Inventory;
using Alta.Networking;
using HarmonyLib;
using UnityEngine;

namespace EnoughIron
{
    internal static class MetalBowMoulds
    {
        // This ID is stored in world saves and must stay stable across releases.
        internal const uint DefinitionHash = 0x4549424D;
        internal const int IngotCost = 8;

        private const uint BowItemHash = 64508;
        private const uint StandardPressHash = 13362;
        private const uint NativeMetalMouldHash = 22952;
        private const string DefinitionName = "Metal Bow Mould";
        private const string DefinitionNameKey = "metalbowmould";

        private static readonly FieldInfo hashField = Require<int>(typeof(HashedGeneralValue), "hash");
        private static readonly FieldInfo hashIndex = Require<Dictionary<uint, MouldDefinition>>(typeof(HashedGeneralValue<MouldDefinition>), "items");
        private static readonly FieldInfo nameIndex = Require<Dictionary<string, MouldDefinition>>(typeof(HashedGeneralValue<MouldDefinition>), "itemsByName");
        private static readonly FieldInfo productIndex = Require<Dictionary<Item, MouldDefinition>>(typeof(MouldDefinition), "definitions");
        private static readonly FieldInfo productField = Require<Item>(typeof(MouldDefinition), "product");
        private static readonly FieldInfo findableField = Require<bool>(typeof(MouldDefinition), "isFindable");
        private static readonly FieldInfo allowedMaterialsField = Require<ItemSet>(typeof(MouldDefinition), "allowedMaterials");
        private static readonly FieldInfo costField = Require<int>(typeof(MouldDefinition), "cost");
        private static readonly FieldInfo quantityField = Require<int>(typeof(MouldDefinition), "quantityProduced");
        private static readonly FieldInfo componentsField = Require<List<ItemComponent>>(typeof(Item), "components");
        private static readonly FieldInfo definitionField = Require<MouldDefinition>(typeof(MouldItemComponent), "definition");
        private static readonly FieldInfo componentMaterialsField = Require<List<PhysicalMaterial>>(typeof(ItemComponent), "physicalMaterials");
        private static readonly FieldInfo inputDockField = Require<PickupDock>(typeof(MouldPress), "weaponPartDock");
        private static readonly FieldInfo settingsField = Require<PickupDockSettings>(typeof(PickupDock), "settings");

        private static Item bowItem;

        // Validate the reflected game schema before changing the registries.
        static MetalBowMoulds() { }

        internal static void Install(HarmonyLib.Harmony harmony)
        {
            if (bowItem != null)
                return;

            var bow = Item.Get(BowItemHash);
            var template = MouldDefinition.Get(NativeMetalMouldHash);
            if (bow == null || template == null || template.AllowedMaterials == null || template.AllowedMaterials.Items.Count == 0)
                throw new InvalidOperationException("Native Metal Bow or mould ingot set is missing.");

            var byHash = (Dictionary<uint, MouldDefinition>)hashIndex.GetValue(null);
            var byName = (Dictionary<string, MouldDefinition>)nameIndex.GetValue(null);
            var byProduct = (Dictionary<Item, MouldDefinition>)productIndex.GetValue(null);
            var components = (List<ItemComponent>)componentsField.GetValue(bow);
            if (byHash.ContainsKey(DefinitionHash) || byName?.ContainsKey(DefinitionNameKey) == true
                || byProduct?.ContainsKey(bow) == true || components.OfType<MouldItemComponent>().Any()
                || byHash.Values.Any(existing => existing != null && (existing.Product == bow
                    || string.Equals(existing.name.Replace(" ", ""), DefinitionNameKey, StringComparison.OrdinalIgnoreCase))))
                throw new InvalidOperationException("Another mould already uses the Metal Bow, its mould name, or its mould hash.");

            var initialize = AccessTools.DeclaredMethod(typeof(MouldPress), nameof(MouldPress.Initialize), Type.EmptyTypes);
            if (initialize == null)
                throw new MissingMethodException(typeof(MouldPress).FullName, nameof(MouldPress.Initialize));

            MouldDefinition definition = null;
            MouldItemComponent component = null;
            try
            {
                definition = ScriptableObject.CreateInstance<MouldDefinition>();
                definition.name = DefinitionName;
                hashField.SetValue(definition, (int)DefinitionHash);
                productField.SetValue(definition, bow);
                allowedMaterialsField.SetValue(definition, template.AllowedMaterials);
                findableField.SetValue(definition, false);
                costField.SetValue(definition, IngotCost);
                quantityField.SetValue(definition, 1);
#if !SERVER
                if (!ApplicationManager.IsHeadless)
                {
                    var stockBow = Resources.Load<GameObject>("Network Prefabs/Props/Combat/Metal Bow");
                    if (stockBow == null || stockBow.GetComponent<NetworkPrefab>()?.Hash != MetalBowInstances.PrefabHash)
                        throw new InvalidOperationException("Native Metal Bow graphic source is missing.");
                    MetalBowMouldAppearance.Configure(definition, stockBow);
                }
#endif

                component = ScriptableObject.CreateInstance<MouldItemComponent>();
                component.name = "Metal Bow Mould Item Component";
                definitionField.SetValue(component, definition);
                componentMaterialsField.SetValue(component, new List<PhysicalMaterial>());

                // Keep the native hash, name, and product lookups consistent. An
                // unbuilt cache will discover this definition on first use.
                byHash.Add(DefinitionHash, definition);
                byName?.Add(DefinitionNameKey, definition);
                byProduct?.Add(bow, definition);
                components.Add(component);

                harmony.Patch(initialize, prefix: new HarmonyMethod(typeof(MetalBowMoulds), nameof(BeforePressInitialize)));
                bowItem = bow;
            }
            catch
            {
                RemoveIfOwned(byHash, DefinitionHash, definition);
                RemoveIfOwned(byName, DefinitionNameKey, definition);
                RemoveIfOwned(byProduct, bow, definition);
                if (component != null)
                {
                    components.Remove(component);
                    UnityEngine.Object.Destroy(component);
                }
                if (definition != null)
                {
                    if (definition.Graphic != null)
                        UnityEngine.Object.Destroy(definition.Graphic);
                    UnityEngine.Object.Destroy(definition);
                }
                throw;
            }
        }

        private static void BeforePressInitialize(MouldPress __instance)
        {
            var prefab = __instance.Entity?.Prefab;
            if (bowItem == null || prefab == null || prefab.Hash != StandardPressHash)
                return;

            var dock = (PickupDock)inputDockField.GetValue(__instance);
            var original = dock?.Settings;
            if (original == null || original.IncludedItems.Contains(bowItem))
                return;

            // The settings asset may be shared. Give only this press input a
            // copy, preserving all its other native filters and snap settings.
            var settings = UnityEngine.Object.Instantiate(original);
            settings.IncludedItems.Add(bowItem);
            settingsField.SetValue(dock, settings);
        }

        private static void RemoveIfOwned<TKey>(Dictionary<TKey, MouldDefinition> index, TKey key, MouldDefinition definition)
        {
            if (index != null && index.TryGetValue(key, out var current) && ReferenceEquals(current, definition))
                index.Remove(key);
        }

        private static FieldInfo Require<T>(Type type, string name)
        {
            var field = AccessTools.Field(type, name);
            if (field == null || field.FieldType != typeof(T))
                throw new MissingFieldException(type.FullName, name);
            return field;
        }
    }
}
