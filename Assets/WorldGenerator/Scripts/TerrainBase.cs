using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Constants;
using static Utils;

[RequireComponent(typeof(Terrain))]
public class TerrainBase : MonoBehaviour
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
    private int MPDRoughness = 3;
    [SerializeField]
    private float MPDHeightMin = 0.1f, MPDHeightMax = 0.7f, MPDHeightDampenerPower = 2, MPDHeightScale = 0.5f;

    [Header("Texturing")]
    [SerializeField]
    private List<SplatHeightData> splatHeights = new List<SplatHeightData>();

    [Header("Vegetation")]
    [SerializeField, Range (0,10000)]
    private int maxTrees = 5000;
    [SerializeField, Range (0, 50)]
    private int treeSpacing = 5;
    [SerializeField]
    private List<VegetationData> vegetation = new List<VegetationData>();



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
                        float height = peakHeight * VoronoiSmooth(distance, smoothness, voronoiData.Amplitude);

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
        Smooth(smoothRadius);
    }

    public void Smooth(int smoothRadius)
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
        float heightMin = MPDHeightMin;
        float heightMax = MPDHeightMax;
        float heightDampener = (float)Mathf.Pow(MPDHeightDampenerPower, -1 * MPDRoughness);


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
                generatedHeightMap[i, j] *= MPDHeightScale;
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



    public void GenerateTexture()
    {
        Initialize();

        TerrainLayer[] terrainLayers = new TerrainLayer[splatHeights.Count];
        int splatIndex = 0;

        DeleteAssetsInFolder(GENERATED_ASSET_FOLDER_PATH);
        foreach (SplatHeightData sh in splatHeights)
        {
            terrainLayers[splatIndex] = new TerrainLayer();

            terrainLayers[splatIndex].tileOffset = sh.tileOffset;
            terrainLayers[splatIndex].tileSize = sh.tileSize;

            terrainLayers[splatIndex].diffuseTexture = sh.texture;
            terrainLayers[splatIndex].diffuseTexture.Apply(true);

            AssetDatabase.CreateAsset(terrainLayers[splatIndex], GENERATED_ASSET_FOLDER_PATH + "/" + sh.texture.name);
            terrainLayers[splatIndex].name = sh.texture.name;

            splatIndex++;
        }

        terrainData.SetTerrainLayersRegisterUndo(terrainLayers, nameof(TerrainLayerUndo));

        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float[] splat = new float[terrainData.alphamapLayers];
                for (int i = 0; i < splatHeights.Count; i++)
                {
                    float noise = Mathf.PerlinNoise(x * splatHeights[i].splatNoiseXScale, y * splatHeights[i].splatNoiseYScale)
                                       * splatHeights[i].splatNoiseZScaler;
                    float offset = splatHeights[i].splatOffset + noise;
                    float startHeight = splatHeights[i].minHeight - offset;
                    float stopHeight = splatHeights[i].maxHeight + offset;

                    float steepness = terrainData.GetSteepness(y / (float)terrainData.alphamapHeight,x / (float)terrainData.alphamapWidth);

                    if ((heightMap[x, y] >= startHeight && heightMap[x, y] <= stopHeight) &&
                        (steepness >= splatHeights[i].minSlope && steepness <= splatHeights[i].maxSlope))
                    {
                        splat[i] = 1;
                    }
                }
                NormalizeVector(splat);
                for (int j = 0; j < splatHeights.Count; j++)
                {
                    splatmapData[x, y, j] = splat[j];
                }
            }
        }
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    public void PlantVegetation()
    {
        Initialize();

        TreePrototype[] newTreePrototypes = new TreePrototype[vegetation.Count];

        for (int treeIndex = 0; treeIndex < vegetation.Count; treeIndex++)
        {
            newTreePrototypes[treeIndex] = new TreePrototype();
            newTreePrototypes[treeIndex].prefab = vegetation[treeIndex].Mesh;
        }
        terrainData.treePrototypes = newTreePrototypes;

        List<TreeInstance> allVegetation = new List<TreeInstance>();
        
        for (int x = 0; x < terrainData.size.x; x += treeSpacing)
        {
            for (int z = 0; z < terrainData.size.z; z += treeSpacing)
            {
                for (int treeIndex = 0; treeIndex < terrainData.treePrototypes.Length; treeIndex++)
                {
                    float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;
                    float steepness = terrainData.GetSteepness(x / terrainData.size.x, z / terrainData.size.z);

                    bool doesFitInDensity = Random.Range(0.0f, 1.0f) > vegetation[treeIndex].Density;
                    bool doesFitHeight = (thisHeight >= vegetation[treeIndex].Height.min && thisHeight <= vegetation[treeIndex].Height.max);
                    bool doesFitSteepness = (steepness >= vegetation[treeIndex].Slope.min && steepness <= vegetation[treeIndex].Slope.max);

                    if ( doesFitInDensity && doesFitHeight && doesFitSteepness)
                    {
                        TreeInstance instance = new TreeInstance();

                        float xPos = (x + Random.Range(-vegetation[treeIndex].RandomStrength, vegetation[treeIndex].RandomStrength)) / terrainData.size.x;
                        float zPos = (z + Random.Range(-vegetation[treeIndex].RandomStrength, vegetation[treeIndex].RandomStrength)) / terrainData.size.z;
                        float yPos = terrainData.GetHeight(x, z) / terrainData.size.y;

                        xPos = xPos * terrainData.size.x / terrainData.alphamapWidth;
                        zPos = zPos * terrainData.size.z / terrainData.alphamapHeight;

                        instance.position = new Vector3(xPos, yPos, zPos);

                        Vector3 treeWorldPos = new Vector3(instance.position.x * terrainData.size.x,
                                                            instance.position.y * terrainData.size.y,
                                                            instance.position.z * terrainData.size.z) + this.transform.position;

                        RaycastHit hit;
                        int layerMask = LayerMask.NameToLayer(TERRAIN_LAYER_NAME);

                        if (Physics.Raycast(treeWorldPos + new Vector3(0, 20, 0), Vector3.down, out hit, 200) ||
                            Physics.Raycast(treeWorldPos - new Vector3(0, 20, 0), Vector3.up, out hit, 200))
                        {
                            if (hit.transform.gameObject.layer == layerMask)
                            {
                                float treeHeight = (hit.point.y - transform.position.y) / terrainData.size.y;

                                instance.position = new Vector3(instance.position.x, treeHeight, instance.position.z);

                                instance.rotation = Random.Range(0, 360);
                                instance.prototypeIndex = treeIndex;
                                instance.color = Color.Lerp(vegetation[treeIndex].Colors.color1, vegetation[treeIndex].Colors.color2, Random.Range(0.0f, 1.0f));
                                instance.lightmapColor = vegetation[treeIndex].Colors.lightmap;
                                float scale = Random.Range(vegetation[treeIndex].Scale.min, vegetation[treeIndex].Scale.max);
                                instance.heightScale = scale;
                                instance.widthScale = scale;

                                allVegetation.Add(instance);
                                if (allVegetation.Count >= maxTrees) goto FINISH;
                            }
                        }
                    }
                }
            }
        }

    FINISH:
        terrainData.treeInstances = allVegetation.ToArray();
    }



    void NormalizeVector(float[] v)
    {
        float total = 0;
        for (int i = 0; i < v.Length; i++)
        {
            total += v[i];
        }

        for (int i = 0; i < v.Length; i++)
        {
            v[i] /= total;
        }
    }

    public static float[,] InstantiateHeightMap(bool shouldReset, TerrainData terrainData)
    {
        float[,] heightMap = new float[0, 0];

        if (!shouldReset)
            heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        else if (shouldReset)
            heightMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        return heightMap;
    }

    private void DeleteAssetsInFolder(string folderPath)
    {
        string[] foundAssets = AssetDatabase.FindAssets("", new string[1] { folderPath });

        if (foundAssets.Length == 0)
        {
            return; //Do nothing

        }

        foreach (var asset in foundAssets)
        {
            var path = AssetDatabase.GUIDToAssetPath(asset);
            AssetDatabase.DeleteAsset(path);
        }
    }

    private void TerrainLayerUndo()
    {
        //Not yet implemented, only is used for the SetTerrainLayer function.
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

[System.Serializable]
public class SplatHeightData
{
    public Texture2D texture = null;
    public float minHeight = 0.1f, maxHeight = 0.2f;
    public float minSlope = 0, maxSlope = 1.5f;
    public Vector2 tileOffset = new Vector2(0, 0);
    public Vector2 tileSize = new Vector2(50, 50);
    public float splatOffset = 0.1f;
    public float splatNoiseXScale = 0.01f, splatNoiseYScale = 0.01f, splatNoiseZScaler = 0.1f;
}

[System.Serializable]
public class VegetationData
{
    [SerializeField]
    private GameObject mesh;

    [SerializeField, Range (0,1)]
    private float minHeight = 0.1f, maxHeight = 0.2f;

    [SerializeField, Range (0,90)]
    private float minSlope = 0, maxSlope = 90;

    [SerializeField, Range (0, 100)]
    private float minScale = 0.5f, maxScale = 1.0f;

    [SerializeField]
    private Color colour1 = Color.white, colour2 = Color.white, lightmapColor = Color.white;

    [SerializeField]
    private float density = 0.5f, randomStrength = 10f;



    public GameObject Mesh { get => mesh; set => mesh = value; }
    public (float min, float max) Height { get => (minHeight, maxHeight); set => (minHeight, maxHeight) = value; }
    public (float min, float max) Slope { get => (minSlope, maxSlope); set => (minSlope, maxSlope) = value; }
    public (float min, float max) Scale { get => (minScale, maxScale); set => (minScale, maxScale) = value; }
    public (Color color1, Color color2, Color lightmap) Colors
    {
        get => (colour1, colour2, lightmapColor);
        set => (colour1, colour2, lightmapColor) = value;
    }
    public float Density { get => density; set => density = value; }
    public float RandomStrength { get => randomStrength; set => randomStrength = value; }
}
