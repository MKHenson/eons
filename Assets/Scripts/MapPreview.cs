using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour {
  public Renderer textureRenderer;
  public MeshFilter meshFilter;
  public MeshRenderer meshRenderer;

  public enum DrawMode { NoiseMap, Mesh, Falloff };
  public DrawMode drawMode;

  public MeshSettings meshSettings;
  public HeightMapSettings heightMapSettings;
  public TextureData textureData;

  public Material terrainMaterial;

  [Range(0, MeshSettings.numSupportedLODs - 1)]
  public int editorPreviewLOD;
  public bool autoUpdate;
  
  public void drawMapInEditor() {
    textureData.applyToMaterial(terrainMaterial);
    textureData.updateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
    HeightMap heightMap = HeightMapGenerator.generateHeightmap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightMapSettings, Vector2.zero);

    if (drawMode == DrawMode.NoiseMap)
      drawTexture(TextureGenerator.textureFromHeightmap(heightMap));
    else if (drawMode == DrawMode.Mesh) {
      if (heightMapSettings.useFalloff) {
        float [,] falloff = FalloffGenerator.generateFalloffMap(meshSettings.numVerticesPerLine);
        for (int i = 0; i < heightMap.values.GetLength(0); i++)
          for (int j = 0; j < heightMap.values.GetLength(1); j++)
            heightMap.values[i, j] = heightMap.values[i, j] * (1 - falloff[i, j]);
      }
      drawMesh(MeshGenerator.generateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
    }
    else if (drawMode == DrawMode.Falloff)
      drawTexture(TextureGenerator.textureFromHeightmap(new HeightMap(FalloffGenerator.generateFalloffMap(meshSettings.numVerticesPerLine), 0, 1)));
  }
  
  public void drawTexture(Texture2D texture) {
    textureRenderer.sharedMaterial.mainTexture = texture;
    textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

    textureRenderer.gameObject.SetActive(true);
    meshFilter.gameObject.SetActive(false);
  }

  public void drawMesh(MeshData mesh) {
    meshFilter.sharedMesh = mesh.createMesh();

    textureRenderer.gameObject.SetActive(false);
    meshFilter.gameObject.SetActive(true);
  }

  void onValuesUpdated() {
    if (!Application.isPlaying)
      drawMapInEditor();
  }

  void onTextureValuesUpdate() {
    textureData.applyToMaterial(terrainMaterial);
  }

  void OnValidate() {

    if (meshSettings != null) {
      meshSettings.onValuesUpdated -= onValuesUpdated;
      meshSettings.onValuesUpdated += onValuesUpdated;
    }
    if (heightMapSettings != null) {
      heightMapSettings.onValuesUpdated -= onValuesUpdated;
      heightMapSettings.onValuesUpdated += onValuesUpdated;
    }
    if (textureData != null) {
      textureData.onValuesUpdated -= onTextureValuesUpdate;
      textureData.onValuesUpdated += onTextureValuesUpdate;
    }
  }
}
