using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BiomeType {
  DeepOcean,
  Ocean,
  Grassland,
  Dessert,
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
  protected Vector2 _position;

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

  public TerrainLayer[] layers {
    get { return _layers; }
  }

  public HeightMap heightmap {
    get { return _heightMap; }
  }

  public HeightMap processedHeightmap {
    get { return _processedHeightMap; }
  }

  public Vector2 position {
    get { return _position; }
    set { _position = value; }
  }

  public abstract TerrainLayer[] generateLayers();
  public abstract HeightMap generateHeightmap(int size, Vector2 offset);
}
