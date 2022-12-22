using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Utils;

[ExecuteInEditMode]

public class TerrainBlender : MonoBehaviour
{
    [Header("BlendSurroundingTerrain")]
    [SerializeField]
    List<Terrain> terrainList = new List<Terrain>();
    [SerializeField, Range(0, 2)]
    float steepness = 0.55f, blendAmplitude = 1.2f, blendScale = 0.2f, contactEdgeInfluence = 0.4f;

    [SerializeField, Range(0, 10)]
    float stepMultiplier = 1.5f;

    [SerializeField]
    int maxStep = 2;



    List<(float[,] heightmap, Terrain terrain)> heightmapTerrainPairs = new List<(float[,] heightmap, Terrain terrain)>();
    float amplitudeCopied;


    private void OnEnable()
    {
        heightmapTerrainPairs.Clear();

        int heightmapResolution = terrainList[0].terrainData.heightmapResolution;

        //From the terrain list, get their heightmaps and store them in 'allTerrainHeightmap'
        terrainList.ForEach((terrain) =>
        {
            float[,] heightmap = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

            heightmapTerrainPairs.Add((heightmap, terrain));
        });

        amplitudeCopied = blendAmplitude;

        for (int step = 0; step<maxStep; step++)
        {
            //Blend the maps
            for (int i = 0; i < heightmapTerrainPairs.Count; i++)
            {
                for (int j = i + 1; j < heightmapTerrainPairs.Count; j++)
                {
                    BlendEdgePosition blendEdgePosition = GetBlendEdgePosition(heightmapTerrainPairs[i].terrain, heightmapTerrainPairs[j].terrain);

                    /*if (blendEdgePosition != BlendEdgePosition.None)
                        Debug.Log(blendEdgePosition + " " + heightmapTerrainPairs[i].terrain.name + " " + heightmapTerrainPairs[j].terrain.name);
*/
                    if (blendEdgePosition != BlendEdgePosition.None)
                    {
                        var rotatedMaps = RotateHeightmaps(blendEdgePosition, heightmapTerrainPairs[i].heightmap, heightmapTerrainPairs[j].heightmap);

                        var result = BlendHeightMaps(heightmapResolution, rotatedMaps.bottomMap, rotatedMaps.topMap, steepness, amplitudeCopied);

                        var restoredMaps = RotateHeightmapsBack(blendEdgePosition, result.bottomMap, result.topMap);

                        heightmapTerrainPairs[i] = (restoredMaps.map1, heightmapTerrainPairs[i].terrain);
                        heightmapTerrainPairs[j] = (restoredMaps.map2, heightmapTerrainPairs[j].terrain);
                    }
                }

            }

            //Set the heightmap into the terrainDatas
            heightmapTerrainPairs.ForEach((heightmapTerrainPair) =>
            {
                heightmapTerrainPair.terrain.terrainData.SetHeights(0, 0, heightmapTerrainPair.heightmap);
            });

            //With this, in the next iteration, the blend will focus more at the edge
            amplitudeCopied *= stepMultiplier;   
        }
    }



    private (float[,] bottomMap, float[,] topMap) BlendHeightMaps (int resolution, float[,] bottomMap, float[,] topMap, float steepness, float amplitude)
    {
        //Flip the maps
        float[,] flippedBottomMap = FlipMatrix(bottomMap, resolution);
        float[,] flippedTopMap = FlipMatrix(topMap, resolution);

        float[] contactEdge = new float[resolution];
        float[] contactDifference = new float[resolution];


        //Blend those heightmap together
        //Average the heights on the contact edge
        for (int i = 0; i < resolution; i++)
        {
            contactEdge[i] = (bottomMap[resolution - 1, i] + topMap[0, i]) / 2;
            contactDifference[i] = Mathf.Abs(bottomMap[resolution - 1, i] - topMap[0, i]);
        }

        //Blend the bottomMap with the flippedTopMap, with some influence from the middleLine
        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
            {
                /*if (contactDifference[j] == 0)
                    continue;*/

                //The distance to the contact edge is the determining factor of which map have more influence
                float distance = (resolution - i) * 1f / (resolution * blendScale);

                //The larger the distance, the less the map will change
                float lerpFactor = Mathf.Cos(Mathf.Pow(distance, steepness) * Mathf.PI) * amplitude + (1 - amplitude);
                
                //If the distance is larger than 1, the factor becomes the value of that of 1
                if (distance > 1)
                    lerpFactor = Mathf.Cos(Mathf.Pow(1, steepness) * Mathf.PI) * amplitude + (1 - amplitude);

                //Blend the bottomMap with the flippedTopMap, .5f to make sure the contact edge is the average of 2 lines of maps
                float targetHeight = flippedTopMap[i, j];
                float currentHeight = bottomMap[i, j];
                bottomMap[i, j] = Mathf.Lerp(currentHeight, targetHeight, 0.5f * Mathf.Clamp01(lerpFactor));

                //Smooth out the map so that it becomes more even with the contactEdge
                targetHeight = contactEdge[j];
                currentHeight = bottomMap[i, j];
                bottomMap[i, j] = Mathf.Lerp(currentHeight, targetHeight, contactEdgeInfluence * Mathf.Clamp01(lerpFactor));
            }

        //Blend the topMap with the flippedBottomMap, with some influence from the middleLine. Basically the same thing as above
        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
            {
                /*if (contactDifference[j] == 0)
                    continue;*/

                float distance = i * 1f / (resolution * blendScale);

                float lerpFactor = Mathf.Cos(Mathf.Pow(distance, steepness) * Mathf.PI) * amplitude + (1 - amplitude);

                if (distance > 1)
                    lerpFactor = Mathf.Cos(Mathf.Pow(1, steepness) * Mathf.PI) * amplitude + (1 - amplitude);

                float targetHeight = flippedBottomMap[i, j];
                float currentHeight = topMap[i, j];
                topMap[i, j] = Mathf.Lerp(currentHeight, targetHeight, 0.5f * Mathf.Clamp01(lerpFactor));

                targetHeight = contactEdge[j];
                currentHeight = topMap[i, j];
                topMap[i, j] = Mathf.Lerp(currentHeight, targetHeight, contactEdgeInfluence * Mathf.Clamp01(lerpFactor));
            }

        //Set the height of the 2 maps so that it fit the contact line, and those two seemlessly fit together
        for (int i = 0; i < resolution; i++)
        {
            bottomMap[resolution - 1, i] = contactEdge[i];
            topMap[0, i] = contactEdge[i];
        }

        return (bottomMap, topMap);
    }



    private (float[,] bottomMap, float[,] topMap) RotateHeightmaps(BlendEdgePosition blendEdgePosition, float[,] map1, float[,] map2)
    {
        float[,] bottomMap = new float[map1.GetLength(0), map1.GetLength(1)];
        float[,] topMap = new float[map2.GetLength(0), map2.GetLength(1)];

        switch (blendEdgePosition)
        {
            case (BlendEdgePosition.Top):
                bottomMap = map1;
                topMap = map2;
                break;
            case (BlendEdgePosition.Left):
                bottomMap = Rotate2DMatrixAntiClockwise(map1);
                topMap = Rotate2DMatrixAntiClockwise(map2);
                break;
            case (BlendEdgePosition.Right):
                bottomMap = Rotate2DMatrixAntiClockwise(map2);
                topMap = Rotate2DMatrixAntiClockwise(map1);
                break;
            case (BlendEdgePosition.Bottom):
                bottomMap = map2;
                topMap = map1;
                break;
            default:
                break;
        }

        return (bottomMap, topMap);
    }

    private (float[,] map1, float[,] map2) RotateHeightmapsBack(BlendEdgePosition blendEdgePosition, float[,] bottomMap, float[,] topMap)
    {
        float[,] map1 = new float[bottomMap.GetLength(0), bottomMap.GetLength(1)];
        float[,] map2 = new float[topMap.GetLength(0), topMap.GetLength(1)];

        switch (blendEdgePosition)
        {
            case (BlendEdgePosition.Top):
                map1 = bottomMap;
                map2 = topMap;
                break;
            case (BlendEdgePosition.Left):
                map1 = Rotate2DMatrixClockwise(bottomMap);
                map2 = Rotate2DMatrixClockwise(topMap);
                break;
            case (BlendEdgePosition.Right):
                map1 = Rotate2DMatrixClockwise(topMap);
                map2 = Rotate2DMatrixClockwise(bottomMap);
                break;
            case (BlendEdgePosition.Bottom):
                map1 = topMap;
                map2 = bottomMap;
                break;
            default:
                break;
        }

        return (map1, map2);
    }

    private BlendEdgePosition GetBlendEdgePosition(Terrain terrain1, Terrain terrain2)
    {
        Vector3 terrain1Pos = terrain1.gameObject.transform.position;
        Vector3 terrain2Pos = terrain2.gameObject.transform.position;

        float terrainSize = terrain1.terrainData.size.x;
        float distance = (terrain2Pos - terrain1Pos).magnitude;

        //If the distance is (mostly) equal to the terrainSize, the 2 terrains are next to each other
        if (Mathf.Abs(distance - terrainSize) <= Mathf.Epsilon)
        {
            //Decide if the contact edge is on the top, bottom, left, or right of the terrain1
            if (terrain2Pos.z > terrain1Pos.z)
                return BlendEdgePosition.Top;
            else if (terrain2Pos.z < terrain1Pos.z)
                return BlendEdgePosition.Bottom;
            else if (terrain2Pos.x < terrain1Pos.x)
                return BlendEdgePosition.Left;
            else if (terrain2Pos.x > terrain1Pos.x)
                return BlendEdgePosition.Right;
            else return BlendEdgePosition.None;
        }
        else return BlendEdgePosition.None;
    }
}

enum BlendEdgePosition
{
    Top,
    Bottom,
    Left,
    Right,
    None
}
