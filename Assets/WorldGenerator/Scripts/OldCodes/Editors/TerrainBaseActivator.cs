using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TerrainBase))]
[ExecuteInEditMode]
public class TerrainBaseActivator : MonoBehaviour
{
    TerrainBase customTerrain;

    private void Awake()
    {
        customTerrain = GetComponent<TerrainBase>();
    }

    public void ResetHeight()
    {
        customTerrain.ResetHeight();
    }

    internal void PerlinNoise()
    {
        customTerrain.PerlinNoise();
    }

    internal void VoronoiPeaks()
    {
        customTerrain.Voronoi();
    }

    internal void Smooth()
    {
        customTerrain.Smooth();
    }

    internal void MidpointDisplacement()
    {
        customTerrain.MidPointDisplacement();
    }

    internal void Texture()
    {
        //customTerrain.GenerateTexture();
    }

    internal void PlantVegetation()
    {
        customTerrain.PlantVegetation();
    }
}
