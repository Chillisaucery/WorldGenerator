using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Constants;

[ExecuteInEditMode]
public class TerrainBezierColor : MonoBehaviour
{
    //Static variables
    const int RED=0, BLUE=1, PURPLE=2, GREY=3;

    [SerializeField]
    TerrainBezier terrainBezier = null;

    [SerializeField]
    Terrain terrain = null;

    [SerializeField, Range(0,1), Tooltip("0 for RED, 1 for BLUE")]
    int defaultSplatIndex = 0;

    public void GenerateTexture(bool shouldMerge)
    {
        TerrainData terrainData = terrain.terrainData;

        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        if (!shouldMerge)
            GenerateTexture(terrainData, ref splatmapData, defaultSplatIndex);
        else
            GenerateTexture(terrainData, ref splatmapData, PURPLE);

        FillGrey(terrainData, ref splatmapData, GREY);
    }

    private void GenerateTexture(TerrainData terrainData, ref float[,,] splatmapData, int splatIndex)
    {
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                splatmapData[x, y, splatIndex] = 1;
            }
        }
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    private void FillGrey(TerrainData terrainData, ref float[,,] splatmapData, int greySplatIndex)
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                if (heightMap[x, y] <= 0.01f)
                {
                    splatmapData[x, y, RED] = 0;
                    splatmapData[x, y, BLUE] = 0;
                    splatmapData[x, y, PURPLE] = 0;

                    splatmapData[x, y, greySplatIndex] = 1;
                }
                else
                    splatmapData[x, y, greySplatIndex] = 0;
            }
        }
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
}
