using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CustomTerrain))]
[ExecuteInEditMode]
public class CustomTerrainActivator : MonoBehaviour
{
    CustomTerrain customTerrain;

    private void Awake()
    {
        customTerrain = GetComponent<CustomTerrain>();
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
}
