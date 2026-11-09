using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Alta.Impact;
using Alta.Inventory;
using Alta.Networking;
using HarmonyLib;
using UnityEngine;

namespace EnoughIron
{
    internal static class BowShotDamage
    {
        private readonly struct Launch
        {
            private readonly Arrow primary;
            private readonly List<ExtraArrowUpdater.ExtraArrowData> extras;
            private readonly float factor;

            internal Launch(Arrow primary, List<ExtraArrowUpdater.ExtraArrowData> extras, float factor)
            {
                this.primary = primary;
                this.extras = extras;
                this.factor = factor;
            }

            internal float FactorFor(IShootable projectile)
            {
                if (!(factor > 0f) || factor == 1f || float.IsInfinity(factor)) return 1f;
                if (ReferenceEquals(primary, projectile)) return factor;
                if (extras != null)
                {
                    foreach (var extra in extras)
                    {
                        if (ReferenceEquals(extra.Arrow, projectile)) return factor;
                    }
                }
                return 1f;
            }
        }

        private sealed class Flight
        {
            private readonly ImpactTool tool;
            private readonly Pickup pickup;
            internal readonly float Factor;

            internal Flight(ImpactTool tool, Pickup pickup, float factor)
            {
                this.tool = tool;
                this.pickup = pickup;
                Factor = factor;
                if (pickup != null)
                {
                    pickup.PickupGrabbed += Grabbed;
                    pickup.PreDocked += Docking;
                }
            }

            internal void Remove()
            {
                if (flights.TryGetValue(tool, out var current) && ReferenceEquals(current, this)) flights.Remove(tool);
                if (pickup != null)
                {
                    pickup.PickupGrabbed -= Grabbed;
                    pickup.PreDocked -= Docking;
                }
            }

            private void Grabbed(Pickup item, Interactor interactor) => Remove();
            private void Docking(Interactor interactor, Pickup item, PickupDock dock) => Remove();
        }

        private static readonly ConditionalWeakTable<ImpactTool, Flight> flights = new ConditionalWeakTable<ImpactTool, Flight>();

        [ThreadStatic]
        private static Launch launch;

        internal static void Install(HarmonyLib.Harmony harmony)
        {
            var close = PatchMethod(nameof(CloseLaunch), Priority.Last);
            harmony.Patch(Require(AccessTools.DeclaredMethod(typeof(Shooter), nameof(Shooter.Shoot),
                new[] { typeof(Pickup), typeof(float), typeof(bool), typeof(Vector3) }), "Shooter.Shoot"),
                prefix: PatchMethod(nameof(OpenPrimary), Priority.First), finalizer: close);
            harmony.Patch(Require(AccessTools.DeclaredMethod(typeof(ExtraArrowUpdater), "ShootExtraArrows",
                new[] { typeof(BowString), typeof(NetworkEntity), typeof(Vector3) }), "ExtraArrowUpdater.ShootExtraArrows"),
                prefix: PatchMethod(nameof(OpenExtras), Priority.First), finalizer: close);
            harmony.Patch(Require(AccessTools.DeclaredMethod(typeof(Arrow), nameof(Arrow.ShootArrowWithVelocity),
                new[] { typeof(Vector3) }), "Arrow.ShootArrowWithVelocity"), prefix: PatchMethod(nameof(LaunchArrow), Priority.First));
            harmony.Patch(Require(AccessTools.DeclaredMethod(typeof(Projectile), nameof(Projectile.Shoot),
                new[] { typeof(Vector3), typeof(ColliderGroup) }), "Projectile.Shoot(Vector3, ColliderGroup)"),
                prefix: PatchMethod(nameof(LaunchProjectile), Priority.First));

            // GenerateHit/GetFullTriggerHit, GenerateImpactOnDetector, and the trigger fallback each use a separate constructor.
            PatchHit(harmony, nameof(ScaleArgumentSix), typeof(ImpactTool), typeof(ImpactGenerator), typeof(ImpactDetector),
                typeof(ContactPointStructure), typeof(Vector3), typeof(Vector3), typeof(float), typeof(float), typeof(ImpactChannel));
            PatchHit(harmony, nameof(ScaleArgumentThree), typeof(ImpactTool), typeof(ImpactGenerator), typeof(ImpactDetector),
                typeof(float), typeof(float), typeof(ImpactChannel), typeof(Vector3), typeof(Vector3));
            PatchHit(harmony, nameof(ScaleArgumentSix), typeof(ImpactTool), typeof(ImpactType), typeof(ImpactDetector),
                typeof(ContactPointStructure), typeof(float), typeof(float), typeof(float), typeof(float));
        }

        private static MethodBase Require(MethodBase method, string name) => method ?? throw new MissingMethodException(name);

        private static HarmonyMethod PatchMethod(string name, int priority) =>
            new HarmonyMethod(AccessTools.DeclaredMethod(typeof(BowShotDamage), name)) { priority = priority };

        private static void PatchHit(HarmonyLib.Harmony harmony, string prefix, params Type[] parameters) =>
            harmony.Patch(Require(AccessTools.DeclaredConstructor(typeof(ImpactHitWithTool), parameters), "ImpactHitWithTool constructor"),
                prefix: PatchMethod(prefix, Priority.First));

        private static void OpenPrimary(Shooter __instance, Pickup __0, out Launch? __state)
        {
            __state = launch;
            var arrow = __0 == null ? null : __0.GetComponent<Arrow>();
            launch = arrow == null ? default : new Launch(arrow, null, MetalBowBehaviour.DamageFor(__instance.GetComponent<BowString>()));
        }

        private static void OpenExtras(ExtraArrowUpdater __instance, BowString __0, out Launch? __state)
        {
            __state = launch;
            launch = new Launch(null, __instance.SpawnedExtraArrows, MetalBowBehaviour.DamageFor(__0));
        }

        private static void CloseLaunch(Launch? __state)
        {
            if (__state.HasValue) launch = __state.Value;
        }

        private static void LaunchArrow(Arrow __instance) => Record(__instance.ImpactTool, launch.FactorFor(__instance));

        private static void LaunchProjectile(Projectile __instance, bool ___isFired)
        {
            if (___isFired) return;
            // ExtraArrowUpdater.CreateExtraArrow looks up this same per-projectile ImpactTool.
            Record(__instance.GetComponent<ImpactTool>(), launch.FactorFor(__instance));
        }

        private static void Record(ImpactTool tool, float factor)
        {
            if (tool == null) return;
            if (flights.TryGetValue(tool, out var previous)) previous.Remove();
            if (factor != 1f) flights.Add(tool, new Flight(tool, tool.Pickup, factor));
        }

        private static void ScaleArgumentSix(ImpactTool __0, ref float __6) => Scale(__0, ref __6);
        private static void ScaleArgumentThree(ImpactTool __0, ref float __3) => Scale(__0, ref __3);

        private static void Scale(ImpactTool tool, ref float multipliers)
        {
            // BowDrawSkill overwrites OneOffMultiplier and GenerateHit resets it. Keep this factor independent.
            // Arrow can stop its flight on collision before DelayedImpactResolver or SyncImpact constructs the hit.
            if (tool != null && flights.TryGetValue(tool, out var flight)) multipliers *= flight.Factor;
        }
    }
}
