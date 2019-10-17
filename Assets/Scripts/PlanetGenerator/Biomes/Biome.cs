using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BiomeType {
  DeepOcean,
  Ocean,
  Dessert,
  Grassland,
  TemperateForest,
  Jungle,
  Mountains,
  SnowyPeaks
}

public abstract class Biome {
  protected BiomeType _type;
  protected HeightMap _heightMap;
  protected HeightMap _processedHeightMap;
  protected TerrainLayer[] _layers;
  protected Vector2Int _position;

  public Biome(BiomeType type) {
    this._type = type;
  }

  public Biome generate(int size, Vector2 offset) {
    _heightMap = generateHeightmap(size, offset);
    _processedHeightMap = _heightMap.clone();
    // for (int i = 0; i < size; i++) {
    //   _processedHeightMap.values[0, i] = 0.0f;
    //   _processedHeightMap.values[i, 0] = 0.0f;
    //   _processedHeightMap.values[i, size - 1] = 0.0f;
    //   _processedHeightMap.values[size - 1, i] = 0.0f;
    // }

    return this;
  }

  public BiomeType type {
    get { return _type; }
  }

  public TerrainLayer[] layers {
    get { return _layers; }
  }

  public HeightMap heightmap {
    get { return _heightMap; }
  }

  public HeightMap processedHeightmap {
    get { return _processedHeightMap; }
  }

  public Vector2Int position {
    get { return _position; }
    set { _position = value; }
  }

  public abstract float[] blendLayer(int x, int y, TerrainData terrainData, float[,] heights);
  public abstract TerrainLayer[] generateLayers();
  public abstract void generateDetails(Terrain terrain, Dictionary<int[], Chunk> chunksDict);
  public abstract HeightMap generateHeightmap(int size, Vector2 offset);
}
