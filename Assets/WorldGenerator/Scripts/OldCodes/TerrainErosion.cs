using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ErosionData;
using static Constants;
using static Utils;
using System;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Terrain))]
[RequireComponent(typeof(TerrainBase))]
public class TerrainErosion : MonoBehaviour
{
    [SerializeField]
    private List<ErosionData> erosionDataList = new List<ErosionData>();



    private ErosionData currentErosionData;
    private TerrainData terrainData;
    private TerrainBase customTerrainBase;
    private int mapRes = 0;

    public void Erode()
    {
        currentErosionData = erosionDataList[0];
        terrainData = GetComponent<Terrain>().terrainData;
        customTerrainBase = GetComponent<TerrainBase>();
        mapRes = terrainData.heightmapResolution;

        

        ErosionType erosionType = currentErosionData.erosionType;
         
        if (erosionType == ErosionType.Rain)
            Rain();
        else if (erosionType == ErosionType.Tidal)
            Tidal();
        else if (erosionType == ErosionType.Thermal)
            Thermal();
        else if (erosionType == ErosionType.River)
            River();
        else if (erosionType == ErosionType.Wind)
            Wind();
        else if (erosionType == ErosionType.Canyon)
            DigCanyon();

        int smoothAmount = currentErosionData.erosionSmoothAmount;
        
        //customTerrainBase.Smooth(smoothAmount);
    }

    enum TravelDirection { UpLeft, UpRight, DownLeft, DownRight };
    void Thermal()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, mapRes, mapRes);

        float startTime = Time.time;

        for (int i = 0; i < currentErosionData.thermalIteration; i++)
            for (int x = 0; x < mapRes; x++)
                for (int y = 0; y < mapRes; y++)
                    ThermalErodeAtPoint(heightMap, x, y, currentErosionData.thermalStrength, currentErosionData.thermalThreshold);

        Debug.Log("Time: " + (Time.time - startTime));

        terrainData.SetHeights(0, 0, heightMap);
    }


    private void ThermalErodeAtPoint(float[,] heightMap, int x, int y, float strength, float threshold)
    {
        List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, y), 1, 2, mapRes);

        foreach (Vector2 neighbour in neighbours)
        {
            //If this value is larger than 0, it means the neighbour is lower than the current location
            float heightDifference = heightMap[x, y] - heightMap[(int)neighbour.x, (int)neighbour.y];

            //Transport the material to the lower location
            if (heightDifference > threshold)
            {
                float transportAmount = heightDifference * strength;
                
                //The map can be lowered by the amount of at most 0.5 heightDifference
                float minNewHeight = heightMap[x, y] - heightDifference * 0.5f;

                heightMap[x, y] = Mathf.Clamp(heightMap[x,y] - transportAmount, minNewHeight, 1);
                heightMap[(int)neighbour.x, (int)neighbour.y] += transportAmount;
            }
        }
    }

    int crawCount = 0;
    float[,] tempHeightMap;
    public void DigCanyon()
    {
        float digStrength = currentErosionData.canyonStrength;
        int digRange = (int) (currentErosionData.canyonBankSize * mapRes);
        float stepMagnitude = currentErosionData.canyonStep;
        float minDepth = 0.2f;

        float[,] heightMap = terrainData.GetHeights(0, 0, mapRes, mapRes);
        List<Vector2Int> canyonDigPoints = GenerateCanyonPoints(stepMagnitude, currentErosionData.canyonDirectionJiggle);

        foreach (Vector2Int dig in canyonDigPoints)
        {
            float pitHeight = Mathf.Clamp01(heightMap[dig.x, dig.y] - digStrength);

            for (int x = Mathf.Max(dig.x - digRange, 0); x < Mathf.Min(dig.x + digRange, mapRes); x++)
                for (int y = Mathf.Max(dig.y - digRange, 0); y < Mathf.Min(dig.y + digRange, mapRes); y++)
                {
                    float angle = Vector2.SignedAngle(dig - new Vector2(x, y), Vector2.right) + 180;

                    float distance = (Vector2.Distance(new Vector2(x, y), dig) / digRange);

                    float lerpFactor = SteepVoronoiSmooth(distance) + (Mathf.PerlinNoise(angle, 0) * currentErosionData.canyonBankJiggle);
                    float targetHeight = Mathf.Lerp(heightMap[x,y], pitHeight, Mathf.Clamp01(lerpFactor));
;
                    heightMap[x, y] = Mathf.Min(1, targetHeight);
                }    
        }

        /*tempHeightMap = terrainData.GetHeights(0, 0, mapRes, mapRes);
        int cx = 1;
        int cy = Random.Range(10, mapRes - 10);

        while (cy >= 0 && cy < mapRes && cx > 0 && cx < mapRes)
        {
            CanyonCrawler(cx, cy, tempHeightMap[cx, cy] - digStrength, 0.1f, minDepth);
            cx = cx + Random.Range(1, 4);
            cy = cy + Random.Range(-2, 3);
        }*/

        //Debug.Log("Digged " + canyonDigPoints[0] + " " + canyonDigPoints[canyonDigPoints.Count-1] + " " + canyonDigPoints.Count);
        terrainData.SetHeights(0, 0, heightMap);
    }

    private List<Vector2Int> GenerateCanyonPoints(float stepMagnitude, float jiggle)
    {
        Vector2 startDigPoint = new Vector2(Random.Range(0, mapRes), Random.Range(0, mapRes));
        Vector2 endDigPoint = new Vector2(Random.Range(0, mapRes), Random.Range(0, mapRes));

        //Find the end point that would be far away from the start point
        while (Vector2.Distance(startDigPoint, endDigPoint) < currentErosionData.canyonMinDistance * mapRes)
            endDigPoint = new Vector2Int(Random.Range(0, mapRes), Random.Range(0, mapRes));

        //Initiate the variables
        List<Vector2> canyonDigPoints = new List<Vector2>();
        canyonDigPoints.Add(startDigPoint);

        Vector2 currentDigPoint = startDigPoint;

        //Move the current dig point close to the end point
        while (Vector2.Distance(currentDigPoint, endDigPoint) > stepMagnitude * mapRes)
        {
            Vector2 randomStep = new Vector2(Random.Range(-jiggle, jiggle), Random.Range(-jiggle, jiggle));
            Vector2 directionStep = (endDigPoint - currentDigPoint).normalized;
            Vector2 step = (randomStep + directionStep).normalized * stepMagnitude * mapRes;

            currentDigPoint = currentDigPoint + step;
            canyonDigPoints.Add(currentDigPoint);
        }

        canyonDigPoints.Add(endDigPoint);

        return ConvertToVector2Ints(canyonDigPoints);
    }

    void CanyonCrawler(int x, int y, float height, float slope, float maxDepth)
    {
        if (x < 0 || x >= mapRes) return; //off x range of map
        if (y < 0 || y >= mapRes) return; //off y range of map
        if (height <= maxDepth) return; //if hit lowest level
        if (tempHeightMap[x, y] <= height) return; //if run into lower elevation

        crawCount++;
        tempHeightMap[x, y] = height;

        CanyonCrawler(x + 1, y, height + Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x - 1, y, height + Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x + 1, y + 1, height + Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x - 1, y + 1, height + Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x, y - 1, height + Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x, y + 1, height + Random.Range(slope, slope + 0.01f), slope, maxDepth);
    }

    void Rain()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0,
                                                    mapRes,
                                                    mapRes);
        for (int i = 0; i < currentErosionData.droplets; i++)
        {
            heightMap[Random.Range(0, mapRes),
                      Random.Range(0, mapRes)]
                        -= currentErosionData.thermalStrength;
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    

    void Tidal()
    {
        /*float[,] heightMap = terrainData.GetHeights(0, 0,
                                    mapRes,
                                    mapRes);
        for (int y = 0; y < mapRes; y++)
        {
            for (int x = 0; x < mapRes; x++)
            {
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neighbours = GenerateNeighbours(thisLocation, mapRes, mapRes);
                foreach (Vector2 n in neighbours)
                {
                    if (heightMap[x, y] < currentErosionData.waterHeight && heightMap[(int)n.x, (int)n.y] > waterHeight)
                    {
                        heightMap[x, y] = waterHeight;
                        heightMap[(int)n.x, (int)n.y] = waterHeight;
                    }
                }
            }
        }

        terrainData.SetHeights(0, 0, heightMap);*/
    }


    void River()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0,
                                            mapRes,
                                            mapRes);
        float[,] erosionMap = new float[mapRes, mapRes];

        for (int i = 0; i < currentErosionData.droplets; i++)
        {
            Vector2 dropletPosition = new Vector2(Random.Range(0, mapRes), Random.Range(0, mapRes));

            erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] = currentErosionData.thermalStrength;
            for (int j = 0; j < currentErosionData.springsPerRiver; j++)
            {
                erosionMap = RunRiver(dropletPosition, heightMap, erosionMap,
                                   mapRes,
                                   mapRes);
            }
        }

        for (int y = 0; y < mapRes; y++)
        {
            for (int x = 0; x < mapRes; x++)
            {
                if (erosionMap[x, y] > 0)
                {
                    heightMap[x, y] -= erosionMap[x, y];
                }
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    float[,] RunRiver(Vector2 dropletPosition, float[,] heightMap, float[,] erosionMap, int width, int height)
    {
        while (erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] > 0)
        {
            List<Vector2> neighbours = GenerateNeighbours(dropletPosition, width, height);
            neighbours.Shuffle();
            bool foundLower = false;
            foreach (Vector2 n in neighbours)
            {
                if (heightMap[(int)n.x, (int)n.y] < heightMap[(int)dropletPosition.x, (int)dropletPosition.y])
                {
                    erosionMap[(int)n.x, (int)n.y] = erosionMap[(int)dropletPosition.x,
                                                                (int)dropletPosition.y] - currentErosionData.solubility;
                    dropletPosition = n;
                    foundLower = true;
                    break;
                }
            }
            if (!foundLower)
            {
                erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] -= currentErosionData.solubility;
            }
        }
        return erosionMap;
    }

    void Wind()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, mapRes, mapRes);

/*        float WindDir = currentErosionData.windStrength;
        float sinAngle = -Mathf.Sin(Mathf.Deg2Rad * WindDir);
        float cosAngle = Mathf.Cos(Mathf.Deg2Rad * WindDir);*/

        for (int y = -(mapRes) * 2; y <= mapRes * 2; y += 1)
        {
            for (int x = -(mapRes) * 2; x <= mapRes * 2; x += 1)
            {
                float noise = (float) Mathf.PerlinNoise(x* currentErosionData.windScale, 
                                                        y* currentErosionData.windScale) * 20;
                int pileX = x;
                int digY = y + (int)noise;
                int pileY = y + 5 + (int)noise;

                /* Vector2 digCoords = new Vector2(x * cosAngle - digy * sinAngle, digy * cosAngle + x * sinAngle);
                 Vector2 pileCoords = new Vector2(pileX * cosAngle - pileY * sinAngle, pileY * cosAngle + pileX * sinAngle);*/

                Vector2 digCoords = new Vector2(x, digY);
                Vector2 pileCoords = new Vector2(pileX, pileY);

                if (!(pileCoords.x < 0 || pileCoords.x > (mapRes) || pileCoords.y < 0 ||
                      pileCoords.y > (mapRes - 1) ||
                     (int)digCoords.x < 0 || (int)digCoords.x > (mapRes) ||
                     (int)digCoords.y < 0 || (int)digCoords.y > (mapRes - 1)))
                {
                    float erosionAmount = currentErosionData.windStrength;

                    heightMap[(int)digCoords.x, (int)digCoords.y] -= erosionAmount;
                    heightMap[(int)pileCoords.x, (int)pileCoords.y] += erosionAmount;
                }

            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
}

[System.Serializable]
public class ErosionData
{
    public enum ErosionType
    {
        Rain = 0, Thermal = 1, Tidal = 2,
        River = 3, Wind = 4, Canyon = 5
    }
    public ErosionType erosionType = ErosionType.Rain;

    public float thermalStrength = 0.99f;
    public float thermalThreshold = 0.005f;
    public int thermalIteration = 7;

    public float windStrength = 0.02f;
    public float windScale = 0.05f;
    public int windDirection = 30;

    // These canyon value is relative to the 0-1
    [SerializeField, Range(0,1)]
    public float canyonStrength = 0.8f, canyonBankSize = 0.1f, canyonBankJiggle = 0.5f, 
                canyonMinDistance = 0.5f, canyonStep = 0.05f;
    public float canyonDirectionJiggle = 0.2f;

    public int springsPerRiver = 5;
    public float solubility = 0.01f;
    public int droplets = 10;
    public int erosionSmoothAmount = 2;

}
