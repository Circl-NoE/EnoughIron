using Il2CppAlta;
using Il2CppAlta.Blacksmithing;
using Il2CppAlta.Caves;
using Il2CppAlta.Networking;
using MelonLoader;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[assembly : MelonInfo(typeof(EnoughIron.Main), "Enough Iron!", "1.0.0-Q", "Circl")]

namespace EnoughIron
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Giving the game an iron deficiency");

            ChangeItem(Resources.Load<GameObject>("network prefabs/crafting/handles/Handle Large Cool"), "_handle", "wrap");
            ChangeItem(Resources.Load<GameObject>("network prefabs/crafting/handles/Handle Medium Cool"), "_handle", "wrap");
            ChangeItem(Resources.Load<GameObject>("network prefabs/crafting/handles/Handle Short Cool"), "_handle", "wrap");
            ChangeItem(Resources.Load<GameObject>("network prefabs/crafting/handles/Handle Fist"), "_handle", "grab");
            ChangeItem(Resources.Load<GameObject>("network prefabs/crafting/handles/Handle Medium Curved"), "_handle", "wrap");
            ChangeItem(Resources.Load<GameObject>("network prefabs/crafting/handles/Handle Short C Curve"), "_handle", "wrap");
            ChangeItem(Resources.Load<GameObject>("network prefabs/crafting/handles/Handle Short Pointy End"), "_handle", "wrap");

            ChangeItem(Resources.Load<GameObject>("network prefabs/props/combat/Shield"), "_metal", "wraps", false);

            ChangeItem(Resources.Load<GameObject>("network prefabs/props/combat/Small Shield"), "_metal", "wraps", false);

            MelonLogger.Msg("Success");
        }

        internal static void ChangeItem(GameObject handle, string include, string exclude, bool includeMelt = true)
        {
            MelonLogger.Msg($"Modifying {handle.name}");
            Il2Cpp.PhysicalMaterialPart part = handle.AddComponent<Il2Cpp.PhysicalMaterialPart>();

            part.physicalMaterial = HashedGeneralValue<Il2Cpp.PhysicalMaterial>.Get(1u);

            part.materialChannel = Il2Cpp.PhysicalMaterialChannel.A;

            MeshRenderer[] renders = handle.GetComponentsInChildren<MeshRenderer>().Where(c => c.gameObject.name.Contains(include) && !c.gameObject.name.Contains(exclude)).ToArray();

            part.renderers = renders;

            NetworkEntity entity = handle.GetComponent<NetworkEntity>();
            List<MonoBehaviour> behaviours = entity.networkEntityBehaviours.ToList();
            behaviours.Add(part);
            entity.networkEntityBehaviours = behaviours.ToArray();

            part.entity = entity;

            part.materialDistribution = Resources.Load<Distribution>("distributions/Metal Material Distribution");

            if (includeMelt)
            {
                BasicMeltingModel basicMelt = handle.GetComponent<BasicMeltingModel>();
                MeltingModelPart melt = handle.AddComponent<MeltingModelPart>();

                melt.entity = entity;
                melt.androidMeshFilter = basicMelt.androidMeshFilter;
                melt.heatPointCollection = basicMelt.heatPointCollection;
                melt.modifiers = basicMelt.modifiers;
                melt.mainMeshFilter = basicMelt.mainMeshFilter;
                melt.ingotValue = basicMelt.ingotValue;
                melt.istriggeringAbilities = basicMelt.istriggeringAbilities;

                melt.physicalMaterialPart = part;

                MeltingHeatPoint heatPoint = handle.GetComponentInChildren<MeltingHeatPoint>();

                heatPoint.meltingModel = melt;

                GameObject.Destroy(basicMelt);
            }
        }
    }
}
