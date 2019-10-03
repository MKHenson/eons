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
  protected TerrainLayer[] _layers;
  protected HeightMap _heightmap;

  public Biome(BiomeType type) {
    this._type = type;
  }

  public virtual void generate(int size, Vector2 offset) {
    _layers = generateLayers();
    _heightmap = generateHeightmap(size, offset);
  }

  public TerrainLayer[] layers {
    get { return _layers; }
  }

  public HeightMap heightmap {
    get { return _heightmap; }
  }

  public abstract TerrainLayer[] generateLayers();
  public abstract HeightMap generateHeightmap(int size, Vector2 offset);
}
