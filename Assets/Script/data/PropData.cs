using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class PropData : UpdatableData
{

    public Prop[] props;

    [Range(0, 1000)]
    public int numberOfProps;

    [Range(1, 10)]
    public int PopulationSpreading;
    [Range(0f, 1f)]
    public float PopulationDensitiy;

}

[System.Serializable]
public struct Prop
{
    [Range(1f, 10f)]
    public float MaxScale;
    [Range(0f, 10f)]
    public float MinScale;
    public GameObject PropPrefab;

    [Range(0f, 100f)]
    public float SpawnChance;
}
