using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Color = UnityEngine.Color;
using static Utils;

[ExecuteInEditMode]
public class TerrainBezier : MonoBehaviour
{
    //Serialize Fields
    [SerializeField]
    Terrain terrain, baseTerrain;

    [SerializeField]
    List<Vector2> controlPoints = new List<Vector2>();

    [SerializeField]
    List<Vector2> baseControlPoints = new List<Vector2>();

    [SerializeField]
    bool shouldScaleMap = false, shouldScaleBaseMap = false;

    [SerializeField, Range(0,1)]
    float step = 0.05f, g1Strength = 0.5f;

    [SerializeField, Range(-1, 2)]
    int gContinuityLevel = 2;

    [SerializeField]
    bool shouldMerge = false;

    [SerializeField]
    int bSplineRadius = 4;

    [SerializeField, Range(0,1)]
    float distanceToMoveBezier = 0.4f;

    //Local variables
    List<Vector2> smoothedControlPoints = new List<Vector2>();

    List<Vector2> curve = new List<Vector2>();
    List<Vector2> baseCurve = new List<Vector2>();

    List<Vector2> newControlPts = new List<Vector2>();
    List<Vector2> newCurve = new List<Vector2>();



    private void OnEnable()
    {
        smoothedControlPoints = new List<Vector2>(controlPoints);
        
        if (gContinuityLevel>=0)
            smoothedControlPoints = G0Smooth(smoothedControlPoints, baseControlPoints);
        if (gContinuityLevel >= 1)
            smoothedControlPoints = G1Smooth(smoothedControlPoints, baseControlPoints, g1Strength);
        if (gContinuityLevel >= 2)
            smoothedControlPoints = G2Smooth(smoothedControlPoints, baseControlPoints);

        baseCurve.Clear();
        curve.Clear();

        baseCurve = GenerateBezierCurve(step, baseControlPoints);
        curve = GenerateBezierCurve(step, smoothedControlPoints);

        newControlPts.Clear();
        newCurve.Clear();

        if (shouldMerge)
        {
            newControlPts = MergerCtrlPts(controlPoints, baseControlPoints);
            
            newCurve = GenerateBezierCurve(step/2, newControlPts);
            //newCurve = GenerateBSplineCurve(step/2, newControlPts, bSplineRadius);

            Vector2 contactPoint = GetPointInCurve(newCurve, controlPoints[0].x);
            Vector2 middlePoint = (baseControlPoints[0] + controlPoints[controlPoints.Count-1])/2;
            //Debug.Log(middlePoint);

            baseCurve = ExtractCurve(newCurve, baseControlPoints[0], middlePoint);
            //baseCurve.Add(contactPoint);

            curve = ExtractCurve(newCurve, middlePoint, controlPoints[controlPoints.Count-1]);
            //curve.Insert(0,contactPoint);
        }

        (int resolution, int maxHeight) = (terrain.terrainData.heightmapResolution, Mathf.RoundToInt(terrain.terrainData.heightmapScale.y));

        baseTerrain.terrainData.SetHeights(0, 0, 
            ConvertCurveToHeightmap(new List<Vector2> (baseCurve), resolution, maxHeight, shouldScaleBaseMap, baseControlPoints[0].x));
        terrain.terrainData.SetHeights(0, 0, 
            ConvertCurveToHeightmap(new List<Vector2> (curve), resolution, maxHeight, shouldScaleMap, controlPoints[0].x));

        baseTerrain.GetComponent<TerrainBezierColor>().GenerateTexture(shouldMerge);
        terrain.GetComponent<TerrainBezierColor>().GenerateTexture(shouldMerge);
    }

    

    private List<Vector2> ExtractCurve(List<Vector2> curve, Vector2 start, Vector2 end)
    {
        List<Vector2> newCurve = new List<Vector2>();

        for (int i = 0; i<curve.Count; i++)
        {
            //If it is before the start
            if (i + 1 < curve.Count && curve[i].x < start.x && curve[i + 1].x > start.x)
            {
                float factor = (start.x - curve[i].x) / (curve[i + 1].x - curve[i].x);
                newCurve.Add(GetPointInLine(curve[i], curve[i + 1], factor));

                //Debug.Log("Before start " + (curve[i], curve[i+1]) + " " + (start,end) + " " + (factor, GetPointInLine(curve[i], curve[i + 1], factor)));
            }

            //If it is after the end
            if (i -1 >= 0 && curve[i].x > end.x && curve[i - 1].x < end.x)
            {
                float factor = (end.x - curve[i-1].x) / (curve[i].x - curve[i-1].x);
                newCurve.Add(GetPointInLine(curve[i-1], curve[i], factor));

                //Debug.Log("After end " + (curve[i-1], curve[i]) + " " + (start, end) + " " + (factor,GetPointInLine(curve[i - 1], curve[i], factor)));
            }

            if (curve[i].x >= start.x && curve[i].x <= end.x)
                newCurve.Add(curve[i]);
        }

        return newCurve;
    }


    private List<Vector2> MergerCtrlPts(List<Vector2> controlPoints, List<Vector2> baseControlPoints)
    {
        List<Vector2> newControlPts = new List<Vector2>();

        //Check if it should move the control points forward
        float distanceBetweenTiles = controlPoints[0].x - baseControlPoints[baseControlPoints.Count - 1].x;
        float maximumDistanceAllowed = (controlPoints[0].x - baseControlPoints[0].x) * distanceToMoveBezier;

        Vector2 offset = Vector2.zero;

        //If it should move, also add the offset into the new control points
        if (distanceBetweenTiles >= maximumDistanceAllowed)
        {
            offset = new Vector2(maximumDistanceAllowed, 0);
        }



        for (int i = 0; i < baseControlPoints.Count-1; i++)
            newControlPts.Add(baseControlPoints[i] + offset);



        if (Mathf.Abs(baseControlPoints[baseControlPoints.Count -1].x - controlPoints[0].x) <= 0.1f)
        {
            //If the points are close together on the x-axis, take the average
            newControlPts.Add((baseControlPoints[baseControlPoints.Count - 1] + controlPoints[0]) / 2);
        }
        else
        {
            //If ther are too far, add both points
            newControlPts.Add(baseControlPoints[baseControlPoints.Count - 1] + offset);
            //newControlPts.Add((baseControlPoints[baseControlPoints.Count - 1] + controlPoints[0]) / 2);
            newControlPts.Add(controlPoints[0]);
        }

        for (int i = 1; i < controlPoints.Count; i++)
            newControlPts.Add(controlPoints[i]);

        //newControlPts.ForEach(e => Debug.Log(e));

        return newControlPts;
    }

    private void OnDrawGizmos()
    {
        DrawBezierCurve(Color.black, baseCurve);
        DrawBezierCurve(Color.black, curve);

        DrawBezierCurve(new Color(1, 0.2f, 0.6f, 0.5f), newControlPts);
        //DrawBezierCurve(Color.red, newCurve);

        DrawBezierCurve(Color.green, baseControlPoints);
        DrawBezierCurve(Color.yellow, controlPoints);
        DrawBezierCurve(Color.cyan, smoothedControlPoints);

        //Gizmos.DrawWireSphere(Vector3.zero, 500);
        //Gizmos.DrawLine(Vector3.zero, new Vector3(600, 100, 700));
    }

    //GO G1 G2 continuity
    private List<Vector2> G0Smooth(List<Vector2> controlPts, List<Vector2> baseControlPts)
    {
        controlPts[0] = baseControlPts[baseControlPts.Count - 1];
        return controlPts;
    }

    private List<Vector2> G1Smooth(List<Vector2> controlPts, List<Vector2> baseControlPts, float strength)
    {
        Vector2 direction = (baseControlPts[baseControlPts.Count-1] - baseControlPts[baseControlPts.Count-2]).normalized;
        Vector2 newPoint = controlPts[0] + direction * ((controlPts[1] - controlPts[0]).x / direction.x);

        controlPts[1] = Vector2.Lerp(controlPts[1], newPoint, strength);

        return controlPts;
    }

    private List<Vector2> G2Smooth(List<Vector2> controlPts, List<Vector2> baseControlPts)
    {
        float t = (controlPts[2].x - controlPts[0].x) / (controlPts[3].x - controlPts[0].x);

        List<Vector2> nearbyControlPts = new List<Vector2>();

        nearbyControlPts.Add(controlPoints[0]);
        nearbyControlPts.Add(controlPoints[1]);
        nearbyControlPts.Add(controlPoints[3]);

        controlPts[2] = GetBezierCurvePoint(t, nearbyControlPts);

        return controlPts;
    }

    //Low level methods
    private float[,] ConvertCurveToHeightmap(List<Vector2> curve, int resolution, int maxHeight, bool keepWorldScale, float start)
    {
        float[,] heightmap = new float[resolution, resolution];

        float xScale = keepWorldScale? resolution*1f/500 : resolution / (curve[curve.Count - 1].x - curve[0].x);
        float yScale = 1f / maxHeight;

        float xOffset = start;

        for (int i=0; i<curve.Count; i++)
        {
            curve[i] = new Vector2(curve[i].x - xOffset, curve[i].y);
        }

        for (int i=0; i < curve.Count-1; i++)
        {
            for (int x = Mathf.RoundToInt(curve[i].x * xScale); x < Mathf.RoundToInt(curve[i+1].x * xScale) && x <resolution; x++)
            {
                float lerpFactor = (x - curve[i].x * xScale) / (curve[i + 1].x * xScale - curve[i].x * xScale);
                float height = Mathf.Lerp(curve[i].y, curve[i + 1].y, lerpFactor) * yScale;

                for (int y = 0; y < resolution; y++)
                    heightmap[x, y] = height;
            }
        }


        return heightmap;
    }

    private void DrawBezierCurve(Color color, List<Vector2> curve)
    {
        Gizmos.color = color;

        for (int i = 0; i < curve.Count - 1; i++)
        {
            Vector3 start = transform.position + new Vector3 (terrain.terrainData.size.x, curve[i].y, curve[i].x*2);
            Vector3 end = transform.position + new Vector3 (terrain.terrainData.size.x, curve[i + 1].y, curve[i+1].x*2);

            Gizmos.DrawLine(start, end);
        }
    }

    private List<Vector2> GenerateBezierLocalCurve(float step, List<Vector2> controlPoints, int localControlRadius)
    {
        return GenerateBezierLocalCurve(step, controlPoints, localControlRadius, controlPoints[0], controlPoints[controlPoints.Count - 1]);
    }

    private List<Vector2> GenerateBezierLocalCurve(float step, List<Vector2> ctrlPts, int localControlRadius, Vector2 start, Vector2 end)
    {
        float distance = end.x - start.x;

        int pointerIndex = 0;

        List<Vector2> curve = new List<Vector2>();

        curve.Add(start);
        AddIfPointInRange(ref curve, ctrlPts[0], start.x, end.x);

        for (float i = 0; i <= 1; i += step)
        {
            if (i * distance > ctrlPts[pointerIndex].x)
                pointerIndex++;

            List<Vector2> localCtrlPts = new List<Vector2>();

            for (int j = Mathf.Max(0, pointerIndex - localControlRadius); 
                j < Mathf.Min(ctrlPts.Count, pointerIndex+localControlRadius); j++)
            {
                localCtrlPts.Add(ctrlPts[pointerIndex]);
            }

            float factor = i*distance / (localCtrlPts[localCtrlPts.Count - 1].x - localCtrlPts[0].x);

            Vector2 newPoint = GetBezierCurvePoint(factor, localCtrlPts);
            AddIfPointInRange(ref curve, newPoint, start.x, end.x);
        }

        AddIfPointInRange(ref curve, ctrlPts[ctrlPts.Count - 1], start.x, end.x);
        curve.Add(end);

        return curve;
    }

    private List<Vector2> GenerateBSplineCurve(float step, List<Vector2> ctrlPts, int localControlRadius)
    {
        int pointerIndex = 0;
        float distance = ctrlPts[ctrlPts.Count-1].x - ctrlPts[0].x;

        List<Vector2> curve = new List<Vector2>();

        for (float i = 0; i <= 1; i += step)
        {
            int startPointIndex = pointerIndex;// Mathf.Max(0, pointerIndex-2);
            int endPointerIndex = Mathf.Min(ctrlPts.Count-1, pointerIndex + localControlRadius);

            if (pointerIndex <ctrlPts.Count && i * distance > ctrlPts[endPointerIndex].x)
                pointerIndex += localControlRadius;

            List<Vector2> localCtrlPts = new List<Vector2>();

            for (int j = startPointIndex; j <= endPointerIndex; j++)
            {
                localCtrlPts.Add(ctrlPts[j]);
            }

            //float factor = (i - ctrlPts[Mathf.Max(0, pointerIndex - localControlRadius)].x/distance) * distance 
            //                / (localCtrlPts[localCtrlPts.Count - 1].x - localCtrlPts[0].x);

            Debug.Log("At: " + i*distance + " Base: " + ctrlPts[Mathf.Max(0, pointerIndex - localControlRadius)].x / distance);
            //Debug.Log("Local Control Points: ");
            //localCtrlPts.ForEach(point => Debug.Log(point));
            //Debug.Log(" Factor: " + factor + " Pointer Index: " + pointerIndex);

            Vector2 newPoint = GetBSplinePoint(i*distance, localCtrlPts);
            //Vector2 newPoint = GetBezierCurvePoint()
            curve.Add(newPoint);
        }
        curve.Add(ctrlPts[ctrlPts.Count - 1]);

        return curve;
    }

    private Vector2 GetBSplinePoint(float x, List<Vector2> localCtrlPts)
    {
        float y = 0;
        float totalDistance = 0;
        List<float> factorList = new List<float>();

        localCtrlPts.ForEach((point) => totalDistance += Mathf.Abs(point.x - x));
        localCtrlPts.ForEach(point => factorList.Add(GAUSSIAN(Mathf.Abs(point.x-x) / totalDistance)));

        List<float> normalizedFactorList = NORMALIZE_LIST(factorList);

        for (int i=0; i < normalizedFactorList.Count;i++)
        {
            Debug.Log("List: " + localCtrlPts[i].x + " " + (localCtrlPts[i].y, normalizedFactorList[i], factorList[i]));

            y += localCtrlPts[i].y * normalizedFactorList[i];
        }

        return new Vector2(x, y);
    }

    private bool AddIfPointInRange(ref List<Vector2> curve, Vector2 newPoint, float start, float end)
    {
        bool isInRange = newPoint.x >= start && newPoint.x <= end;

        if (isInRange)
            curve.Add(newPoint);

        return isInRange;
    }

    public static List<Vector2> GenerateBezierCurve(float step, List<Vector2> controlPoints)
    {
        List<Vector2> curve = new List<Vector2>();

        curve.Add(controlPoints[0]);

        for (float i = 0; i <= 1; i += step)
        {
            curve.Add(GetBezierCurvePoint(i, controlPoints));
        }

        curve.Add(controlPoints[controlPoints.Count - 1]);

        return curve;
    }

    public static Vector2 GetBezierCurvePoint(float t, List<Vector2> controlPoints)
    {
        while (controlPoints.Count > 1)
        {
            List<Vector2> newPoints = new List<Vector2>();

            for (int i=0; i<controlPoints.Count-1; i++)
            {
                newPoints.Add(controlPoints[i] * (1 - t) + controlPoints[i+1] * (t));
            }

            controlPoints = newPoints;
        }

        return controlPoints[0];
    }

    public static Vector2 GetPointInCurve(List<Vector2> curve, float x)
    {
        for (int i = 0; i < curve.Count - 1; i++)
            if (curve[i].x < x && curve[i + 1].x > x)
                return GetPointInLine(curve[i], curve[i + 1], x);

        return Vector2.zero;
    }

    public static Vector2 GetPointInLine(Vector2 start, Vector2 end, float x)
    {
        return Vector2.Lerp(start, end, x);
        //return (end - start).normalized * (x - start.x) + start;
    }
}
