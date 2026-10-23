using Alta;
using Alta.Blacksmithing;
using Alta.Networking;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

[assembly : MelonInfo(typeof(EnoughIron.Main), "Enough Iron!", "0.0.1", "Circl")]

namespace EnoughIron
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Trying to make handles better lol");

            ChangeHandle((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Large Cool"));
            ChangeHandle((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Medium Cool"));
            ChangeHandle((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Short Cool"));
            ChangeHandle((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Fist"));
            ChangeHandle((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Medium Curved"));
            ChangeHandle((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Short C Curve"));
            ChangeHandle((GameObject)Resources.Load("network prefabs/crafting/handles/Handle Short Pointy End"));

            MelonLogger.Msg("Success");
        }

        internal static void ChangeHandle(GameObject handle)
        {
            PhysicalMaterialPart part = handle.AddComponent<PhysicalMaterialPart>();

            part.physicalMaterial = HashedGeneralValue<PhysicalMaterial>.Get(1u);

            part.materialChannel = PhysicalMaterialChannel.A;

            MeshRenderer[] renders = handle.GetComponentsInChildren<MeshRenderer>().Where(c => c.gameObject.name.Contains("_handle") && !c.gameObject.name.Contains("wrap") && !c.gameObject.name.Contains("grab")).ToArray();

            part.renderers = renders;

            NetworkEntity entity = handle.GetComponent<NetworkEntity>();
            List<MonoBehaviour> behaviours = entity.networkEntityBehaviours.ToList();
            behaviours.Add(part);
            entity.networkEntityBehaviours = behaviours.ToArray();

            part.entity = entity;

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
