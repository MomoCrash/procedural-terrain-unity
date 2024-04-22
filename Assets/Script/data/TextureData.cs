using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public Layer[] layers; 

    float savedMinHeight;
    float savedMaxheight;

    public void ApplyToMaterial(Material material)
    {
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColours", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseColorStrenght", layers.Select(x => x.tintStrenght).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrenght).ToArray());
        material.SetFloatArray("baseTextureScale", layers.Select(x => x.textureScale).ToArray());
        Texture2DArray texture2DArray = GenerateTexture2DArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texture2DArray);

        UpdateMeshHeights(material, savedMinHeight, savedMaxheight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxheight = maxHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTexture2DArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range (0,1)]
        public float tintStrenght;
        [Range (0,1)]
        public float startHeight;
        [Range (0,1)]
        public float blendStrenght;
        public float textureScale;
    }

}
