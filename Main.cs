using Alta;
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

            GameObject BigMetal = (GameObject)Resources.Load("network prefabs/crafting/handles/Handle Large Cool");

            MelonLogger.Msg("Got game object");

            PhysicalMaterialPart part = BigMetal.AddComponent<PhysicalMaterialPart>();

            MelonLogger.Msg("Added physicalmaterialpart");

            part.physicalMaterial = HashedGeneralValue<PhysicalMaterial>.Get(1u);

            MelonLogger.Msg("Set default material to iron");

            part.materialChannel = PhysicalMaterialChannel.A;

            MelonLogger.Msg("Set material channel to A");

            MeshRenderer[] renders = BigMetal.GetComponentsInChildren<MeshRenderer>().Where(c =>  c.gameObject.name.Contains("metal_handle") && !c.gameObject.name.Contains("wrap")).ToArray();

            MelonLogger.Msg("Got list of renderers");

            part.renderers = renders;

            MelonLogger.Msg("Applied renderers");

            NetworkEntity entity = BigMetal.GetComponent<NetworkEntity>();
            List<MonoBehaviour> behaviours = entity.networkEntityBehaviours.ToList();
            behaviours.Add(part);
            entity.networkEntityBehaviours = behaviours.ToArray();

            part.entity = entity;

            MelonLogger.Msg("Success");
        }
    }
}
