using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {
  public enum DrawMode { NoiseMap, Mesh, Falloff };

  public DrawMode drawMode;



  [Range(0, MeshSettings.numSupportedLODs - 1)]
  public int editorPreviewLOD;

  public Material terrainMaterial;

  public MeshSettings meshSettings;
  public HeightMapSettings heightMapSettings;
  public TextureData textureData;

  float[,] falloffMap;

  public bool autoUpdate;

  Queue<MapThreadInfo<HeightMap>> heightMapThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
  Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

  void Start() {
    textureData.applyToMaterial(terrainMaterial);
    textureData.updateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
  }

  void onValuesUpdated() {
    if (!Application.isPlaying)
      drawMapInEditor();
  }

  void onTextureValuesUpdate() {
    textureData.applyToMaterial(terrainMaterial);
  }


  public void drawMapInEditor() {

    textureData.updateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
    HeightMap mapdata = HeightMapGenerator.generateHeightmap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightMapSettings, Vector2.zero);

    MapDisplay display = FindObjectOfType<MapDisplay>();
    if (drawMode == DrawMode.NoiseMap)
      display.drawTexture(TextureGenerator.textureFromHeightmap(mapdata.values));
    else if (drawMode == DrawMode.Mesh)
      display.drawMesh(MeshGenerator.generateTerrainMesh(mapdata.values, meshSettings, editorPreviewLOD));
    else if (drawMode == DrawMode.Falloff)
      display.drawTexture(TextureGenerator.textureFromHeightmap(FalloffGenerator.generateFalloffMap(meshSettings.numVerticesPerLine)));
  }

  public void requestHeightMap(Vector2 center, Action<HeightMap> callback) {
    ThreadStart threadStart = delegate {
      HeightMapThread(center, callback);
    };

    new Thread(threadStart).Start();
  }

  void HeightMapThread(Vector2 center, Action<HeightMap> callback) {
    HeightMap heightMap = HeightMapGenerator.generateHeightmap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightMapSettings, center);

    lock (heightMapThreadInfoQueue) {
      heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
    }
  }

  public void requestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback) {
    ThreadStart threadStart = delegate {
      MeshDataThread(heightMap, lod, callback);
    };

    new Thread(threadStart).Start();
  }

  void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback) {
    MeshData meshData = MeshGenerator.generateTerrainMesh(heightMap.values, meshSettings, lod);
    lock (meshDataThreadInfoQueue) {
      meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }
  }

  void Update() {
    if (heightMapThreadInfoQueue.Count > 0) {
      for (int i = 0; i < heightMapThreadInfoQueue.Count; i++) {
        MapThreadInfo<HeightMap> threadInfo = heightMapThreadInfoQueue.Dequeue();
        threadInfo.callback(threadInfo.parameter);
      }
    }

    if (meshDataThreadInfoQueue.Count > 0) {
      for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
        MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
        threadInfo.callback(threadInfo.parameter);
      }
    }
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
    if (textureData!= null) {
      textureData.onValuesUpdated -= onTextureValuesUpdate;
      textureData.onValuesUpdated += onTextureValuesUpdate;
    }
  }

  struct MapThreadInfo<T> {
    public readonly Action<T> callback;
    public readonly T parameter;

    public MapThreadInfo(Action<T> callback, T parameter) {
      this.callback = callback;
      this.parameter = parameter;
    }
  }
}