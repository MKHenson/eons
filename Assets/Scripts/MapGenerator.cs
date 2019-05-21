using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {
  public enum DrawMode { NoiseMap, Mesh, Falloff };

  public DrawMode drawMode;

  public const int mapChunkSize = 239; 
  [Range(0, 6)]
  public int editorPreviewLOD;

  public Material terrainMaterial;

  public TerrainData terrainData;
  public NoiseData noiseData;
  public TextureData textureData;

  float[,] falloffMap;

  public bool autoUpdate;

  Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
  Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

  private void Awake() {
    textureData.updateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
  }

  void onValuesUpdated() {
    if (!Application.isPlaying)
      drawMapInEditor();
  }

  void onTextureValuesUpdate() {
    textureData.applyToMaterial(terrainMaterial);
  }

  public void drawMapInEditor() {

    textureData.updateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
    MapData mapdata = generateMapData(Vector2.zero);

    MapDisplay display = FindObjectOfType<MapDisplay>();
    if (drawMode == DrawMode.NoiseMap)
      display.drawTexture(TextureGenerator.textureFromHeightmap(mapdata.heightMap));
    else if (drawMode == DrawMode.Mesh)
      display.drawMesh(MeshGenerator.generateTerrainMesh(mapdata.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD) );
    else if (drawMode == DrawMode.Falloff)
      display.drawTexture(TextureGenerator.textureFromHeightmap(falloffMap));
  }

  public void requestMapData(Vector2 center, Action<MapData> callback) {
    ThreadStart threadStart = delegate {
      MapDataThread(center, callback);
    };

    new Thread(threadStart).Start();
  }

  void MapDataThread(Vector2 center, Action<MapData> callback) {
    MapData mapData = generateMapData(center);

    lock (mapDataThreadInfoQueue) {
      mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }
  }

  public void requestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
    ThreadStart threadStart = delegate {
      MeshDataThread(mapData, lod, callback);
    };

    new Thread(threadStart).Start();
  }

  void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
    MeshData meshData = MeshGenerator.generateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod);
    lock (meshDataThreadInfoQueue) {
      meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }
  }

  void Update() {
    if (mapDataThreadInfoQueue.Count > 0) {
      for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
        MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
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

  MapData generateMapData(Vector2 center) {
    float[,] noiseMap = Noise.generateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode);

    if (terrainData.useFalloff) {

      if (falloffMap == null)
        falloffMap = FalloffGenerator.generateFalloffMap(mapChunkSize + 2);

      for (int y = 0; y < mapChunkSize + 2; y++)
        for (int x = 0; x < mapChunkSize + 2; x++) {
          if (terrainData.useFalloff)
            noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
        }
    }
    
    return new MapData(noiseMap);
  }

  void OnValidate() {

    if (terrainData != null) {
      terrainData.onValuesUpdated -= onValuesUpdated;
      terrainData.onValuesUpdated += onValuesUpdated;
    }
    if (noiseData != null) {
      noiseData.onValuesUpdated -= onValuesUpdated;
      noiseData.onValuesUpdated += onValuesUpdated;
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

public struct MapData {
  public readonly float[,] heightMap;

  public MapData(float[,] heightMap) {
    this.heightMap = heightMap;
  }
}