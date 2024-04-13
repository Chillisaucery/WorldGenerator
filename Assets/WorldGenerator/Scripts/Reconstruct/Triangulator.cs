using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Triangulation;
using UnityEngine;

[ExecuteInEditMode]
public class Triangulator : MonoBehaviour
{
    [SerializeField]
    int _iteration = 10;


    FillHole _fillHole;

    List<Vector3> _points = new List<Vector3>();

    List<(Vector3 v1, Vector3 v2)> _linesToDraw = new List<(Vector3 v1, Vector3 v2)>();
    public List<(Vector3 v1, Vector3 v2)> LinesToDraw { get => _linesToDraw; }


    List<(Vector3 v1, Vector3 v2)> _innerBoundaryLines = new List<(Vector3 v1, Vector3 v2)> ();
    List<(Vector3 v1, Vector3 v2)> _outerBoundaryLines = new List<(Vector3 v1, Vector3 v2)>();

    [HideInInspector]
    public float Tolerance = 1;



    private void OnEnable()
    {
        _fillHole = GetComponent<FillHole>();

        _points = _fillHole.GetAllPoints();

        //Find center
        Vector3 center = GetCenter();

        _linesToDraw.Clear();
        GenerateLines(center);
        CheckNewBoundaryEdges();
        CheckSmallHoles(_linesToDraw);

        List<(Vector3 v1, Vector3 v2)> boundaryEdges = new List<(Vector3 v1, Vector3 v2)>();
        List<(Vector3 v1, Vector3 v2)> innerBoundaryEdges = new List<(Vector3 v1, Vector3 v2)>();
        GetBoundary(out boundaryEdges, out innerBoundaryEdges);
    }

    private void CheckSmallHoles(List<(Vector3 v1, Vector3 v2)> boundaryEdges)
    {
        int iterationLeft = _iteration;
        int i = 0;

        while (iterationLeft > 0 && i < boundaryEdges.Count)
        {
            var edge = boundaryEdges[i];

            var v1Edges = boundaryEdges.Where(e => e.v1 == edge.v1 || e.v2 == edge.v1).ToList();
            var v2Edges = boundaryEdges.Where(e => e.v1 == edge.v2 || e.v2 == edge.v2).ToList();

            bool didGetFourthEdge = false;
            (Vector3 v1, Vector3 v2) fourthEdge = (Vector3.negativeInfinity, Vector3.negativeInfinity);

            foreach (var e1 in v1Edges)
            {
                foreach (var e2 in v2Edges)
                {
                    if (boundaryEdges.Contains((e1.v1, e2.v1)))
                    {
                        fourthEdge = (e1.v1, e2.v1);
                        didGetFourthEdge = true;
                    }
                    else if (boundaryEdges.Contains((e1.v1, e2.v2)))
                    {
                        fourthEdge = (e1.v1, e2.v2);
                        didGetFourthEdge = true;
                    }
                    else if (boundaryEdges.Contains((e1.v2, e2.v1)))
                    {
                        fourthEdge = (e1.v2, e2.v1);
                        didGetFourthEdge = true;
                    }
                    else if (boundaryEdges.Contains((e1.v2, e2.v2)))
                    {
                        fourthEdge = (e1.v2, e2.v2);
                        didGetFourthEdge = true;
                    }
                }
            }

            if (didGetFourthEdge)
            {
                List<(Vector3 v1, Vector3 v2)> linesToAdd = new List<(Vector3 v1, Vector3 v2)> ();

                linesToAdd.Add((edge.v1, fourthEdge.v1));
                linesToAdd.Add((edge.v1, fourthEdge.v2));
                linesToAdd.Add((edge.v2, fourthEdge.v1));
                linesToAdd.Add((edge.v2, fourthEdge.v2));

                linesToAdd.RemoveAll(line => _linesToDraw.Contains(line));
                linesToAdd = linesToAdd.OrderBy(line => Vector3.Distance(line.v1, line.v2)).ToList();

                //_linesToDraw.Add(linesToAdd[0]);
            }

            iterationLeft--;
            i++;
        }
    }




    private void CheckNewBoundaryEdges()
    {
        List<(Vector3 v1, Vector3 v2)> boundaryEdges, innerBoundaryEdges;
        GetBoundary(out boundaryEdges, out innerBoundaryEdges);

        List<Vector3> boundaryPoints = new List<Vector3>();
        foreach (var line in boundaryEdges)
        {
            boundaryPoints.Add(line.v1);
            boundaryPoints.Add(line.v2);
        }
        boundaryPoints = boundaryPoints.Distinct().ToList();

        int lineCountBeforeAdding = 0;
        int iterationLeft = 100;

        while (iterationLeft > 0)
        {
            lineCountBeforeAdding = _linesToDraw.Count;

            foreach (Vector3 point in boundaryPoints)
            {
                List<Vector3> connectablePoints = new List<Vector3>(boundaryPoints);
                connectablePoints.Remove(point);
                connectablePoints = connectablePoints
                    .Except(GetPointsConnectedToThisPoint(point))
                    .OrderBy(p => Vector3.Distance(p, point)).ToList();

                foreach (Vector3 otherPoint in connectablePoints)
                {
                    List<Vector3> pointsInsideSphere = GetPointInsideSphere(point, otherPoint, _points);

                    if (pointsInsideSphere.Count <= 0)
                    {
                        _linesToDraw.Add((point, otherPoint));
                    }
                }
            }

            _linesToDraw = _linesToDraw.Distinct().ToList();

            iterationLeft--;

            if (_linesToDraw.Count == lineCountBeforeAdding)
            {
                break;
            }
        }

        Debug.Log("New Boundary edges: " + (boundaryEdges.Count, innerBoundaryEdges.Count));
    }

    private void GetBoundary(out List<(Vector3 v1, Vector3 v2)> boundaryEdges, out List<(Vector3 v1, Vector3 v2)> innerBoundaryEdges)
    {
        boundaryEdges = new List<(Vector3 v1, Vector3 v2)>();
        foreach (var line in _linesToDraw)
        {
            List<Vector3> pointsConnectedToV1 = GetPointsConnectedToThisPoint(line.v1);
            List<Vector3> pointsConnectedToV2 = GetPointsConnectedToThisPoint(line.v2);
            List<Vector3> sharedConnectedPoints = pointsConnectedToV1.Where(point => pointsConnectedToV2.Contains(point)).ToList();

            if (sharedConnectedPoints.Count <= 1)
            {
                boundaryEdges.Add(line);
            }
        }

        innerBoundaryEdges = boundaryEdges
            .Where(edge => _fillHole.InnerPoints.Contains(edge.v1) || _fillHole.InnerPoints.Contains(edge.v2))
            .ToList();
        _innerBoundaryLines = new List<(Vector3 v1, Vector3 v2)>(innerBoundaryEdges);
        _outerBoundaryLines = boundaryEdges.Except(innerBoundaryEdges).ToList();
    }

    private void GenerateLines(Vector3 center)
    {
        int iterationLeft = _iteration;
        int linesBeforeIteration = 0;

        while (linesBeforeIteration == 0 || linesBeforeIteration < _linesToDraw.Count)
        {
            List<Vector3> points = _points.OrderByDescending(point => Vector3.Distance(center, point)).ToList();
            linesBeforeIteration = _linesToDraw.Count;

            while (points.Count > 0 && iterationLeft > 0)
            {
                Vector3 point = points[0];

                List<Vector3> excludedPoints = new List<Vector3>() { point };
                excludedPoints.AddRange(GetPointsConnectedToThisPoint(point));

                List<Vector3> otherPoints = _points
                    .Except(excludedPoints)
                    .OrderBy(otherPoint => Vector3.Distance(otherPoint, point))
                    .ToList();

                if (otherPoints.Count >= 1)
                {
                    List<Vector3> pointsInsideSphere = GetPointInsideSphere(point, otherPoints[0], _points);

                    if (pointsInsideSphere.Count <= 0)
                    {
                        _linesToDraw.Add((point, otherPoints[0]));
                    }
                }

                points.RemoveAt(0);

                iterationLeft--;
            }

            _linesToDraw = _linesToDraw.Distinct().ToList();
        }

        Debug.Log("Iteration left: " + iterationLeft);

        _linesToDraw = _linesToDraw.Distinct().ToList();
    }

    List<Vector3> GetPointInsideSphere(Vector3 point1, Vector3 point2, List<Vector3> allPoints)
    {
        List<Vector3> otherPoints = new List<Vector3>(allPoints);

        otherPoints.Remove(point1);
        otherPoints.Remove(point2);

        Vector3 center = (point1 + point2) / 2;
        float radius = Vector3.Distance(point1, point2) / 2;

        return otherPoints.Where(point => Vector3.Distance(point, center) < radius * Tolerance).ToList();    
    }


    private List<Vector3> GetPointsConnectedToThisPoint(Vector3 point)
    {
        _linesToDraw = LinesToDraw.Distinct().ToList();

        List<Vector3> pointsConnectedToThisPoint = new List<Vector3>();

        for (int i = 0; i < _linesToDraw.Count; i++)
        {
            (Vector3 v1, Vector3 v2) line = _linesToDraw[i];

            if (Vector3.Distance(line.v1, point) < 0.0001f)
            {
                pointsConnectedToThisPoint.Add(line.v2);
            }
            else if (Vector3.Distance(line.v2, point) < 0.0001f)
            {
                pointsConnectedToThisPoint.Add(line.v1);
            }
        }

        return pointsConnectedToThisPoint;
    }

    private Vector3 GetCenter()
    {
        Vector3 center = Vector3.zero;

        foreach (Vector3 point in _points)
        {
            center += point;
        }
        center = center / _points.Count;
        return center;
    }



    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.blue;
        for (int i = 0; i < _points.Count; i++)
        {
            Vector3 point = _points[i];
            Gizmos.DrawSphere(point, 0.01f);
        }

        Gizmos.color = UnityEngine.Color.cyan;
        foreach (var line in _linesToDraw)
        {
            Gizmos.DrawLine(line.v1, line.v2);
        }

        Gizmos.color = UnityEngine.Color.yellow;
        foreach (var line in _innerBoundaryLines)
        {
            Gizmos.DrawLine(line.v1, line.v2);
        }

/*        foreach (var line in _outerBoundaryLines)
        {
            Gizmos.DrawLine(line.v1, line.v2);
        }*/
    }
}
