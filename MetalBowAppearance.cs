using System;
using UnityEngine;

namespace EnoughIron
{
    public sealed partial class MetalBowBehaviour
    {
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private int[] bodySlots;
        [SerializeField] private Material[] originalBodyMaterials;

        private void ConfigureAppearance(GameObject root)
        {
            if (ApplicationManager.IsHeadless)
                return;

            bodyRenderers = new Renderer[5];
            bodySlots = new int[bodyRenderers.Length];
            originalBodyMaterials = new Material[bodyRenderers.Length];

            BindBodySlot<SkinnedMeshRenderer>(root, 0, "Mesh/metal_boy_body_LOD0", 0);
            BindBodySlot<SkinnedMeshRenderer>(root, 1, "Mesh/metal_bow_worn_grp/metal_bow_body_worn_LOD0", 0);
            BindBodySlot<SkinnedMeshRenderer>(root, 2, "Mesh/metal_bow_damaged_grp/metal_bow_body_damaged_LOD0", 0);
            BindBodySlot<SkinnedMeshRenderer>(root, 3, "Mesh/metal_bow_LOD1", 1);
            BindBodySlot<MeshRenderer>(root, 4, "Mesh/metal_bow_LOD2", 1);
        }

        private void BindBodySlot<TRenderer>(GameObject root, int index, string path, int slot)
            where TRenderer : Renderer
        {
            Transform target = root.transform.Find(path);
            TRenderer renderer = target != null ? target.GetComponent<TRenderer>() : null;
            if (renderer == null)
                throw new InvalidOperationException($"Metal Bow body renderer is missing or has a different type at {path}.");

            Material[] materials = renderer.sharedMaterials;
            if (materials.Length != slot + 1 || materials[slot] == null || materials[slot].name != "IronNonForging"
                || (slot == 1 && (materials[0] == null || materials[0].name != "Wooden_Atlas_m"))
                || (index > 0 && materials[slot] != originalBodyMaterials[0]))
            {
                throw new InvalidOperationException($"Metal Bow body material layout is unexpected at {path}.");
            }

            bodyRenderers[index] = renderer;
            bodySlots[index] = slot;
            originalBodyMaterials[index] = materials[slot];
        }

        private void ApplyAppearance(PhysicalMaterial material)
        {
            if (bodyRenderers == null || bodySlots == null || originalBodyMaterials == null
                || bodyRenderers.Length != bodySlots.Length || bodyRenderers.Length != originalBodyMaterials.Length)
            {
                return;
            }

            Material replacement = GetBodyMaterial(material);
            for (int index = 0; index < bodyRenderers.Length; index++)
            {
                Renderer renderer = bodyRenderers[index];
                Material visual = replacement != null ? replacement : originalBodyMaterials[index];
                if (renderer == null || visual == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                int slot = bodySlots[index];
                if (slot < 0 || slot >= materials.Length || materials[slot] == visual)
                    continue;

                materials[slot] = visual;
                renderer.sharedMaterials = materials;
            }
        }

        private static Material GetBodyMaterial(PhysicalMaterial material)
        {
            if (material == null || material.Hash == IronHash)
                return null;

            try
            {
                // The mixed LOD meshes share UVs with the wraps. Use the metal's
                // standalone shader material so the original UVs stay intact.
                Material visual = material.GetNonAtlasedNonDeformingMaterial(PhysicalMaterialChannel.A);
                Shader shader = visual != null ? visual.shader : null;
                return shader != null && shader.isSupported && shader.name != "Hidden/InternalErrorShader" ? visual : null;
            }
            catch (Exception)
            {
                // A material with incomplete channels falls back to the original body.
                return null;
            }
        }
    }
}
