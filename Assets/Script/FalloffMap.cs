using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffMap
{

    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(0); j++)
            {
                float x = i / (float)size * 2 -1;
                float y = j / (float)size * 2 - 1;
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = value;
            }
        }
        return map;
    }
    
}
