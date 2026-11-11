using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace EnoughIron
{
    public sealed partial class MetalBowBehaviour : MonoBehaviour, IListenToPhysicalMaterial
    {
        internal const uint IronHash = 1;
        private static readonly FieldInfo tightness = AccessTools.Field(typeof(BowString), "tightnessMultiplier");

        private BowString bowString;
        private float ironDamage;
        private float damageFactor = 1f;

        internal static void ValidateSchema()
        {
            if (tightness == null || tightness.FieldType != typeof(float))
                throw new MissingFieldException(typeof(BowString).FullName, "tightnessMultiplier");
        }

        internal void Configure(BowString nativeString, PhysicalMaterial iron)
        {
            ConfigureAppearance(gameObject);
            bowString = nativeString;
            ironDamage = iron.DamageMultiplier;
            SetPhysicalMaterial(iron);
        }

        public void SetPhysicalMaterial(PhysicalMaterial material)
        {
            if (bowString == null || material == null)
                return;

            float ratio = material.DamageMultiplier / ironDamage;
            if (ratio <= 0f || float.IsNaN(ratio) || float.IsInfinity(ratio) || float.IsInfinity(1f / ratio))
                ratio = 1f;

            // Native HeldUpdate divides its pull response by this cached value.
            // Every material change derives a fresh value from the Iron baseline.
            tightness.SetValue(bowString, 1f / ratio);
            damageFactor = ratio;
            ApplyAppearance(material);
        }

        internal static float DamageFor(BowString nativeString)
        {
            if (nativeString == null || nativeString.Bow == null)
                return 1f;

            var behaviour = nativeString.Bow.GetComponent<MetalBowBehaviour>();
            return behaviour != null && behaviour.bowString == nativeString ? behaviour.damageFactor : 1f;
        }
    }
}
