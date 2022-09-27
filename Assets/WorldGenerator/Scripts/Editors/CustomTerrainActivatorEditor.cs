using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CustomTerrainActivator))]
[CanEditMultipleObjects]
public class CustomTerrainActivatorEditor : Editor
{
    bool showPerlin = false;
    bool showVoronoi = false;
    bool showSmooth = false;
    bool showMidpointDisplacement = false;

    public override void OnInspectorGUI()
    {
        CustomTerrainActivator customTerrainActivator = (CustomTerrainActivator)target;

        if (GUILayout.Button("ResetHeight"))
        {
            customTerrainActivator.ResetHeight();
        }



        showPerlin = EditorGUILayout.Foldout(showPerlin, "Perlin");

        if (showPerlin)
        {
            if (GUILayout.Button("PerlinNoise"))
            {
                customTerrainActivator.PerlinNoise();
            }
        }

        showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");

        if (showVoronoi)
        {
            if (GUILayout.Button("Voronoi"))
            {
                customTerrainActivator.VoronoiPeaks();
            }
        }

        showSmooth = EditorGUILayout.Foldout(showSmooth, "Smooth");

        if (showSmooth)
        {
            if (GUILayout.Button("Smooth"))
            {
                customTerrainActivator.Smooth();
            }
        }

        showMidpointDisplacement = EditorGUILayout.Foldout(showMidpointDisplacement, "Midpoint Displacement");

        if (showMidpointDisplacement)
        {
            if (GUILayout.Button("Midpoint Displacement"))
            {
                customTerrainActivator.MidpointDisplacement();
            }
        }
    }
}
