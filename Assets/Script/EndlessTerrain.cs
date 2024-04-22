using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{

    const float vwrMoveThresholdForUpdt = 25f;
    const float sqrvwrMoveThresholdForUpdt = vwrMoveThresholdForUpdt * vwrMoveThresholdForUpdt;

    static TerrainGenerator terrainGenerator;

    public LODInfo[] lodDetailsInfo;
    [Range(400, 2000)]
    public static float FieldOfView;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    int chunkSize;
    int chunkVisibleInView;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionnary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisible = new List<TerrainChunk>();

    private void Start()
    {
        terrainGenerator = FindObjectOfType<TerrainGenerator>();

        FieldOfView = lodDetailsInfo[lodDetailsInfo.Length-1].dstThreshold;
        chunkSize = terrainGenerator.mapChunkSize-1;
        chunkVisibleInView = Mathf.RoundToInt(FieldOfView / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / terrainGenerator.terrainData.uniformScale;
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrvwrMoveThresholdForUpdt)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {

        terrainChunksVisible.ForEach(t =>
        {
            t.SetVisible(false);
        });
        terrainChunksVisible.Clear();
        
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunkVisibleInView; yOffset <= chunkVisibleInView; yOffset++)
        {
            for (int xOffset = -chunkVisibleInView; xOffset <= chunkVisibleInView; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionnary.ContainsKey(viewedChunkCoord))
                {
                    var terrainChunk = terrainChunkDictionnary[viewedChunkCoord];
                    terrainChunk.UpdateChunk();
                } else
                {
                    terrainChunkDictionnary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, lodDetailsInfo, transform, mapMaterial));
                }
            }
        }

    }

    public class TerrainChunk
    {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer renderer;
        MeshFilter filter;
        MeshCollider collider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        LODMesh collisionLODMesh;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");

            renderer = meshObject.AddComponent<MeshRenderer>();
            filter = meshObject.AddComponent<MeshFilter>();
            collider = meshObject.AddComponent<MeshCollider>();
            renderer.material = material;

            meshObject.transform.position = position3 * terrainGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * terrainGenerator.terrainData.uniformScale;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateChunk);
                if (detailLevels[i].useForColider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            terrainGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData data)
        {
            this.mapData = data;
            mapDataReceived = true;



            UpdateChunk();
        }

        public void UpdateChunk()
        {
            if (mapDataReceived) {

                float viewerDistanceToEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistanceToEdge <= FieldOfView;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceToEdge > detailLevels[i].dstThreshold)
                        {
                            lodIndex = i + 1;
                        } else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasResponseMesh)
                        {
                            previousLODIndex = lodIndex;
                            filter.mesh = lodMesh.mesh;
                        } else if (!lodMesh.hasRequestMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                    if (lodIndex == 0)
                    {
                        if (collisionLODMesh.hasResponseMesh)
                        {
                            collider.sharedMesh = collisionLODMesh.mesh;
                        } else if (!collisionLODMesh.hasRequestMesh)
                        {
                            collisionLODMesh.RequestMesh(mapData);
                        }
                    }
                    terrainChunksVisible.Add(this);
                }
                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }


        public bool IsVisible()
        {
            return meshObject.activeSelf;
        } 
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestMesh;
        public bool hasResponseMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        public void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasResponseMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestMesh = true;
            terrainGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float dstThreshold;
        public bool useForColider;
    }
}
