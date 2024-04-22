using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class NoiseMap
{

    public enum NormalizedMode { Local, Global };

    public static float[,] GetNoiseMap(int size, int seed, Vector2 offset, float scale, int octaves, float lacunarity, float persistance, NormalizedMode normalizedMode)
    {

        System.Random prng = new System.Random(seed);

        float minLocalNoiseHeight = float.MaxValue;
        float maxLocalNoiseHeight = float.MinValue;

        float maxPossibleHeight = 0;
        float frequence = 1;
        float amplitude = 1;

        float halfSize = size / 2;

        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        float[,] noiseMap = new float[size, size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {

                float noiseHeight = 0;
                frequence = 1;
                amplitude = 1;

                for (int i = 0; i < octaves; i++)
                {
                    noiseHeight = GetNoiseXY(x-halfSize+octaveOffsets[i].x, y-halfSize+octaveOffsets[i].y, scale, noiseHeight, amplitude, frequence);

                    frequence *= lacunarity;
                    amplitude *= persistance;
                }

                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                } else if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
                    
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (normalizedMode == NormalizedMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                } else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 1.65f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight,0, int.MaxValue);
                }
            }
        }

        return noiseMap;
    }

    private static float GetNoiseXY(float x, float y, float scale, float noise, float amplitude, float frequence)
    {

        float xSample = x / scale * frequence;
        float ySample = y / scale * frequence;

        float perlinvalue = Mathf.PerlinNoise(xSample, ySample) * 2 - 1;
        noise += perlinvalue * amplitude;

        return noise;
    }

}
