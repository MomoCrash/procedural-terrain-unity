using UnityEngine;

public class TerrainDisplay : MonoBehaviour
{

    public Renderer previewRenderer;
    public MeshFilter previewMeshFilter;
    public MeshRenderer previewMeshRenderer;
    public Transform meshTransform;

    public void RenderTerrainTexture(Color[] colour, int size)
    {
        Texture2D texture = new Texture2D(size, size);

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(colour);
        texture.Apply();

        previewRenderer.sharedMaterial.mainTexture = texture;
        previewRenderer.transform.localScale = new Vector3(size, 1, size);
    }

    public void DrawMesh(MeshData meshData)
    {
        previewMeshFilter.mesh = meshData.CreateMesh();
        previewMeshFilter.transform.localScale = Vector3.one * FindObjectOfType<TerrainGenerator>().terrainData.uniformScale;
    }

}