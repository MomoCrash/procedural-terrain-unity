using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{

    public NoiseMap.NormalizedMode normalizedMode;

    public int octaves;
    [Range(0f, 1f)]
    public float persistance;
    public float lacunarity;

    [Range(0f, 100f)]
    public float scale;

    public int seed;
    public Vector2 offset;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        if (octaves < 1)
        {
            octaves = 1;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        base.OnValidate();
    }
#endif
}
