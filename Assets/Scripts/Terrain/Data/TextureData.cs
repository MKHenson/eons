using System;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData {

  const int textureSize = 512;
  const TextureFormat textureFormat = TextureFormat.RGB565;

  public Layer[] layers;
  float savedMinHeight;
  float savedMaxHeight;

  public void applyToMaterial(Material material) {
    material.SetInt("layerCount", layers.Length);
    material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
    material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
    material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
    material.SetFloatArray("baseColorStrength", layers.Select(x => x.tintStrength).ToArray());
    material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
    Texture2D[] diffuse = layers.Select(x => x.texture).ToArray();
    Texture2D[] normals = layers.Select(x => x.normal).ToArray();
    Texture2D[] diffAndNormals = new Texture2D[diffuse.Length + normals.Length];
    Array.Copy(diffuse, diffAndNormals, diffuse.Length);
    Array.Copy(normals, 0, diffAndNormals, normals.Length, normals.Length);


    material.SetTexture("baseTextures", generateTextureArray(diffAndNormals));
    updateMeshHeights(material, savedMinHeight, savedMaxHeight);
  }

  public void updateMeshHeights(Material material, float minHeight, float maxHeight) {
    savedMinHeight = minHeight;
    savedMaxHeight = maxHeight;

    material.SetFloat("minHeight", minHeight);
    material.SetFloat("maxHeight", maxHeight);
  }

  Texture2DArray generateTextureArray(Texture2D[] textures) {
    Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);

    for (int i = 0; i < textures.Length; i++) {
      textureArray.SetPixels(textures[i].GetPixels(), i);
    }

    textureArray.Apply();
    return textureArray;
  }

  [System.Serializable]
  public class Layer {
    public Texture2D texture;
    public Texture2D normal;
    public Color tint;
    [Range(0, 1)]
    public float tintStrength;
    [Range(0, 1)]
    public float startHeight;
    [Range(0, 1)]
    public float blendStrength;
    public float textureScale;
  }
}
