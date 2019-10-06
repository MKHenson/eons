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

  public Biome(BiomeType type) {
    this._type = type;
  }

  public void generate(int size, Vector2 offset) {
    _layers = generateLayers();
    _heightMap = generateHeightmap(size, offset);
    _processedHeightMap = _heightMap.clone();
    for (int i = 0; i < size; i++) {
      _processedHeightMap.values[0, i] = 0.0f;
      _processedHeightMap.values[i, 0] = 0.0f;
      _processedHeightMap.values[i, size - 1] = 0.0f;
      _processedHeightMap.values[size - 1, i] = 0.0f;
    }
  }

  public float sample(Chunk[,] chunks, int x, int y, int heightmapSize) {
    int whichSampleX = 1;
    int whichSampleY = 1;

    if (x < 0)
      whichSampleX = 0;
    else if (x >= heightmapSize)
      whichSampleX = 2;

    if (y < 0)
      whichSampleY = 0;
    else if (y >= heightmapSize)
      whichSampleY = 2;

    // int targetChunk = whichSampleY * 3 + whichSampleX;
    // if (targetChunk < 0 || targetChunk >= chunks.Count)
    //   return -1;

    Chunk chunk = chunks[whichSampleX, whichSampleY];

    if (chunk == null)
      return -1;

    float[,] values = chunk.biome._heightMap.values;
    int normalizedX = x < 0 ? heightmapSize + x : x >= heightmapSize ? x - heightmapSize : x;
    int normalizedY = y < 0 ? heightmapSize + y : y >= heightmapSize ? y - heightmapSize : y;
    return values[normalizedX, normalizedY];
  }

  public void blendEdges(Chunk[,] chunks) {
    int heightmapSize = _heightMap.values.GetLength(0);
    float[,] heightmap = _heightMap.values;
    float[,] processedHeightmap = _processedHeightMap.values;
    int radius = 2;
    int halfRadius = radius / 2;
    float average = 0;
    int divCount = 0;

    for (int i = 0; i < heightmapSize; i++)
      for (int j = 0; j < heightmapSize; j++) {
        if (!((i > halfRadius && i < heightmapSize - halfRadius) && (j > halfRadius && j < heightmapSize - halfRadius))) {

          average = -1;
          divCount = 0;

          for (int rx = -halfRadius; rx < halfRadius; rx++)
            for (int ry = -halfRadius; ry < halfRadius; ry++) {
              float sampleValue = sample(chunks, (rx) + i, (ry) + j, heightmapSize);
              if (sampleValue != -1) {
                average += sampleValue;
                divCount++;
              }
            }

          if (average != -1)
            processedHeightmap[i, j] = average / divCount;
        }
      }
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

  public abstract TerrainLayer[] generateLayers();
  public abstract HeightMap generateHeightmap(int size, Vector2 offset);
}
