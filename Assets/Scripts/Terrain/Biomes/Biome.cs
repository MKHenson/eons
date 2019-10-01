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
  BiomeType type;
  HeightMap heightMap;
  TerrainLayer[] layers;

  public Biome(BiomeType type) {
    this.type = type;
  }

  public abstract TerrainLayer[] generateLayers();
}
