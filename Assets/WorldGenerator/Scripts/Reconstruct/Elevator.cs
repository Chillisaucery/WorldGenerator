using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class Elevator : MonoBehaviour
{
    Triangulator _triangulator;
    FillHole _fillHole;

    List<(Vector3 v1, Vector3 v2)> _linesToDraw = new List<(Vector3 v1, Vector3 v2)>();
    public List<(Vector3 v1, Vector3 v2)> LinesToDraw { get => _linesToDraw; }

    List<Vector3> _points = new List<Vector3>();
    List<Vector3> _meshPoints = new List<Vector3>();



    private void OnEnable()
    {
        _triangulator = GetComponent<Triangulator>();
        _fillHole = GetComponent<FillHole>();

        _linesToDraw = _triangulator.LinesToDraw;
        _points = _triangulator.Points;


        Mesh mesh = GetComponent<Reconstruct>().Mesh;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPosition = transform.TransformPoint(vertices[i]);

            if (Vector3.Distance(worldPosition, _fillHole.Center) <= _fillHole.AverageEdgeLength * 15)
            { 
                _meshPoints.Add(worldPosition);
            }
        }

        ElevateFirstRing();
    }

    private void ElevateFirstRing()
    {
        List<Vector3> boundaryPoints = _points.Intersect(_meshPoints).ToList();
        List<Vector3> innerPoints = _points.Except(boundaryPoints).ToList();
        List<Vector3> outsidePoints = _meshPoints.Except(_points).ToList();

        for (int i = 0; i < innerPoints.Count; i++) 
        {
            var point = innerPoints[i];

            var connectedEdges = _linesToDraw.Where(line => line.v1 == point || line.v2 == point).ToList();

            List<Vector3> connectedPoints = new List<Vector3> { };
            foreach (var edge in connectedEdges) 
            { 
                connectedPoints.Add(edge.v1);
                connectedPoints.Add(edge.v2);
            }

            connectedPoints = connectedPoints.Distinct().ToList();
            connectedPoints.Remove(point);
            connectedPoints = connectedPoints.Intersect(boundaryPoints).ToList();

            Vector3 connectedPCenter = AverageVector(connectedPoints);
            Vector3 nearestOutsidePoint = outsidePoints.OrderBy(p => Vector3.Distance(p, connectedPCenter)).ToList()[0];

            Vector3 supposedPos = connectedPCenter - nearestOutsidePoint;

            int pointIndex = _points.IndexOf(point);
            if (pointIndex >= 0)
            {
                _points[i] = Vector3.Lerp(point, supposedPos, 1f);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.yellow;
        for (int i = 0; i < _points.Count; i++)
        {
            Vector3 point = _points[i];
            Gizmos.DrawSphere(point, 0.01f);
        }
/*
        Gizmos.color = UnityEngine.Color.cyan;
        foreach (var line in _linesToDraw)
        {
            Gizmos.DrawLine(line.v1, line.v2);
        }
*/
    }

    private float TotalDistancesToPoints(Vector3 point, List<Vector3> otherPoints)
    {
        float sum = 0;

        foreach (var otherP in otherPoints)
        {
            sum += Vector3.Distance(point, otherP);
        }

        return sum;
    }

    Vector3 AverageVector(List<Vector3> vectors)
    {
        // Initialize sum of components
        float sumX = 0;
        float sumY = 0;
        float sumZ = 0;

        // Iterate through the list and accumulate component sums
        foreach (Vector3 vector in vectors)
        {
            sumX += vector.x;
            sumY += vector.y;
            sumZ += vector.z;
        }

        // Calculate the average by dividing each component sum by the count
        float count = vectors.Count;
        float averageX = sumX / count;
        float averageY = sumY / count;
        float averageZ = sumZ / count;

        // Create and return the average Vector3
        return new Vector3(averageX, averageY, averageZ);
    }
}
