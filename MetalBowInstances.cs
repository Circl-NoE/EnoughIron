using System;
using System.Reflection;
using Alta;
using Alta.Caves;
using Alta.Impact;
using Alta.Networking;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace EnoughIron
{
    internal static class MetalBowInstances
    {
        internal const uint PrefabHash = 25100;
        private const uint ItemHash = 64508;

        private static readonly FieldInfo entityBehaviours = Require<MonoBehaviour[]>(typeof(NetworkEntity), "networkEntityBehaviours");
        private static readonly FieldInfo partEntity = Require<NetworkEntity>(typeof(NetworkEntityBehaviour), "entity");
        private static readonly FieldInfo material = Require<PhysicalMaterial>(typeof(PhysicalMaterialPart), "physicalMaterial");
        private static readonly FieldInfo distribution = Require<Distribution>(typeof(PhysicalMaterialPart), "materialDistribution");
        private static readonly FieldInfo channel = Require<PhysicalMaterialChannel>(typeof(PhysicalMaterialPart), "materialChannel");
        private static readonly FieldInfo renderers = Require<Renderer[]>(typeof(PhysicalMaterialPart), "renderers");
        private static readonly FieldInfo impactors = Require<ImpactGenerator[]>(typeof(PhysicalMaterialPart), "impactors");
        private static readonly FieldInfo listeners = Require<MonoBehaviour[]>(typeof(PhysicalMaterialPart), "listenToPhysicalMaterial");

        private static PhysicalMaterial iron;
        private static Distribution metalDistribution;
        private static bool reportedInstallation;
        private static bool reportedFailure;

        // Validate the reflected fields before installing the spawn patch.
        static MetalBowInstances() { }

        internal static void Install(HarmonyLib.Harmony harmony)
        {
            MetalBowBehaviour.ValidateSchema();
            var initialize = AccessTools.Method(typeof(NetworkEntity), nameof(NetworkEntity.Initialize), new[]
            {
                typeof(INetworkScene), typeof(NetworkEntity), typeof(uint), typeof(uint[]), typeof(bool)
            });
            if (initialize == null)
                throw new MissingMethodException(typeof(NetworkEntity).FullName, nameof(NetworkEntity.Initialize));

            var generate = AccessTools.DeclaredMethod(typeof(PhysicalMaterialPart), "Generate", new[] { typeof(SpawnData) });
            if (generate == null)
                throw new MissingMethodException(typeof(PhysicalMaterialPart).FullName, "Generate");

            harmony.Patch(initialize, prefix: new HarmonyMethod(typeof(MetalBowInstances), nameof(BeforeInitialize)));
            harmony.Patch(generate, prefix: new HarmonyMethod(typeof(MetalBowInstances), nameof(BeforeMaterialGeneration)));
        }

        private static bool BeforeMaterialGeneration(PhysicalMaterialPart __instance, SpawnData __0)
        {
            // Manual spawns start as Iron. Loot with generation attributes uses
            // the native metal distribution, and spawn arguments can set a metal.
            return __0?.Attributes != null || __instance.GetComponent<MetalBowBehaviour>() == null;
        }

        private static void BeforeInitialize(NetworkEntity __instance)
        {
            if (__instance.IsInitialized || !IsStockBow(__instance))
                return;

            try
            {
                if (iron == null)
                    iron = HashedGeneralValue<PhysicalMaterial>.Get(MetalBowBehaviour.IronHash);
                if (metalDistribution == null)
                    metalDistribution = Resources.Load<Distribution>("distributions/Metal Material Distribution");
                AddMaterialPart(__instance, iron, metalDistribution);

                if (!reportedInstallation)
                {
                    reportedInstallation = true;
                    MelonLogger.Msg("ENOUGH_IRON_BOWS_READY prefab=25100 iron=1 damageAndDraw=materialRatio");
                }
            }
            catch (Exception exception)
            {
                if (!reportedFailure)
                {
                    reportedFailure = true;
                    MelonLogger.Error("ENOUGH_IRON_BOWS_SETUP_FAILED " + exception);
                }
            }
        }

        internal static bool IsStockBow(NetworkEntity entity)
        {
            var prefab = entity.Prefab;
            var pickup = entity.CommonPickup;
            return prefab != null && prefab.Entity == entity && prefab.Hash == PrefabHash
                && pickup != null && pickup.Item != null && pickup.Item.Hash == ItemHash;
        }

        internal static void AddMaterialPart(NetworkEntity entity, PhysicalMaterial defaultIron, Distribution metals)
        {
            var root = entity.gameObject;
            if (root.GetComponent<MetalBowBehaviour>() != null)
                return;
            if (root.GetComponent<PhysicalMaterialPart>() != null)
                throw new InvalidOperationException("A different mod already added a material part to this Metal Bow.");
            if (defaultIron == null || defaultIron.Hash != MetalBowBehaviour.IronHash || metals == null)
                throw new InvalidOperationException("Native Iron or Metal Material Distribution is missing.");
            float baseline = defaultIron.DamageMultiplier;
            if (baseline <= 0f || float.IsNaN(baseline) || float.IsInfinity(baseline))
                throw new InvalidOperationException("Native Iron damage is invalid.");

            var bow = root.GetComponent<Bow>();
            var stringNode = root.transform.Find("String");
            var bowString = stringNode != null ? stringNode.GetComponent<BowString>() : null;
            var originalBehaviours = entityBehaviours.GetValue(entity) as MonoBehaviour[];
            if (bow == null || bowString == null || bowString.Bow != bow || originalBehaviours == null)
                throw new InvalidOperationException("Native Metal Bow graph is incomplete.");

            var behaviour = root.AddComponent<MetalBowBehaviour>();
            PhysicalMaterialPart part = null;
            try
            {
                behaviour.Configure(bowString, defaultIron);
                part = root.AddComponent<PhysicalMaterialPart>();
                partEntity.SetValue(part, entity);
                material.SetValue(part, defaultIron);
                distribution.SetValue(part, metals);
                channel.SetValue(part, PhysicalMaterialChannel.A);
                renderers.SetValue(part, Array.Empty<Renderer>());
                impactors.SetValue(part, Array.Empty<ImpactGenerator>());
                listeners.SetValue(part, new MonoBehaviour[] { behaviour });

                // Keep every existing behaviour in order. The native material
                // part owns its save data and SyncMaterial message.
                var updated = new MonoBehaviour[originalBehaviours.Length + 1];
                Array.Copy(originalBehaviours, updated, originalBehaviours.Length);
                updated[updated.Length - 1] = part;
                entityBehaviours.SetValue(entity, updated);
            }
            catch
            {
                if (part != null)
                    UnityEngine.Object.Destroy(part);
                UnityEngine.Object.Destroy(behaviour);
                throw;
            }
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
