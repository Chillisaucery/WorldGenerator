using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

[RequireComponent(typeof(Terrain))]
public class CustomTerrain : MonoBehaviour
{
    //Serialize Field
    [Header("Terrain setting"), SerializeField]
    private bool shouldReset = false;



    [Header("Perlin noise")]
    [SerializeField]
    private bool shouldRandomPerlin = false;
    [SerializeField]
    private List<PerlinNoiseData> perlinNoiseDataList;

    [Header("Perlin noise")]
    [SerializeField]
    private bool shouldRandomVoronoi = false;
    [SerializeField]
    private VoronoiData voronoiData;

    [Header("Smooth")]
    [SerializeField, Range (1, 10), Tooltip("Blur radius, equivalent to 1 pixels on the height map")]
    private int smoothRadius = 2;

    [Header("Mid Point Displacement")]
    [SerializeField]
    private int MPDroughness = 3;
    [SerializeField]
    private float MPDheightMin = 0.1f, MPDheightMax = 0.7f, MPDheightDampenerPower = 2, MPDheightScale = 0.5f;



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



    public void PerlinNoise()
    {
        Initialize();

        float[,] heightMap = InstantiateHeightMap(shouldReset, terrainData);

        //Find the the min fBM value and move the whole terrain down
        float minHeight = 1;

        //Generate the height
        foreach (PerlinNoiseData perlinNoiseData in perlinNoiseDataList)
        {
            (int x, int y) offset = (perlinNoiseData.XOffset, perlinNoiseData.YOffset);

            //Random Offset if necessary
            if (shouldRandomPerlin)
            {
                offset = (Random.Range(0, MAX_PERLIN_OFFSET), Random.Range(0, MAX_PERLIN_OFFSET));
            }

            //Go throught all the point on the height map
            for (int i = 0; i < terrainData.heightmapResolution; i++)
            {
                for (int j = 0; j < terrainData.heightmapResolution; j++)
                {
                    heightMap[i, j] += Utils.fBM((i + offset.x) * perlinNoiseData.XScale,
                            (j + offset.y) * perlinNoiseData.YScale,
                            perlinNoiseData.Octave,
                            perlinNoiseData.Persistance, perlinNoiseData.Lacunarity)
                    * perlinNoiseData.ZScale;

                    //After generating, check if the value is the min value
                    if (minHeight > heightMap[i, j])
                        minHeight = heightMap[i, j];
                }
            }
        }
 

        //Move it down so that the lowest point meet the ground
        for (int i = 0; i < terrainData.heightmapResolution; i++)
        {
            for (int j = 0; j < terrainData.heightmapResolution; j++)
            {
                heightMap[i, j] -= minHeight;
            }
        }



        terrainData.SetHeights(0, 0, heightMap);
    }

    public void Voronoi()
    {
        Initialize();

        float[,] heightMap = InstantiateHeightMap(shouldReset, terrainData);

        //Find the the min fBM value and move the whole terrain down
        float minHeightGenerated = 1;

        //Loop through each peak
        for (int peak = 0; peak < voronoiData.PeakCount; peak++)
        {
            float peakHeight = Random.Range(voronoiData.MinHeight, voronoiData.MaxHeight);
            float scale = Random.Range(voronoiData.Scale.min, voronoiData.Scale.max);
            float smoothness = Random.Range(voronoiData.Smoothness.min, voronoiData.Smoothness.max);

            Vector3 peakPos = new Vector3(Random.Range(0, terrainData.heightmapResolution), peakHeight, Random.Range(0, terrainData.heightmapResolution));

            //Only set the height if the new peak is higher than the current terrain position
            if (heightMap[(int)peakPos.x, (int)peakPos.z] < peakPos.y)
                heightMap[(int)peakPos.x, (int)peakPos.z] = peakPos.y;
            else
                continue;

            //Initialize some variables for distance
            Vector2 peakLocation = new Vector2(peakPos.x, peakPos.z);
            float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(terrainData.heightmapResolution, terrainData.heightmapResolution));

            //Loop through the height map
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                for (int y = 0; y < terrainData.heightmapResolution; y++)
                {
                    if (!(x == peakPos.x && y == peakPos.z))    //Will not do anything to the peak itself
                    {
                        float distance = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance * scale;
                        float height = 0;

                        if (distance<=1 && distance >=0)
                        {
                            height = peakHeight * ((Mathf.Cos(Mathf.Pow(distance, smoothness) * Mathf.PI) * voronoiData.Amplitude
                                        + (1 - voronoiData.Amplitude)));
                        }

                        if (heightMap[x, y] < height)
                            heightMap[x, y] = height;
                    }

                    //After generating, check if the value is the min value
                    if (minHeightGenerated > heightMap[y, x])
                        minHeightGenerated = heightMap[y, x];
                }
            }
        }

        //Move the terrain down according to the min height generated, the min height, and the max height
        for (int i = 0; i < terrainData.heightmapResolution; i++)
        {
            for (int j = 0; j < terrainData.heightmapResolution; j++)
            {
                heightMap[i, j] -= minHeightGenerated;
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }
    
    public void Smooth()
    {
        Initialize();

        float[,] heightMap = InstantiateHeightMap(false, terrainData);

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                float avgHeight = heightMap[x, y];
                List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, y), smoothRadius, terrainData.heightmapResolution);
                foreach (Vector2 n in neighbours)
                {
                    avgHeight += heightMap[(int)n.x, (int)n.y];
                }

                heightMap[x, y] = avgHeight / ((float)neighbours.Count + 1);
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    public void MidPointDisplacement()
    {
        Initialize();

        float[,] generatedHeightMap = InstantiateHeightMap(true, terrainData);
        int width = terrainData.heightmapResolution - 1;
        int squareSize = width;
        float heightMin = MPDheightMin;
        float heightMax = MPDheightMax;
        float heightDampener = (float)Mathf.Pow(MPDheightDampenerPower, -1 * MPDroughness);


        int cornerX, cornerY;
        int midX, midY;
        int pmidXL, pmidXR, pmidYU, pmidYD;

        while (squareSize > 0)
        {
            //Diamond step
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    generatedHeightMap[midX, midY] = (generatedHeightMap[x, y] + generatedHeightMap[cornerX, y] + generatedHeightMap[x, cornerY] + generatedHeightMap[cornerX, cornerY]) / 4.0f 
                                                + Random.Range(heightMin, heightMax);
                }
            }

            //Square step
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {

                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    pmidXR = midX + squareSize;
                    pmidYU = midY + squareSize;
                    pmidXL = midX - squareSize;
                    pmidYD = midY - squareSize;


                    //Calculate the square value for the bottom side  
                    try
                    {
                        generatedHeightMap[midX, y] = (generatedHeightMap[midX, midY] + generatedHeightMap[x, y] + generatedHeightMap[midX, pmidYD] + generatedHeightMap[cornerX, y]) / 4.0f
                        + Random.Range(heightMin, heightMax);
                    }
                    catch { }

                    //Calculate the square value for the top side   
                    try
                    {
                        generatedHeightMap[midX, cornerY] = (generatedHeightMap[x, cornerY] + generatedHeightMap[midX, midY] + generatedHeightMap[cornerX, cornerY] + generatedHeightMap[midX, pmidYU]) / 4.0f
                            + Random.Range(heightMin, heightMax);
                    }
                    catch { }

                    //Calculate the square value for the left side   
                    try
                    {
                        generatedHeightMap[x, midY] = (generatedHeightMap[x, y] + generatedHeightMap[pmidXL, midY] + generatedHeightMap[x, cornerY] + generatedHeightMap[midX, midY]) / 4.0f
                            + Random.Range(heightMin, heightMax);
                    }
                    catch { }

                    //Calculate the square value for the right side   
                    try
                    {
                        generatedHeightMap[cornerX, midY] = (generatedHeightMap[midX, y] + generatedHeightMap[midX, midY] + generatedHeightMap[cornerX, cornerY] + generatedHeightMap[pmidXR, midY]) / 4.0f
                            + Random.Range(heightMin, heightMax);
                    }
                    catch { }

                }
            }

            squareSize = (int)(squareSize / 2.0f);
            heightMin *= heightDampener;
            heightMax *= heightDampener;
        }

        float[,] resultHeightMap = InstantiateHeightMap(shouldReset, terrainData);

        for (int i = 0; i < terrainData.heightmapResolution; i++)
        {
            for (int j = 0; j < terrainData.heightmapResolution; j++)
            {
                generatedHeightMap[i, j] *= MPDheightScale;
                resultHeightMap[i,j] += generatedHeightMap[i, j];
            }
        }

        terrainData.SetHeights(0, 0, resultHeightMap);
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

    List<Vector2> GenerateNeighbours(Vector2 pos, int radius, int resolution)
    {
        List<Vector2> neighbours = new List<Vector2>();

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (!(x == 0 && y == 0))
                {
                    Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, resolution - 1),
                                                Mathf.Clamp(pos.y + y, 0, resolution - 1));
                    if (!neighbours.Contains(nPos))
                        neighbours.Add(nPos);
                }
            }
        }
        return neighbours;
    }
}

[System.Serializable]
public class PerlinNoiseData
{
    [SerializeField, Range(0, 1)]
    private float xScale = 0.05f, yScale = 0.05f, zScale = 1f;

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

[System.Serializable]
public class VoronoiData
{
    [SerializeField, Range(0, 10)]
    private int peakCount = 5;

    [SerializeField, Range(0, 1)]
    private float maxHeight = 1f, minHeight = 0.5f;

    [SerializeField, Range(0, 1)]
    private float amplitude = 0.5f;

    [SerializeField, Range(0, 10)]
    private float maxSmoothness = 2, minSmoothness = 0.5f, maxScale = 10, minScale =3;

    
    public (float min, float max) Smoothness { get => (minSmoothness, maxSmoothness); private set => (minSmoothness, maxSmoothness) = value; }
    public float Amplitude { get => amplitude; private set => amplitude = value; }
    public int PeakCount { get => peakCount; private set => peakCount = value; }
    public float MaxHeight { get => maxHeight; private set => maxHeight = value; }
    public float MinHeight { get => minHeight; private set => minHeight = value; }
    public (float min, float max) Scale { get => (minScale, maxScale); private set => (minScale, maxScale) = value; }
}
