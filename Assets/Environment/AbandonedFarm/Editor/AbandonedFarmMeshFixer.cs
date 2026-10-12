using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Some meshes in the Abandoned Farm model were exported inside-out (faces and normals point
// inward), so back-face culling hides the side facing the camera, e.g. the windmill roof.
// Flip them when the model is imported so the fix survives reimports.
class AbandonedFarmMeshFixer : AssetPostprocessor
{
    const string ModelPath = "Assets/Environment/AbandonedFarm/Models/Aband1.1.fbx";

    // "globaltrees" is also inward-facing, but on purpose: it's the backdrop ring facing the map.
    static readonly HashSet<string> InsideOutMeshes = new HashSet<string>
    {
        // Windmill roof, roof cap and window grating
        "Circle.021", "Circle.024", "Circle.026",
        // Props
        "Cantaloupe.002", "Cantaloupe.003", "Cantaloupe.004",
        "Radio_01.002",
        "Soap_powder.001", "Soap_powder_01.001", "Soap_powder_02.001",
        "Soda_02.002", "Soda_02.003",
        "Soda_07.001", "Soda_07.002", "Soda_07.003", "Soda_07.004", "Soda_07.005", "Soda_07.006", "Soda_07.007",
        "Soda_07.008", "Soda_07.009", "Soda_07.010", "Soda_07.011", "Soda_07.012", "Soda_07.013", "Soda_07.014",
        "Soda_13.002", "Soda_13.003",
        "B_03.002", "B_03.003",
    };

    public override uint GetVersion() => 1;

    void OnPostprocessModel(GameObject root)
    {
        if (assetPath != ModelPath)
            return;

        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = filter.sharedMesh;
            if (mesh != null && InsideOutMeshes.Contains(mesh.name))
                Flip(mesh);
        }
    }

    static void Flip(Mesh mesh)
    {
        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
        {
            var triangles = mesh.GetTriangles(subMesh);
            for (int i = 0; i < triangles.Length; i += 3)
                (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
            mesh.SetTriangles(triangles, subMesh, false);
        }

        var normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
            normals[i] = -normals[i];
        mesh.normals = normals;

        if (mesh.tangents.Length > 0)
            mesh.RecalculateTangents();
    }
}
