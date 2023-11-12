using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;



[ExecuteInEditMode]
public class MeshGenerator : MonoBehaviour
{
    public Material material;
    FillHole _fillHole = null;
    public List<List<Vector3>> triangleVerticesList = new List<List<Vector3>>();



    void OnEnable()
    {
        if (_fillHole == null)
        {
            _fillHole = GetComponent<FillHole>();
        }

        triangleVerticesList = GetTriangles(_fillHole.GetAllPoints());

        // Destroy any existing game objects with the name "GeneratedMesh" to avoid duplicates
        GameObject[] existingMeshObjects = GameObject.FindGameObjectsWithTag("GeneratedMesh");
        foreach (var existingMeshObject in existingMeshObjects)
        {
            DestroyImmediate(existingMeshObject);
        }

        foreach (var triangleVertices in triangleVerticesList)
        {
            // Create a new empty GameObject with MeshFilter and MeshRenderer
            GameObject meshObject = new GameObject("GeneratedMesh");
            meshObject.tag = "GeneratedMesh"; // To identify these game objects
            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

            // Create a new empty mesh
            Mesh mesh = new Mesh();

            // Create vertices, normals, and UVs
            Vector3[] vertices = triangleVertices.ToArray();
            Vector3[] normals = new Vector3[3]; // Assuming all normals point up
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.up;
            }
            Vector2[] uvs = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) };

            // Assign vertices, normals, and UVs to the mesh
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;

            // Create triangles
            int[] triangles = { 0, 1, 2 };

            // Assign triangles to the mesh
            mesh.triangles = triangles;

            // Assign the mesh to the MeshFilter
            meshFilter.mesh = mesh;

            // Set a material for rendering
            meshRenderer.material = material;

            // Set up rendering
            mesh.RecalculateBounds();
            mesh.Optimize();

            // Set the meshObject's position, rotation, and scale
            meshObject.transform.position = Vector3.zero;
            meshObject.transform.rotation = Quaternion.identity;
            meshObject.transform.localScale = Vector3.one;

            meshObject.transform.parent = this.transform;
        }
    }

    private List<List<Vector3>> GetTriangles(List<(Vector3 v1, Vector3 v2)> linesToDraw)
    {
        throw new NotImplementedException();
    }

    private List<List<Vector3>> GetTriangles(List<Vector3> points)
    {
        List<Triangle> triangles = new List<Triangle>();

        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i+1; j < points.Count; j++)
            {
                for (int k = j+1; k < points.Count; k++)
                {
                    Triangle t = new Triangle();

                    t.v1 = points[i];
                    t.v2 = points[j];
                    t.v3 = points[k];

                    float perimeter = Vector3.Distance(t.v1, t.v2) + Vector3.Distance(t.v1, t.v3) + Vector3.Distance(t.v2, t.v3);

                    if (perimeter > Vector3.Distance(t.v1, t.v2) * 4 ||
                        perimeter > Vector3.Distance(t.v2, t.v3) * 4 ||
                        perimeter > Vector3.Distance(t.v1, t.v3) * 4)
                        continue;

                    if (!triangles.Any(tri => tri.Equals(t)))
                        triangles.Add(t);
                }
            }
        }

        List<List<Vector3>> results = new List<List<Vector3>>();

        foreach (Triangle t in triangles)
            results.Add(new List<Vector3>() { t.v1, t.v2, t.v3 });

        return results;
    }



    public class Triangle
    {
        public Vector3 v1, v2, v3;

        public bool Equals(Triangle f)
        {
            return 
                (f.v1 == v1 && f.v2 == v2 && f.v3 == v3) ||
                (f.v1 == v1 && f.v2 == v3 && f.v3 == v2) ||

                (f.v1 == v2 && f.v2 == v1 && f.v3 == v3) ||
                (f.v1 == v2 && f.v2 == v3 && f.v3 == v1) ||

                (f.v1 == v3 && f.v2 == v2 && f.v3 == v1) ||
                (f.v1 == v3 && f.v2 == v1 && f.v3 == v2);
        }
    }
}
