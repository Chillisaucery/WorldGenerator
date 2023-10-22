using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Reconstruct;
using Edge = Reconstruct.Edge;

[ExecuteInEditMode]
public class FillHole : MonoBehaviour
{
    [SerializeField] float _maximumParimeter = 10;
    [SerializeField] [Range(0,1)] float _advancingStrength = 0.5f;
    Reconstruct _reconstruct = null;
    List<(Vector3 coord, int index)> _points = new List<(Vector3 coord, int index)>();
    List<Vector3> _innerPoints = new List<Vector3>();
    float _averageRadius = 0;

    List<(Vector3 v1, Vector3 v2)> _linesToDraw = new List<(Vector3 v1, Vector3 v2)> ();



    private void OnEnable()
    {
        int iterationLeft = 100;

        if (_reconstruct == null)
            _reconstruct = GetComponent<Reconstruct>();


        List<int> pointIndexes = ExtractBoundary(new List<Edge> (_reconstruct.BoundaryEdges));

        _averageRadius = CalculateAverageHoleRadius(new List<int>(pointIndexes));

        List<Vector3> innerPointCoords = GenerateInnerPoint(new List<int> (pointIndexes), new List<Edge>(_reconstruct.BoundaryEdges));
        List<Edge> innerEdges = GenerateInnerEdges(new List<Vector3> (innerPointCoords));

        _innerPoints.Clear();
        _innerPoints.AddRange(innerPointCoords);

        List<Vector3> furtherInnerCoords = new List<Vector3>(innerPointCoords);
        List<Edge> furtherInnerEdges = new List<Edge>(innerEdges);

        iterationLeft = 100;
        while (iterationLeft > 0)
        {
            furtherInnerCoords = GenerateInnerPoints(new List<Vector3>(furtherInnerCoords), furtherInnerEdges);

            if (furtherInnerCoords.Count > 0)
            {
                furtherInnerEdges = GenerateInnerEdges(new List<Vector3>(furtherInnerCoords));
                _innerPoints.AddRange(furtherInnerCoords);
            }
            else
            {
                break;
            }
        }

        _points.Clear();
        foreach (int index in pointIndexes)
        {
            _points.Add((_reconstruct.Mesh.vertices[index], index));
        }
    }
    private void OnDisable()
    {
        _linesToDraw.Clear();
    }

    private float CalculateAverageHoleRadius(List<int> pointIndexes)
    { 
        //Find center
        Vector3 center = Vector3.zero;
        foreach (int point in pointIndexes)
        {
            center += _reconstruct.Mesh.vertices[point];
        }
        center = center / pointIndexes.Count;

        //Find radius
        float totalRadius = 0;
        foreach (int point in pointIndexes)
        {
            totalRadius += Vector3.Distance(_reconstruct.Mesh.vertices[point], center);
        }
        totalRadius = totalRadius / pointIndexes.Count;

        return totalRadius;
    }

    private List<Vector3> GenerateInnerPoints(List<Vector3> pointCoords, List<Edge> boundaryEdges)
    {
        //Find center
        Vector3 center = Vector3.zero;
        foreach (Vector3 point in pointCoords)
        {
            center += point;
        }
        center = center / pointCoords.Count;

        //Generate inner points
        List<Vector3> generatedInnerPoints = new List<Vector3>();

        foreach (Edge edge in boundaryEdges)
        {
            Vector3 point1 = pointCoords[edge.vertex1];
            Vector3 point2 = pointCoords[edge.vertex2];

            Vector3 startingCoord = (point1 + point2) / 2;
            Vector3 offset = (center - startingCoord).normalized * _averageRadius * _advancingStrength;
            Vector3 newCoord = startingCoord + offset;

            bool isNearOtherInnerPoints = generatedInnerPoints.Any(coord => Vector3.Distance(coord, newCoord) < _averageRadius * _advancingStrength);
            bool isTooNearBoundaryPoints = pointCoords.Any(coord => Vector3.Distance(newCoord, coord) < _averageRadius * _advancingStrength * 0.5f);

            if (!isNearOtherInnerPoints && !isTooNearBoundaryPoints)
            {
                generatedInnerPoints.Add(newCoord);
            }
        }

        return generatedInnerPoints;
    }

    private List<Edge> GenerateInnerEdges(List<Vector3> points)
    {
        List<Edge> edges = new List<Edge>();

        for (int i=0; i<points.Count; i++)
        {
            List<Vector3> otherPoints = points
                .Where(point => point != points[i])
                .OrderBy(point => Vector3.Distance(point, points[i]))
                .ToList();

            //otherPoints.ForEach(coord => Debug.Log("Other points: " + (i, points.IndexOf(coord))));

            int iterationLeft = 100;
            while (iterationLeft > 0 && otherPoints.Count >0)
            {
                Edge newEdge = new Edge(i, points.IndexOf(otherPoints[0]));

                if (!edges.Contains(newEdge))
                {
                    edges.Add(newEdge);
                    _linesToDraw.Add((points[i], otherPoints[0]));
                    //Debug.Log("Added edge " + newEdge);
                    break;
                }
                else
                {
                    iterationLeft--;
                    otherPoints.RemoveAt(0);    
                }
            }
        }

        return edges;
    }

    private List<Vector3> GenerateInnerPoint(List<int> pointIndexes, List<Edge> boundaryEdges)
    {
        //Find center
        Vector3 center = Vector3.zero;
        foreach (int point in pointIndexes)
        {
            center += _reconstruct.Mesh.vertices[point];
        }
        center = center / pointIndexes.Count;

        //Generate inner points
        List<Vector3> generatedInnerPoints = new List<Vector3>();
        boundaryEdges = boundaryEdges.Where(edge => pointIndexes.Any(index => index == edge.vertex1 || index == edge.vertex2)).ToList();

        foreach (Edge edge in boundaryEdges)
        {
            int point1 = edge.vertex1;
            int point2 = edge.vertex2;  

            Vector3 startingCoord = (_reconstruct.Mesh.vertices[point1] + _reconstruct.Mesh.vertices[point2]) / 2;
            Vector3 offset = (center - startingCoord).normalized * _averageRadius * _advancingStrength;
            Vector3 newCoord = startingCoord + offset;

            bool isNearOtherInnerPoints = generatedInnerPoints.Any(coord => Vector3.Distance(coord, newCoord) < _averageRadius * _advancingStrength);
            bool isTooNearBoundaryPoints = pointIndexes.Any(point => Vector3.Distance(newCoord, _reconstruct.Mesh.vertices[point]) < _averageRadius * _advancingStrength * 0.5f);

            if (!isNearOtherInnerPoints && !isTooNearBoundaryPoints)
            {
                generatedInnerPoints.Add(newCoord);
            }
        }

        return generatedInnerPoints;
    }

    int _iterationLeft = 5000;
    private List<int> ExtractBoundary(List<Edge> edges)
    {
        List<int> consideredPoints = new List<int> ();
        int startingPoint = edges[0].vertex1;
        int currentPoint = startingPoint;

        float examinedParimeter = 0;

        consideredPoints.Add(currentPoint);

        while (_iterationLeft > 0)
        {
            int edgeIndex = edges.FindIndex(edge => edge.vertex1 == currentPoint || edge.vertex2 == currentPoint);
            if (edgeIndex >= 0)
            {
                Edge edge = edges[edgeIndex];
                int previousPoint = currentPoint;
                currentPoint = edge.vertex1 == currentPoint? edge.vertex2 : edge.vertex1;
                
                if (currentPoint == startingPoint)
                {
                    Debug.Log("Found a boundary " + consideredPoints.Count);
                    return (consideredPoints);
                }

                consideredPoints.Add(currentPoint);
                edges.RemoveAt(edgeIndex);

                examinedParimeter += Vector3.Distance(_reconstruct.Mesh.vertices[currentPoint], _reconstruct.Mesh.vertices[previousPoint]);
            }
            else 
            {
                Debug.Log("No more point");
                return (consideredPoints);
            }

            _iterationLeft--;
        }

        return (consideredPoints);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.blue;
        for (int i = 0; i < _points.Count; i++)
        {
            Vector3 point = _points[i].coord;
            int index = _points[i].index;
            Gizmos.DrawSphere(point, 0.01f);
            //Handles.Label(coord, index.ToString());
        }

        Gizmos.color = UnityEngine.Color.cyan;
        for (int i = 0; i < _innerPoints.Count; i++)
        {
            Vector3 point = _innerPoints[i];
            Gizmos.DrawSphere(point, 0.01f);
            //Handles.Label(coord, i.ToString());
        }

        foreach (var line in _linesToDraw)
        {
            Gizmos.DrawLine(line.v1, line.v2);
        }
    }
}
