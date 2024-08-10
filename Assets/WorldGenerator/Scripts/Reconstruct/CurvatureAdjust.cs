using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class CurvatureAdjust : MonoBehaviour
{
    Reconstruct _reconstruct = null;
    Triangulator _triangulator = null;
    FillHole _fillHole = null;

    [SerializeField]
    float _steepnessTolerance = 1;
    [SerializeField]
    float _ringFactor = 0.8f;
    [SerializeField]
    float _smoothFactor = 1;

    List<Vector3> _newInnerPoints = new List<Vector3>();
    List<(Vector3 p1, Vector3 p2)> _displacementLines = new List<(Vector3 p1, Vector3 p2)>();
    List<Vector3> _nearBoundaryPoints = new List<Vector3>();
    List<(Vector3 p1, Vector3 p2)> _linesToDraw = new List<(Vector3 p1, Vector3 p2)>();
    List<float> _smoothness = new List<float>();

    public List<Vector3> Points { get => _newInnerPoints; }

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

        //Debug.Log("Near: " + _nearBoundaryPoints.Count);

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
            bool isInfluenced = IsConnectedToAPoint(influencingPoints, point, _linesToDraw);

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
            float lerpFactor = Mathf.Pow(_ringFactor, Mathf.Max(ring-1,0));
            innerPoints[i] = Vector3.LerpUnclamped(point, projectedPoint, lerpFactor);

            //Add points to influencing points
            point = innerPoints[i];
            adjustedPoints.Add(point);

            //Update for lines
            ReplacePointInEdges(_linesToDraw, pointBeforeAdjust, point);
            _displacementLines.Add((pointBeforeAdjust, point));
        }

        _newInnerPoints = new List<Vector3>(innerPoints);

        //Calculate Smoothness
        _smoothness.Clear();
        for (int i = 0; i < _newInnerPoints.Count; i++)
        {
            Vector3 point = _newInnerPoints[i];

            List<Vector3> neighbours = GetNeighborsByRadius(_newInnerPoints, point, edgeLength);
            float smoothness = GetDistanceFromNearestPlane(point, neighbours);
            _smoothness.Add(smoothness);
        }

        _smoothness = Normalize(_smoothness);

        float averageSmoothness = _smoothness.Average();
/*        Debug.Log("Average " + averageSmoothness);*/
        for (int i = 0; i < _smoothness.Count; i++)
        {
            float smoothness = _smoothness[i];
/*            Debug.Log("Smoothness: " +  smoothness);
*/
            if (smoothness < averageSmoothness * _steepnessTolerance)
            {
                smoothness = 0;
                _smoothness[i] = smoothness;
            }
        }

        //Smooth points
        for (int i = 0; i < _newInnerPoints.Count; i++)
        {
            Vector3 pointBeforeAdjust = _newInnerPoints[i];

            //Check if it is near boundary
            bool isNearBound = IsConnectedToAPoint(nearBoundaryPoints, pointBeforeAdjust, _linesToDraw);

            if (isNearBound)
            {
                continue;
            }

            Vector3 smoothedPoint = GetSmoothedPoint(pointBeforeAdjust, innerPoints, edgeLength);
            float smoothness = _smoothness[i];
            float lerpFactor = smoothness * _smoothFactor;

            Vector3 newPoint = Vector3.Lerp(pointBeforeAdjust, smoothedPoint, lerpFactor);

            //Update for lines
            ReplacePointInEdges(_linesToDraw, pointBeforeAdjust, newPoint);
            //_displacementLines.Add((pointBeforeAdjust, pointBeforeAdjust));
        }
    }

    public Vector3 GetSmoothedPoint(Vector3 point, List<Vector3> allPoints, float radius = 1.0f)
    {
        List<Vector3> neighbors = GetNeighborsByRadius(allPoints, point, radius);

        if (neighbors.Count == 0)
        {
            return point;
        }

        Vector3 smoothedPosition = Vector3.zero;
        float totalWeight = 0.0f;

        foreach (Vector3 neighbor in neighbors)
        {
            float distance = Vector3.Distance(point, neighbor);
            float weight = 1.0f / (distance * distance);  //Quadratic Inverse distance weighting

            smoothedPosition += neighbor * weight;
            totalWeight += weight;
        }

        smoothedPosition /= totalWeight;

        return smoothedPosition;
    }

    public static bool IsConnectedToAPoint(List<Vector3> influencingPoints, Vector3 point, List<(Vector3 p1, Vector3 p2)> edges)
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

   /* public float GetSmoothness(Vector3 point, List<Vector3> points, float radius)
    {
        List<Vector3> neighbors = GetNeighbors(points, point, radius);

        if (neighbors.Count == 0)
            return 0f; 

        Vector3 normal = GetDistanceFromNearestPlane(point, neighbors);

        float meanCurvature = 0f;
        foreach (var neighbor in neighbors)
        {
            Vector3 neighborNormal = GetDistanceFromNearestPlane(neighbor, GetNeighbors(points, neighbor, radius));
            float angle = Vector3.Angle(normal, neighborNormal);
            float distance = Vector3.Distance(point, neighbor);
            meanCurvature += angle / distance;
        }

        // Average the mean curvature
        meanCurvature /= neighbors.Count;

        return meanCurvature;
    }*/

    private List<Vector3> GetNeighborsByRadius(List<Vector3> points, Vector3 currentPoint, float radius)
    {
        List<Vector3> neighbors = new List<Vector3>();

        foreach (var point in points)
        {
            if (point != currentPoint && Vector3.Distance(point, currentPoint) <= radius)
            {
                neighbors.Add(point);
            }
        }

        return neighbors;
    }

    private float GetDistanceFromNearestPlane(Vector3 point, List<Vector3> neighbors)
    {
        if (neighbors.Count < 3)
        {
            return 0;
        }

        float minDistance = float.MaxValue;
        int iteration = 0;

        for (int i = 0; i < neighbors.Count; i++)
        {
            for (int j = i + 1; j < neighbors.Count; j++)
            {
                for (int k = j + 1; k < neighbors.Count; k++)
                {
                    Vector3 p1 = neighbors[i];
                    Vector3 p2 = neighbors[j];
                    Vector3 p3 = neighbors[k];

                    Vector3 v1 = p2 - p1;
                    Vector3 v2 = p3 - p1;
                    Vector3 normal = Vector3.Cross(v1, v2).normalized;
                    Plane plane = new Plane(normal, p1);

                    float distance = Mathf.Abs(plane.GetDistanceToPoint(point));

                    // Update the minimum distance
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }

                    iteration++;

                    if (iteration >= 20)
                        break;
                }
            }
        }

        return minDistance;
    }

    public static List<float> Normalize(List<float> values)
    {
        if (values == null || values.Count == 0)
        {
            return null;
        }

        float min = values.Min();
        float max = values.Max();
        float range = max - min;

        if (range == 0)
        {
            return values.Select(v => 0.5f).ToList();
        }

        List<float> normalizedValues = values.Select(v => (v - min) / range).ToList();
        return normalizedValues;
    }

    private void OnDrawGizmos()
    {
        /*Gizmos.color = UnityEngine.Color.magenta;
        foreach (Vector3 point in _nearBoundaryPoints)
        {
            Gizmos.DrawSphere(point, 0.01f);
        }*/

        Gizmos.color = UnityEngine.Color.blue;
        for (int i = 0; i < _newInnerPoints.Count; i++)
        {
            Vector3 point = _newInnerPoints[i];
            Gizmos.DrawSphere(point, 0.01f);

            //Debug.Log("Smoothness: " + _smoothness[i]);
            //Gizmos.DrawLine(point, point + Vector3.up * _smoothness[i]);
            //Gizmos.DrawSphere(point + point + Vector3.up * _smoothness[i], 0.05f);
        }

/*        Gizmos.color = UnityEngine.Color.cyan;
        foreach (var line in _linesToDraw)
        {
            Gizmos.DrawLine(line.p1, line.p2);
        }*/

/*        Gizmos.color = UnityEngine.Color.green;
        foreach (var line in _displacementLines)
        {
            Gizmos.DrawLine(line.p1, line.p2);
            Gizmos.DrawSphere(line.p1, 0.005f);
        }*/

        //Debug.Log("Gizmos: " + _nearBoundaryPoints.Count);
    }
}
