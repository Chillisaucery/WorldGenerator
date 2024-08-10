using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Triangulation;
using Unity.Burst.Intrinsics;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class MeshExporter : MonoBehaviour
{
    [SerializeField]
    MeshFilter _meshFilter;

    CurvatureAdjust _curvatureAdjust;
    Reconstruct _reconstruct;
    List<Vector3> _allPoints = new List<Vector3> ();



    private void OnEnable()
    {
        _curvatureAdjust = GetComponent<CurvatureAdjust>();
        _reconstruct = GetComponent<Reconstruct>();
        var _triangulator = GetComponent<Triangulator>();

        List<Vector3> innerPoints = GetUniquePointsFromLines(_triangulator.LinesToDraw);

        _allPoints.Clear();
        _allPoints.AddRange(_curvatureAdjust.Points);
        _allPoints.AddRange(_reconstruct.Mesh.vertices);
        _allPoints = _allPoints.Distinct().ToList();

        Mesh mesh = new Mesh();
        _meshFilter.mesh = mesh;
        mesh.vertices = _allPoints.ToArray();
    }

    List<Vector3> GetUniquePointsFromLines(List<(Vector3 v1, Vector3 v2)> lines)
    {
        HashSet<Vector3> uniquePointsSet = new HashSet<Vector3>();

        // Iterate through each line segment
        foreach (var line in lines)
        {
            // Add both points of the line segment to the HashSet
            uniquePointsSet.Add(line.v1);
            uniquePointsSet.Add(line.v2);
        }

        // Convert HashSet back to List
        List<Vector3> uniquePointsList = new List<Vector3>(uniquePointsSet);
        return uniquePointsList;
    }
}
