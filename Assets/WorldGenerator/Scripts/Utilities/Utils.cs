using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;
using Random = System.Random;

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

    public static float fBMDefault1D(float x)
    {
        return fBM(x, 0, 2, 0.5f, 2);
    }

    public static float fBMDefault(float x, float y)
    {
        return fBM(x, 0, 2, 0.5f, 2);
    }

    public static List<Vector2> GenerateNeighbours(Vector2 pos, int radius, int resolution)
    {
        return GenerateNeighbours(pos, radius, 1, resolution);
    }

    public static List<Vector2> GenerateNeighbours(Vector2 pos, int radius, int step, int resolution)
    {
        List<Vector2> neighbours = new List<Vector2>();

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (!(x == 0 && y == 0))
                {
                    Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x*step, 0, resolution - 1),
                                                Mathf.Clamp(pos.y + y*step, 0, resolution - 1));
                    if (!neighbours.Contains(nPos))
                        neighbours.Add(nPos);
                }
            }
        }
        return neighbours;
    }


    public static float[,] Rotate2DMatrixClockwise(float[,] matrix)
    {
        //Exit if the 2D array is not square 
        if (matrix.GetLength(0) != matrix.GetLength(1)) 
            return matrix;

        int matrixSize = matrix.GetLength(0);

        for (int x = 0; x < matrixSize / 2+1; x++)
            for (int y = x; y < matrixSize - x - 1; y++)
            {
                // Swap elements of each cycle in clockwise direction
                float temp = matrix[x, y];
                matrix[x, y] = matrix[matrixSize - 1 - y, x];
                matrix[matrixSize - 1 - y, x] = matrix[matrixSize - 1 - x, matrixSize - 1 - y];
                matrix[matrixSize - 1 - x, matrixSize - 1 - y] = matrix[y, matrixSize - 1 - x];
                matrix[y, matrixSize - 1 - x] = temp;
            }

        return matrix;
    }

    public static float[,] Rotate2DMatrixAntiClockwise(float[,] matrix)
    {
        //Exit if the 2D array is not square 
        if (matrix.GetLength(0) != matrix.GetLength(1))
            return matrix;

        int matrixSize = matrix.GetLength(0);

        for (int x = 0; x < matrixSize / 2+1; x++)
            for (int y = x; y < matrixSize - x - 1; y++)
            {
                // Swap elements of each cycle in anti-clockwise direction
                float temp = matrix[x, y];
                matrix[x, y] = matrix[y, matrixSize - 1 - x];
                matrix[y, matrixSize - 1 - x] = matrix[matrixSize - 1 - x, matrixSize - 1 - y];
                matrix[matrixSize - 1 - x, matrixSize - 1 - y] = matrix[matrixSize - 1 - y, x];
                matrix[matrixSize - 1 - y, x] = temp;
            }

        return matrix;
    }

    public static float[,] Rotate2DMatrix180Degree(float[,] matrix)
    {
        //Exit if the 2D array is not square 
        if (matrix.GetLength(0) != matrix.GetLength(1))
            return matrix;

        int matrixSize = matrix.GetLength(0);

        float[,] resultMatrix = new float[matrixSize, matrixSize];
        int row = 0, col = 0;

        for (int i = matrixSize - 1; i >= 0; i--)
        {
            for (int j = matrixSize - 1; j >= 0; j--)
            {
                resultMatrix[row, col] = matrix[i, j];
                col++;
            }

            row++;
            col = 0;
        }

        return resultMatrix;   
    }

    public static float[,] FlipMatrix(float[,] matrix, int matrixResolution)
    {
        float[,] flippedMatrix = new float[matrixResolution, matrixResolution];

        for (int i = 0; i < matrixResolution; i++)
            for (int j = 0; j < matrixResolution; j++)
            {
                flippedMatrix[i, j] = matrix[matrixResolution - 1 - i, j];
            }

        return flippedMatrix;
    }

    public static void Shuffle<T>(this IList<T> values)
    {
        Random rand = new Random();

        for (int i = values.Count - 1; i > 0; i--)
        {
            int k = rand.Next(i + 1);
            T value = values[k];
            values[k] = values[i];
            values[i] = value;
        }
    }

    public static List<Vector2Int> ConvertToVector2Ints (List<Vector2> vector2List)
    {
        List<Vector2Int> result = new List<Vector2Int>(); 

        foreach (Vector2 vector2 in vector2List)
        {
            result.Add(Vector2Int.RoundToInt(vector2));
        }

        return result;
    }

    //A function for smoothing out the value, constructed by Long Luu Hien
    public static float VoronoiSmooth(float x, float steepness, float amplitude)
    {
        return Mathf.Cos(Mathf.Pow(Mathf.Clamp01(x), steepness) * Mathf.PI) * amplitude + (1 - amplitude);
    }

    public static float DefaultVoronoiSmooth(float x) => VoronoiSmooth(x, 1, 0.5f);
    public static float SteepVoronoiSmooth(float x) => VoronoiSmooth(x, 0.5f, 0.5f);



    public static IEnumerator AnimateFloatingObj(GameObject obj, int floatingRange)
    {
        bool isGoingUp = false;

        while (true)
        {
            if (isGoingUp)
                obj.transform.DOMoveY(obj.transform.position.y + floatingRange, TWEEN_DURATION).SetEase(TWEEN_EASE);
            else obj.transform.DOMoveY(obj.transform.position.y - floatingRange, TWEEN_DURATION).SetEase(TWEEN_EASE);

            yield return new WaitForSeconds(TWEEN_DURATION);

            isGoingUp = !isGoingUp;
        }
    }

    public static IEnumerator DelayedInvoke(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);

        action.Invoke();
    }
}
