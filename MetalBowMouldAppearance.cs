using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EnoughIron
{
    internal static class MetalBowMouldAppearance
    {
        // Sections of one limb in the installed Metal Bow LOD2 side profile.
        // Each row gives the distance along the bow and the near and far edges.
        private static readonly Vector3[] limbProfile =
        {
            new Vector3(0f, -.0189f, .0183f),
            new Vector3(.16f, -.0299f, .0302f),
            new Vector3(.26f, -.0169f, .0255f),
            new Vector3(.32f, .0039f, .0385f),
            new Vector3(.40f, .0486f, .0763f),
            new Vector3(.48f, .1079f, .1370f),
            new Vector3(.54f, .1673f, .1936f),
            new Vector3(.60f, .2150f, .2373f),
            new Vector3(.66f, .2514f, .2748f),
            new Vector3(.73f, .2778f, .3055f),
            new Vector3(.79f, .2710f, .3278f),
            new Vector3(.84f, .2582f, .3298f),
            new Vector3(.88f, .2593f, .3158f),
            new Vector3(.90f, .2906f, .3139f),
            new Vector3(.909f, .3046f, .3130f)
        };

        private const float GraphicScale = .175f;
        private const float ProfileCenter = .1475f;
        private const float ReliefBack = -.001f;

        internal static void Configure(MouldDefinition definition, GameObject stockBow)
        {
            if (ApplicationManager.IsHeadless)
                return;

            // The player build's native mesh is GPU-only. Build a single-submesh
            // relief from its side profile for the stock mould's graphic renderer.
            var nativeVisual = stockBow.transform.Find("Mesh/metal_bow_LOD2");
            var nativeMesh = nativeVisual != null ? nativeVisual.GetComponent<MeshFilter>() : null;
            if (nativeMesh == null || nativeMesh.sharedMesh == null)
                throw new InvalidOperationException("The native Metal Bow graphic source is missing.");

            var graphic = AccessTools.Field(typeof(MouldDefinition), "graphic");
            if (graphic == null || graphic.FieldType != typeof(Mesh))
                throw new MissingFieldException(typeof(MouldDefinition).FullName, "graphic");

            var outline = new Vector3[limbProfile.Length * 2 - 1];
            int middle = limbProfile.Length - 1;
            outline[middle] = limbProfile[0];
            for (int i = 1; i < limbProfile.Length; i++)
            {
                Vector3 section = limbProfile[i];
                outline[middle + i] = section;
                outline[middle - i] = new Vector3(-section.x, section.y, section.z);
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AddRelief(outline, .012f, vertices, triangles);
            AddRelief(new[]
            {
                new Vector3(-.88f, .300f, .318f),
                new Vector3(.88f, .300f, .318f)
            }, .008f, vertices, triangles);

            var uv = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertex = vertices[i];
                uv[i] = new Vector2(.5f + vertex.x / .35f, .5f + vertex.y / .075f);
            }

            var mesh = new Mesh { name = "Metal Bow mould relief" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            graphic.SetValue(definition, mesh);
        }

        private static void AddRelief(Vector3[] profile, float face, List<Vector3> vertices, List<int> triangles)
        {
            int first = vertices.Count;
            foreach (Vector3 section in profile)
            {
                float axis = section.x * GraphicScale;
                float near = (section.y - ProfileCenter) * GraphicScale;
                float far = (section.z - ProfileCenter) * GraphicScale;
                vertices.Add(new Vector3(axis, near, face));
                vertices.Add(new Vector3(axis, far, face));
                vertices.Add(new Vector3(axis, near, ReliefBack));
                vertices.Add(new Vector3(axis, far, ReliefBack));
            }

            AddQuad(triangles, first, first + 1, first + 3, first + 2);
            for (int i = 1; i < profile.Length; i++)
            {
                int a = first + (i - 1) * 4;
                int b = a + 4;
                AddQuad(triangles, a, b, b + 1, a + 1);
                AddQuad(triangles, a + 2, a + 3, b + 3, b + 2);
                AddQuad(triangles, a, a + 2, b + 2, b);
                AddQuad(triangles, a + 1, b + 1, b + 3, a + 3);
            }
            int last = vertices.Count - 4;
            AddQuad(triangles, last, last + 2, last + 3, last + 1);
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }
    }
}
