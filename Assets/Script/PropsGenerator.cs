using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PropsGenerator
{

    public static void GenerateProps(Prop[] props, PropData data, Transform parent, Transform meshTransform, Mesh mesh, int mapSize, int propAmount)
    {

        float[] chances = props.Select(x => x.SpawnChance).ToArray();
        int generatedAmount = 0;

        Vector3[] meshVertices = mesh.vertices;
        int[,] spawnedPosition = new int[mapSize, mapSize];

        for (int z = 0, i = 0;  z <= mapSize && generatedAmount < propAmount; z++)
        {
            for (int x = 0; x <= mapSize && generatedAmount < propAmount; x++)
            {
                var objectMeshPosition = meshTransform.TransformPoint(meshVertices[i].x, meshVertices[i].y, meshVertices[i].z);

                int propCountAround = IsProAround(spawnedPosition, mapSize, x, i);
                float nextSpawnChance = 1 - (data.PopulationDensitiy * (propCountAround/18));

                Debug.Log(propCountAround);

                var densityRandomSpawn = Random.value;
                if (densityRandomSpawn <= nextSpawnChance)
                {
                    for (var propIndex = 0; propIndex < props.Length; propIndex++)
                    {
                        var prop = props[propIndex];
                        var chance = chances[propIndex];

                        var randomSpawnChance = Random.value;
                        if (randomSpawnChance <= chance)
                        {
                            GameObject go = GameObject.Instantiate(prop.PropPrefab, parent.position, Quaternion.identity);
                            go.transform.localPosition = meshTransform.TransformPoint(meshVertices[i].x, meshVertices[i].y, meshVertices[i].z);

                            float randomScale = Random.Range(prop.MinScale, prop.MaxScale);
                            go.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                            go.transform.parent = parent;

                            spawnedPosition[x, i] = 1;
                            generatedAmount++;
                            i+=3;
                            break;
                        }
                    }
                }
                i +=3;
            }
        }

    }

    private static int IsProAround(int[,] spawnedPosition, int size, int x, int y)
    {

        int treeAround = 0;
        for (int i = x-2; i < x+2; i++)
        {
            for (int j = y-2; j < y+2; j++)
            {
                Debug.Log(x + " " + y);
                bool isOutOfBound = (x+i < 0 || x+i > size) || (y+j < 0 || y+j > size) ;
                if (isOutOfBound) continue;
                if (spawnedPosition[x+i, y+j] != 0)
                {
                    treeAround += 1;
                }
            }
        }

        Debug.Log(treeAround);

        return treeAround;

    }

}