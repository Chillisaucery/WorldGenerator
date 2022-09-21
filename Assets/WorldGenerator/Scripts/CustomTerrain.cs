using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class CustomTerrain : MonoBehaviour
{
    //Serialize Field
    [Header("Terrain setting"), SerializeField]
    private bool shouldReset = false;

    [Header("Random height")]

    [SerializeField, Range(0, 1)]
    private float minHeight = 0;

    [SerializeField, Range(0, 1)]
    private float maxHeight = 0.1f;


    [Header("Perlin noise")]
    [SerializeField]
    private List<PerlinNoiseData> perlinNoiseDataList;
    

    
    //Non-Serialized cached object
    Terrain terrain = null;
    TerrainData terrainData = null;

    private void Initialize()
    {
        if (terrainData == null)
        {
            terrain = GetComponent<Terrain>();
            terrainData = terrain.terrainData;
        }
    }

    public void RandomHeight()
    {
        Initialize();

        float[,] heightMap = InstantiateHeightMap(shouldReset, terrainData);

        for (int i = 0; i < terrainData.heightmapResolution; i++)
        {
            for (int j = 0; j < terrainData.heightmapResolution; j++)
            {
                heightMap[i,j] += Random.Range(minHeight, maxHeight);
            }
        }

        terrainData.SetHeights(0,0,heightMap);  
    }

    public void PerlinNoise()
    {
        Initialize();

        float[,] heightMap = InstantiateHeightMap(shouldReset, terrainData);

        for (int i = 0; i < terrainData.heightmapResolution; i++)
        {
            for (int j = 0; j < terrainData.heightmapResolution; j++)
            {
                foreach (PerlinNoiseData perlinNoiseData in perlinNoiseDataList)
                {
                    heightMap[i, j] += Utils.fBM((i + perlinNoiseData.XOffset) * perlinNoiseData.XScale,
                            (j + perlinNoiseData.YOffset) * perlinNoiseData.YScale,
                            perlinNoiseData.Octave,
                            perlinNoiseData.Persistance, perlinNoiseData.Lacunarity)
                    * perlinNoiseData.ZScale;
                }

            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    public void ResetHeight()
    {
        Initialize();

        float[,] heightMap = InstantiateHeightMap(shouldReset, terrainData);

        for (int i = 0; i < terrainData.heightmapResolution; i++)
        {
            for (int j = 0; j < terrainData.heightmapResolution; j++)
            {
                heightMap[i, j] =0;
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    public static float [,] InstantiateHeightMap(bool shouldReset, TerrainData terrainData)
    {
        float[,] heightMap = new float[0,0];

        if (!shouldReset)
            heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        else if (shouldReset)
            heightMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        return heightMap;
    }
}

[System.Serializable]
public class PerlinNoiseData
{
    [SerializeField, Range(0, 1)]
    private float xScale = 0.05f;

    [SerializeField, Range(0, 1)]
    private float yScale = 0.05f, zScale = 1f;

    [SerializeField, Range(0, 1000), Tooltip("Offset for the Perlin noise")]
    private int xOffset = 0, yOffset = 0;

    [SerializeField, Range(0, 10), Tooltip("Number of loop to perform Fractal Brownian Motion")]
    private int octave = 3;

    [SerializeField, Range(0, 10), Tooltip("During each loop: Persistance is the amplitude multiplication, and lacunarity is the frequency multiplication")]
    private float persistance = 0.5f, lacunarity = 2f;

    public float XScale { get => xScale; private set => xScale = value; }
    public float YScale { get => yScale; private set => yScale = value; }
    public float ZScale { get => zScale; private set => zScale = value; }
    public int XOffset { get => xOffset; private set => xOffset = value; }
    public int YOffset { get => yOffset; private set => yOffset = value; }
    public int Octave { get => octave; private set => octave = value; }
    public float Persistance { get => persistance; private set => persistance = value; }
    public float Lacunarity { get => lacunarity; private set => lacunarity = value; }
}
