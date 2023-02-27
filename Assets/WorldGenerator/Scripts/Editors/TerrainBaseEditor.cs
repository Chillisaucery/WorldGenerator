using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;



#if UNITY_EDITOR

[CustomEditor(typeof(TerrainBaseActivator))]
//[CanEditMultipleObjects]
public class TerrainBaseEditor : Editor
{
    bool showPerlin = false;
    bool showVoronoi = false;
    bool showSmooth = false;
    bool showMidpointDisplacement = false;
    bool showTexture = false;
    bool showVegetation = false;

    public override void OnInspectorGUI()
    {
        TerrainBaseActivator customTerrainActivator = (TerrainBaseActivator)target;

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

        showTexture = EditorGUILayout.Foldout(showTexture, "Texture");

        if (showTexture)
        {
            if (GUILayout.Button("Texture"))
            {
                customTerrainActivator.Texture();
            }
        }

        showVegetation = EditorGUILayout.Foldout(showVegetation, "Vegetation");

        if (showVegetation)
        {
            if (GUILayout.Button("Vegetation"))
            {
                customTerrainActivator.PlantVegetation();
            }
        }
    }
}

#endif