using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{

    public bool useFlatShading;

    public bool useFalloff;
    [Range(0f, 1f)]
    public float fallOffForce;

    public float heightMultiplier;
    public AnimationCurve meshHeightCurve;

    [Range(0f, 10f)]
    public float uniformScale = 1f;

    public float minHeight
    {
        get
        {
            return uniformScale * heightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }

    public float maxHeight
    {
        get
        {
            return uniformScale * heightMultiplier * meshHeightCurve.Evaluate(1);
        }
    }

}
