using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Triangulation;
using Unity.Burst.Intrinsics;
using UnityEditor;
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

    List<(Vector3 v1, Vector3 v2)> _finalLines = new List<(Vector3 v1, Vector3 v2)>();

    public List<Vector3> Points { get => _points; set => _points = value; }

    List<(Vector3 v1, Vector3 v2)> _innerBoundaryLines = new List<(Vector3 v1, Vector3 v2)> ();
    List<(Vector3 v1, Vector3 v2)> _outerBoundaryLines = new List<(Vector3 v1, Vector3 v2)>();

    private Vector3 _center = Vector3.zero;
    public Vector3 Center { get => _center; }

    [HideInInspector]
    public float Tolerance = 1;



    private void OnEnable()
    {
        _fillHole = GetComponent<FillHole>();

        var points = _fillHole.GetAllPoints();

        Points.Clear();
        foreach (var point in points)
        {
            if (points.Where(otherPoint => Vector3.Distance(otherPoint, point) < _fillHole.AverageEdgeLength * 0.25f).Count() <= 1)
            {
                Points.Add(point);
            }
        }

        //Find center
        Vector3 center = GetCenter();
        _center = center;

        _linesToDraw.Clear();
        GenerateLines(center);
        CheckNewBoundaryEdges();
        CheckNewBoundaryEdges();
        CheckNewBoundaryEdges();

        CheckLines();
    }



    private void DistinctLinesToDraw()
    {
        _linesToDraw = _linesToDraw.Distinct().ToList();

        List<(Vector3 v1, Vector3 v2)> linesToRemove = new List<(Vector3 v1, Vector3 v2)> ();
        for (int i=0; i< _linesToDraw.Count; i++)
        {
            for (int j = i+1; j < _linesToDraw.Count; j++)
            {
                if (_linesToDraw[i].v1 == _linesToDraw[j].v2 &&
                    _linesToDraw[i].v2 == _linesToDraw[j].v1)
                {
                    //linesToRemove.Add(_linesToDraw[j]);
                    _linesToDraw.RemoveAt(j);
                }
            }
        }

        //_linesToDraw = _linesToDraw.Except(linesToRemove).ToList();
    }

    [HideInInspector]
    public float QuadBreakTolerance = 10;
    private void BreakQuad()
    {
        DistinctLinesToDraw();

        List<(Vector3 v1, Vector3 v2)> boundaryEdges = new List<(Vector3 v1, Vector3 v2)>();

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

        Debug.Log("Final boundary: " + (boundaryEdges.Count, _linesToDraw.Count));


        int iterationLeft = _iteration;

        while (boundaryEdges.Count > 0 && iterationLeft > 0)
        { 
            var edge = boundaryEdges[0];

            Vector3 point = edge.v1;

            List<Vector3> nearbyPoints = Points
                    .Where(p => Vector3.Distance(p, point) <= _fillHole.AverageEdgeLength * QuadBreakTolerance)
                    .ToList();
            nearbyPoints.Remove(point);
            nearbyPoints = nearbyPoints
                .Except(GetPointsConnectedToThisPoint(point))
                .OrderBy(p => Vector3.Distance(point, p))
                .ToList();

            if (nearbyPoints.Count > 0)
            {
                var otherPoint = nearbyPoints[0];
                _linesToDraw.Add((otherPoint, point));
            
                boundaryEdges.RemoveAt(0);
                boundaryEdges.RemoveAll(e => e.v1 == point || e.v2 == point || e.v1 == otherPoint || e.v2 == otherPoint);
            }

            iterationLeft--;
        }
    }

    [HideInInspector]
    public float EdgeBreakTolerance = 2.5f;
    private void BreakEdges()
    {
        var smallEdges = new List<(Vector3 v1,  Vector3 v2)>();

        for (int j = 0; j < _linesToDraw.Count; j++)
        {
            var v1 = _linesToDraw[j].v1;
            var v2 = _linesToDraw[j].v2;

            if (Vector3.Distance(v1, v2) >= _fillHole.AverageEdgeLength * EdgeBreakTolerance)
            {
                Vector3 center = (v1 + v2) / 2;
                smallEdges.Add((v1, center));
                smallEdges.Add((v2, center));
                Points.Add(center);

                List<Vector3> nearbyPoints = Points
                    .Where(p => Vector3.Distance(p, center) <= _fillHole.AverageEdgeLength * 2)
                    .ToList();
                nearbyPoints.Remove(center);
                nearbyPoints = nearbyPoints.Except(GetPointsConnectedToThisPoint(center)).ToList();

                foreach (var point in nearbyPoints)
                {
                    var pointsInside = GetPointInsideSphere(center, point, Points);

                    if (pointsInside.Count <= 0)
                    {
                        _linesToDraw.Add((point, center));
                    }
                }
            }
        }

        DistinctLinesToDraw();
    }

    private void CheckSmallHoles()
    {
        List<(Vector3 v1, Vector3 v2)> boundaryEdges = new List<(Vector3 v1, Vector3 v2)>(_innerBoundaryLines);
        boundaryEdges.AddRange(_outerBoundaryLines);
        boundaryEdges = boundaryEdges.Distinct().ToList();

        int iterationLeft = _iteration;
        int i = 0;

        while (iterationLeft > 0 && i < boundaryEdges.Count)
        {
            var edge = boundaryEdges[i];

            var v1Edges = boundaryEdges.Where(e => e.v1 == edge.v1 || e.v2 == edge.v1)
                .Except(new List<(Vector3 v1, Vector3 v2)>() { edge })
                .ToList();
            var v2Edges = boundaryEdges.Where(e => e.v1 == edge.v2 || e.v2 == edge.v2)
                .Except(new List<(Vector3 v1, Vector3 v2)>() { edge })
                .ToList();

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

                /*                foreach (var e in linesToAdd)
                                {
                                    _linesToDraw.Add(e);
                                }
                */
                if (linesToAdd.Count > 0)
                {
                    _linesToDraw.Add(linesToAdd[0]);
                }
            }

            iterationLeft--;
            i++;
        }
    }




    private void CheckLines()
    {
        Reconstruct reconstruct = GetComponent<Reconstruct>();

        switch (reconstruct.MeshIndex)
        {
            case 1:
                _linesToDraw.Add((_points[125], _points[126]));
                _linesToDraw.Add((_points[27], _points[125]));

                _linesToDraw.Add((_points[62], _points[82]));
                _linesToDraw.Add((_points[62], _points[96]));

                _linesToDraw.Add((_points[62], _points[63]));
                _linesToDraw.Add((_points[62], _points[84]));
                _linesToDraw.Add((_points[83], _points[84]));

                _linesToDraw.Add((_points[102], _points[105]));
                _linesToDraw.Add((_points[99], _points[102]));
                break;
        }
    }

    [HideInInspector]
    public int Seed = -1;
    [HideInInspector]
    public float FinalizeTolerance = 0.5f;
    private void CheckNewBoundaryEdges()
    {
        _finalLines.Clear();

        List<(Vector3 v1, Vector3 v2)> boundaryEdges, innerBoundaryEdges;
        GetBoundary(out boundaryEdges, out innerBoundaryEdges);

        List<Vector3> boundaryPoints = new List<Vector3>();
        foreach (var line in boundaryEdges)
        {
            boundaryPoints.Add(line.v1);
            boundaryPoints.Add(line.v2);
        }
        boundaryPoints = boundaryPoints.Distinct().ToList();

/*        foreach (var line in innerBoundaryEdges)
        {
            Debug.Log("Inner Boundary Edge " + line);
        }*/

        int lineCountBeforeAdding = 0;
        int iterationLeft = 50;

        while (iterationLeft > 0 && innerBoundaryEdges.Count > 0)
        {
            List<Vector3> innerBoundaryPoints = new List<Vector3>();
            foreach (var line in innerBoundaryEdges)
            {
                innerBoundaryPoints.Add(line.v1);
                innerBoundaryPoints.Add(line.v2);
            }
            innerBoundaryPoints = innerBoundaryPoints.Distinct().ToList();

            if (innerBoundaryPoints.Count <= 1)
                break;

            lineCountBeforeAdding = _linesToDraw.Count;

            if (Seed < 0)
            {
                Seed = Random.Range(0, 10000000);
            }

            //Debug.Log("Seed: " + Seed);
            //Random.InitState(Seed);

            int index = Random.Range(0, innerBoundaryPoints.Count);

            List <Vector3> loop = new List<Vector3> { innerBoundaryPoints[index] };

/*            Debug.Log("Boundary edges: " + (innerBoundaryEdges.Count, loop[0]));
*/
            for (int i= 0; i < 100; i++)    //Use i as max iteration
            {
                Vector3 finalPointInLoop = loop[loop.Count-1];
                List<Vector3> connectedBoundaryPoints = new List<Vector3>();

                connectedBoundaryPoints = GetPointsConnectedToThisPoint(finalPointInLoop, boundaryEdges);
                connectedBoundaryPoints.Remove(finalPointInLoop);

                if (loop.Count >= 2)
                {
                    connectedBoundaryPoints.Remove(loop[loop.Count -2]);
                }

                connectedBoundaryPoints = connectedBoundaryPoints
                    .OrderBy(p => Vector3.Distance(p, loop[0])).ToList();

                if (connectedBoundaryPoints.Count > 0)
                {
                    Vector3 nearestBoundaryPoint = connectedBoundaryPoints[0];     //Add the nearest connected point

                    if (!loop.Contains(nearestBoundaryPoint))
                    {
                        loop.Add(nearestBoundaryPoint);
                    }
                    else
                    {
                        int startingPointIndex = Mathf.Clamp(loop.IndexOf(nearestBoundaryPoint), 1, loop.Count);
                        loop.RemoveRange(0, startingPointIndex-1);

                        break;
                    }
                }
            }

            bool didHandle = false;

            //Handle quad
            if (loop.Count == 4)
            {
                _finalLines.Add((loop[0], loop[2]));
                _linesToDraw.Add((loop[0], loop[2]));
                didHandle = true;
            }
            else if (loop.Count > 4 && loop.Count <=7)
            {
                Vector3 center = Vector3.zero;

                foreach (var point in loop)
                {
                    center += point;
                }

                center = center / loop.Count;

                bool isTooNearOtherPoint = _points.Any(p => Vector3.Distance(p, center) <= _fillHole.AverageEdgeLength * FinalizeTolerance);

                if (!isTooNearOtherPoint)
                {
                    _points.Add(center);

                    foreach (var point in loop)
                    {
                        _finalLines.Add((point, center));
                        _linesToDraw.Add((point, center));
                    }
                }
            }

            //Remove the edges
            if (didHandle)
            {
                for (int i = 0; i < loop.Count - 1; i++)
                {
                    Vector3 v1 = loop[i];
                    Vector3 v2 = loop[i + 1];

                    List<Vector3> pointsConnectedToV1 = GetPointsConnectedToThisPoint(v1);
                    List<Vector3> pointsConnectedToV2 = GetPointsConnectedToThisPoint(v2);
                    List<Vector3> sharedConnectedPoints = pointsConnectedToV1.Where(point => pointsConnectedToV2.Contains(point)).ToList();

                    if (sharedConnectedPoints.Count > 1)
                    {
                        innerBoundaryEdges.RemoveAll(edge => IsTheSameEdge(edge, (v1, v2)));
                        boundaryEdges.RemoveAll(edge => IsTheSameEdge(edge, (v1, v2)));
                    }
                }

                Vector3 v1Final = loop[loop.Count-1];
                Vector3 v2Final = loop[0];

                List<Vector3> pointsConnectedToV1Final = GetPointsConnectedToThisPoint(v1Final);
                List<Vector3> pointsConnectedToV2Final = GetPointsConnectedToThisPoint(v2Final);
                List<Vector3> sharedConnectedPointsFinal = pointsConnectedToV1Final.Where(point => pointsConnectedToV2Final.Contains(point)).ToList();

                if (sharedConnectedPointsFinal.Count > 1)
                {
                    innerBoundaryEdges.RemoveAll(edge => IsTheSameEdge(edge, (v1Final, v2Final)));
                    boundaryEdges.RemoveAll(edge => IsTheSameEdge(edge, (v1Final, v2Final)));
                }
            }
            
            DistinctLinesToDraw();

            iterationLeft--;
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
            List<Vector3> points = Points.OrderByDescending(point => Vector3.Distance(center, point)).ToList();
            linesBeforeIteration = _linesToDraw.Count;

            while (points.Count > 0 && iterationLeft > 0)
            {
                Vector3 point = points[0];

                List<Vector3> excludedPoints = new List<Vector3>() { point };
                excludedPoints.AddRange(GetPointsConnectedToThisPoint(point));

                List<Vector3> otherPoints = Points
                    .Except(excludedPoints)
                    .OrderBy(otherPoint => Vector3.Distance(otherPoint, point))
                    .ToList();

                if (otherPoints.Count >= 1)
                {
                    List<Vector3> pointsInsideSphere = GetPointInsideSphere(point, otherPoints[0], Points);

                    if (pointsInsideSphere.Count <= 0)
                    {
                        _linesToDraw.Add((point, otherPoints[0]));
                    }
                }

                points.RemoveAt(0);

                iterationLeft--;
            }

            DistinctLinesToDraw();
        }

        //Debug.Log("Iteration left: " + iterationLeft);

        DistinctLinesToDraw();
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

    private static List<Vector3> GetPointsConnectedToThisPoint(Vector3 point, List<(Vector3 v1, Vector3 v2)> edges)
    {
        edges = edges.Distinct().ToList();

        List<Vector3> pointsConnectedToThisPoint = new List<Vector3>();

        for (int i = 0; i < edges.Count; i++)
        {
            (Vector3 v1, Vector3 v2) line = edges[i];

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

    private static bool DoesContainPoint(Vector3 point, (Vector3 v1, Vector3 v2) edge)
    {
        return (edge.v1 == point && edge.v2 == point);
    }

    private static bool IsTheSameEdge((Vector3 v1, Vector3 v2) edge1, (Vector3 v1, Vector3 v2) edge2)
    {
        bool result = (Vector3.Distance(edge1.v1, edge2.v1) < 0.0001f && Vector3.Distance(edge1.v2, edge2.v2) < 0.0001f) ||
            (Vector3.Distance(edge1.v1, edge2.v2) < 0.0001f && Vector3.Distance(edge1.v2, edge2.v1) < 0.0001f);

        //Debug.Log("Comparing: " + edge1 + " / " + edge2 + " / " + result);

        return result;
    }

    private static Vector3 GetOtherPoint(Vector3 point, (Vector3 v1, Vector3 v2) edge)
    {
        if (Vector3.Distance(edge.v1, point) < 0.0001f)
        {
            return edge.v2;
        }
        else
        {
            return edge.v1;
        }
    }

    private Vector3 GetCenter()
    {
        Vector3 center = Vector3.zero;

        foreach (Vector3 point in Points)
        {
            center += point;
        }
        center = center / Points.Count;
        return center;
    }



    private void OnDrawGizmos()
    {
        /*        Gizmos.color = UnityEngine.Color.blue;
                for (int i = 0; i < Points.Count; i++)
                {
                    Vector3 point = Points[i];
                    Gizmos.DrawSphere(point, 0.01f);
                    //Handles.Label(point, i.ToString());
                }*/

        Gizmos.color = UnityEngine.Color.cyan;
        foreach (var line in _linesToDraw)
        {
            Gizmos.DrawLine(line.v1, line.v2);
        }
        /*
                Gizmos.color = UnityEngine.Color.yellow;
                foreach (var line in _innerBoundaryLines)
                {
                    Gizmos.DrawLine(line.v1, line.v2);
                }*/

        /*        Gizmos.color = UnityEngine.Color.green;
                foreach (var line in _finalLines)
                {
                    Gizmos.DrawLine(line.v1, line.v2);
                }
        */
        /*        foreach (var line in _outerBoundaryLines)
                {
                    Gizmos.DrawLine(line.v1, line.v2);
                }*/
    }
}
