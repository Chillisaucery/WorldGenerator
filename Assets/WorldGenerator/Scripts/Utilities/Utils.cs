using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    /// <summary>
    /// Fractal Brownian motion
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="oct"> Number of loop to perform </param>
    /// <param name="persistance"> The amplitude multiplication after each loop </param>
    /// <param name="lacunarity"> The prequency multiplication after each loop  </param>
    /// <returns></returns>
    public static float fBM(float x, float y, int oct, float persistance, float lacunarity)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;
        for (int i = 0; i < oct; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

}
