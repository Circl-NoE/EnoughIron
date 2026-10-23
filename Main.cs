using Alta;
using Alta.Blacksmithing;
using Alta.Caves;
using Alta.Networking;
using MelonLoader;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[assembly : MelonInfo(typeof(EnoughIron.Main), "Enough Iron!", "1.0.0", "Circl")]

namespace EnoughIron
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Giving the game an iron deficiency");

            ChangeItem((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Large Cool"), "_handle", "wrap");
            ChangeItem((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Medium Cool"), "_handle", "wrap");
            ChangeItem((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Short Cool"), "_handle", "wrap");
            ChangeItem((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Fist"), "_handle", "grab");
            ChangeItem((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Medium Curved"), "_handle", "wrap");
            ChangeItem((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Short C Curve"), "_handle", "wrap");
            ChangeItem((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Short Pointy End"), "_handle", "wrap");

            ChangeItem((GameObject)Resources.Load("network prefabs/props/combat/Shield"), "_metal", "wraps", false);

            ChangeItem((GameObject)Resources.Load("network prefabs/props/combat/Small Shield"), "_metal", "wraps", false);

            MelonLogger.Msg("Success");
        }

        internal static void ChangeItem(GameObject handle, string include, string exclude, bool includeMelt = true)
        {
            PhysicalMaterialPart part = handle.AddComponent<PhysicalMaterialPart>();

            part.physicalMaterial = HashedGeneralValue<PhysicalMaterial>.Get(1u);

            part.materialChannel = PhysicalMaterialChannel.A;

            MeshRenderer[] renders = handle.GetComponentsInChildren<MeshRenderer>().Where(c => c.gameObject.name.Contains(include) && !c.gameObject.name.Contains(exclude)).ToArray();

            part.renderers = renders;

            NetworkEntity entity = handle.GetComponent<NetworkEntity>();
            List<MonoBehaviour> behaviours = entity.networkEntityBehaviours.ToList();
            behaviours.Add(part);
            entity.networkEntityBehaviours = behaviours.ToArray();

            part.entity = entity;

            part.materialDistribution = (Distribution)Resources.Load("distributions/Metal Material Distribution");

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
