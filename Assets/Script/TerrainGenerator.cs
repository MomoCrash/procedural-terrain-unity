using UnityEngine;
using System.Threading;
using System;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{

    public enum SimulationMode { Endless, Simulation }
    public SimulationMode simulationMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public PropData propData;
    public Transform propContainer;

    public Material terrainMaterial;

    [Range (0, 6)]
    public int editorLOD;

    Queue<MapThreadInfo<MapData>> mapDataQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataQueue = new Queue<MapThreadInfo<MeshData>>();

    float[,] falloffMap;

    private void Awake()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);

        if (simulationMode == SimulationMode.Simulation) {
            FindObjectOfType<EndlessTerrain>().enabled = false;
            DrawSimulationMap();
        } else
        {
            FindObjectOfType<EndlessTerrain>().enabled = true;
        }
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
            DrawMapInEditor();
        }
    }

    void OnTextureValueUpdated()
    {
        textureData.ApplyToMaterial (terrainMaterial);
    }

    public int mapChunkSize
    {
        get
        {
            if (terrainData.useFlatShading)
            {
                return 95;
            } else
            {
                return 239;
            }
        }
    }

    private void OnValidate()
    {

        if (terrainData != null)
        {
            terrainData.OnValueUpdated -= OnValuesUpdated;
            terrainData.OnValueUpdated += OnValuesUpdated;
        }
        if (noiseData != null)
        {
            noiseData.OnValueUpdated -= OnValuesUpdated;
            noiseData.OnValueUpdated += OnValuesUpdated;
        }
        if (propData != null)
        {
            textureData.OnValueUpdated -= OnValuesUpdated;
            textureData.OnValueUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValueUpdated -= OnTextureValueUpdated;
            textureData.OnValueUpdated += OnTextureValueUpdated;
        }
    }

    // Use for the map
    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    public void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);

        lock (mapDataQueue)
        {
            mapDataQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    // Use for the mesh
    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    public void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.heightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);

        lock (meshDataQueue)
        {
            meshDataQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if (mapDataQueue.Count > 0)
        {
            for (int i = 0; i < mapDataQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataQueue.Count > 0)
        {
            for (int i = 0; i < meshDataQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public void DrawSimulationMap()
    {
        MapData mapData = GenerateMapData(Vector2Int.zero);
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.heightMultiplier, terrainData.meshHeightCurve, editorLOD, terrainData.useFalloff);
        TerrainDisplay display = FindObjectOfType<TerrainDisplay>();
        display.DrawMesh(meshData);

        PropsGenerator.GenerateProps(propData.props, propData, propContainer, display.meshTransform, display.previewMeshFilter.sharedMesh, mapData.heightMap.Length, propData.numberOfProps);

    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2Int.zero);
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.heightMultiplier, terrainData.meshHeightCurve, editorLOD, terrainData.useFalloff);
        TerrainDisplay display = FindObjectOfType<TerrainDisplay>();
        display.DrawMesh(meshData);

    }

    public MapData GenerateMapData(Vector2 center)
    {

        var noiseMap = NoiseMap.GetNoiseMap(mapChunkSize + 2, noiseData.seed, center + noiseData.offset, noiseData.scale, noiseData.octaves, noiseData.lacunarity, noiseData.persistance, noiseData.normalizedMode);

        if (terrainData.useFalloff)
        {

            if (falloffMap == null)
            {
                falloffMap = FalloffMap.GenerateFalloffMap(mapChunkSize + 2);
            }

            for (int y = 0; y < mapChunkSize+2; y++)
            {
                for (int x = 0; x < mapChunkSize+2; x++)
                {
                    if (terrainData.useFalloff)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - (falloffMap[x, y] * terrainData.fallOffForce));
                    }
                }
            }
        }

        return new MapData(noiseMap);

    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }


}

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}