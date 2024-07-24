using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class CurvatureAdjust : MonoBehaviour
{
    Reconstruct _reconstruct = null;
    Triangulator _triangulator = null;
    FillHole _fillHole = null;

    List<Vector3> _newInnerPoints = new List<Vector3>();
    List<(Vector3 p1, Vector3 p2)> _displacementLines = new List<(Vector3 p1, Vector3 p2)>();
    List<Vector3> _nearBoundaryPoints = new List<Vector3>();
    List<(Vector3 p1, Vector3 p2)> _linesToDraw = new List<(Vector3 p1, Vector3 p2)>();



    private void OnEnable()
    {
        //Init
        if (_reconstruct == null) 
        {
            _reconstruct = GetComponent<Reconstruct>();
            _triangulator = GetComponent<Triangulator>();
            _fillHole = GetComponent<FillHole>();
        }

        Vector3 center = _triangulator.Center;
        float edgeLength = _fillHole.AverageEdgeLength;

        //Get mesh points
        List<Vector3> points = new List<Vector3>(_reconstruct.Mesh.vertices);
        points = points.OrderBy(point => Vector3.Distance(point, center)).ToList();
        points = points.GetRange(0, points.Count/2);    //Take the first half ==> Better to calculate

        //Get inner points
        List<Vector3> innerPoints = _triangulator.Points;
        innerPoints = innerPoints.Except(points).ToList();

        //Get near boundary points
        List<Vector3> nearBoundaryPoints = new List<Vector3>();     //Including boundary points

        foreach (Vector3 point in points)
        {
            bool isCloseToInnerPoints = innerPoints
                .Any(p => Vector3.Distance(p, point) < edgeLength * 3.5f);

            if (isCloseToInnerPoints)
            {
                nearBoundaryPoints.Add(point);
            }
        }

        _nearBoundaryPoints = new List<Vector3>(nearBoundaryPoints);

        Debug.Log("Near: " + _nearBoundaryPoints.Count);

        float maxDistanceToCenter = Vector3.Distance(innerPoints[0], center);

        //Adjust ring
        _linesToDraw = _triangulator.LinesToDraw;
        _displacementLines.Clear();
        List<Vector3> influencingPoints = new List<Vector3>(nearBoundaryPoints);
        List<Vector3> adjustedPoints = new List<Vector3>();

        int ring = 0;

        for (int i=0; i <innerPoints.Count; i++)    //From further to closest to center
        {
            Vector3 point = innerPoints[i];
            Vector3 pointBeforeAdjust = point;

            //Check influencing points
            bool isInfluenced = IsConnectedToInfluencingPoints(influencingPoints, point, _linesToDraw);

            if (!isInfluenced)  //New ring
            {
                influencingPoints.AddRange(adjustedPoints);
                influencingPoints = influencingPoints.Distinct().ToList();
                adjustedPoints.Clear();
                ring++;
            }

            //Adjust to match curvature
            List<Vector3> nearestEdge = influencingPoints
                .OrderBy(p => Vector3.Distance(p, point))
                .ToList().GetRange(0, 2);

            Vector3 middleOfNearestEdge = Vector3.Lerp(nearestEdge[0], nearestEdge[1], 0.5f);
            Vector3 oppositePoint = middleOfNearestEdge - point + middleOfNearestEdge;

            Vector3 nearestThirdPoint = nearBoundaryPoints
                .Except(nearestEdge)
                .OrderBy(p => Vector3.Distance(p, oppositePoint))
                .ToList()[0];

             Vector3 projectedPoint = ProjectPointOntoPlane(
                nearestThirdPoint,
                nearestEdge[0],
                nearestEdge[1],
                point);

            /* float distanceToCenter = Vector3.Distance(pointBeforeAdjust, center);
             float lerpFactor = Mathf.Pow(1 - Mathf.Clamp01(distanceToCenter / maxDistanceToCenter), 2);*/
            float lerpFactor = Mathf.Pow(0.8f, ring);
            innerPoints[i] = Vector3.Lerp(point, projectedPoint, lerpFactor);

            //Add points to influencing points
            point = innerPoints[i];
            adjustedPoints.Add(point);

            //Update for lines
            ReplacePointInEdges(_linesToDraw, pointBeforeAdjust, point);
            _displacementLines.Add((pointBeforeAdjust, point));

            //influencingPoints.Add(point);
        }

        _newInnerPoints = new List<Vector3>(innerPoints);
    }

    public static bool IsConnectedToInfluencingPoints(List<Vector3> influencingPoints, Vector3 point, List<(Vector3 p1, Vector3 p2)> edges)
    {
        HashSet<Vector3> influencingPointsSet = new HashSet<Vector3>(influencingPoints);

        foreach (var edge in edges)
        {
            Vector3 p1 = edge.p1;
            Vector3 p2 = edge.p2;

            if (p1 == point && influencingPointsSet.Contains(p2))
            {
                return true;
            }
            if (p2 == point && influencingPointsSet.Contains(p1))
            {
                return true;
            }
        }

        return false;
    }

    public static Vector3 ProjectPointOntoPlane(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        Vector3 AB = B - A;
        Vector3 AC = C - A;

        Vector3 normal = Vector3.Cross(AB, AC).normalized;

        Plane plane = new Plane(normal, A);

        Vector3 projectedD = D - plane.normal * plane.GetDistanceToPoint(D);

        return projectedD;
    }

    public static void ReplacePointInEdges(List<(Vector3 p1, Vector3 p2)> edges, Vector3 currentPoint, Vector3 newPoint)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            Vector3 p1 = edges[i].p1;
            Vector3 p2 = edges[i].p2;

            if (p1 == currentPoint)
            {
                p1 = newPoint;
            }

            if (p2 == currentPoint)
            {
                p2 = newPoint;
            }

            edges[i] = (p1, p2);
        }
    }

    public static List<(int pointIdx1, int pointIdx2)> ConvertEdgesToIndices(List<(Vector3 p1, Vector3 p2)> edges, List<Vector3> points)
    {
        Dictionary<Vector3, int> pointToIndexMap = new Dictionary<Vector3, int>();

        for (int i = 0; i < points.Count; i++)
        {
            pointToIndexMap[points[i]] = i;
        }

        List<(int pointIdx1, int pointIdx2)> edgesAsIndexes = new List<(int pointIdx1, int pointIdx2)>();

        foreach (var edge in edges)
        {
            int index1 = pointToIndexMap[edge.p1];
            int index2 = pointToIndexMap[edge.p2];
            edgesAsIndexes.Add((index1, index2));
        }

        return edgesAsIndexes;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.magenta;
        foreach (Vector3 point in _nearBoundaryPoints)
        {
            Gizmos.DrawSphere(point, 0.01f);
        }

        Gizmos.color = UnityEngine.Color.yellow;
        foreach (Vector3 point in _newInnerPoints)
        {
            Gizmos.DrawSphere(point, 0.01f);
        }

        Gizmos.color = UnityEngine.Color.cyan;
        foreach (var line in _linesToDraw)
        {
            Gizmos.DrawLine(line.p1, line.p2);
        }

        Gizmos.color = UnityEngine.Color.green;
        foreach (var line in _displacementLines)
        {
            Gizmos.DrawLine(line.p1, line.p2);
            Gizmos.DrawSphere(line.p1, 0.005f);
        }

        //Debug.Log("Gizmos: " + _nearBoundaryPoints.Count);
    }
}
