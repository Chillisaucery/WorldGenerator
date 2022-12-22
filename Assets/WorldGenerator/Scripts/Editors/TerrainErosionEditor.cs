using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainErosionActivator))]
//[CanEditMultipleObjects]
public class TerrainErosionEditor : Editor
{
    bool showErode = false;

    public override void OnInspectorGUI()
    {
        TerrainErosionActivator customTerrainActivator = (TerrainErosionActivator)target;

        if (GUILayout.Button("Erode"))
        {
            customTerrainActivator.Erode();
        }


        

        /*
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
        }*/
    }
}
