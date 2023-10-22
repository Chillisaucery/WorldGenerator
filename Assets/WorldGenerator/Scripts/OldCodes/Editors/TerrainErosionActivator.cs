using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TerrainErosion))]
[ExecuteInEditMode]
public class TerrainErosionActivator : MonoBehaviour
{
    TerrainErosion terrainErosion = null;


    private void OnEnable()
    {
        if (terrainErosion == null)
            terrainErosion = GetComponent<TerrainErosion>();    
    }

    public void Erode()
    {
        terrainErosion.Erode();
    }
}
