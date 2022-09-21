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

    public void RandomHeight()
    {
        customTerrain.RandomHeight();
    }

    public void ResetHeight()
    {
        customTerrain.ResetHeight();
    }

    internal void PerlinNoise()
    {
        customTerrain.PerlinNoise();
    }
}
