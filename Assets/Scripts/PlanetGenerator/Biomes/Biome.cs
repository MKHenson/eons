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

  public void generate(int size, Vector2 offset) {
    _layers = generateLayers();
    _heightMap = generateHeightmap(size, offset);
    _processedHeightMap = _heightMap.clone();
    // for (int i = 0; i < size; i++) {
    //   _processedHeightMap.values[0, i] = 0.0f;
    //   _processedHeightMap.values[i, 0] = 0.0f;
    //   _processedHeightMap.values[i, size - 1] = 0.0f;
    //   _processedHeightMap.values[size - 1, i] = 0.0f;
    // }
  }

  public float sample(Chunk[,] chunks, int x, int y, int heightmapSize, Vector2Int chunkIndex) {
    int whichSampleX = 0;
    int whichSampleY = 0;

    if (x < 0)
      whichSampleX = -1;
    else if (x >= heightmapSize)
      whichSampleX = 1;

    if (y < 0)
      whichSampleY = -1;
    else if (y >= heightmapSize)
      whichSampleY = 1;

    whichSampleX = chunkIndex.x + whichSampleX;
    whichSampleY = chunkIndex.y + whichSampleY;

    if (whichSampleX < 0 || whichSampleX > 2)
      return -1;
    if (whichSampleY < 0 || whichSampleY > 2)
      return -1;

    Chunk chunk = chunks[whichSampleX, whichSampleY];

    if (chunk == null)
      return -1;

    float[,] values = chunk.biome._heightMap.values;
    int normalizedX = x < 0 ? heightmapSize + x : x >= heightmapSize ? x - heightmapSize : x;
    int normalizedY = y < 0 ? heightmapSize + y : y >= heightmapSize ? y - heightmapSize : y;
    float toReturn = values[normalizedY, normalizedX];
    return toReturn;
  }

  public void blendEdges(Chunk[,] chunks) {
    int heightmapSize = _heightMap.values.GetLength(0);
    float[,] heightmap = _heightMap.values;
    float[,] processedHeightmap = _processedHeightMap.values;
    int radius = 2;
    int halfRadius = radius / 2;
    Vector2Int curChunkIndex = new Vector2Int();

    for (int i = 0; i < 3; i++)
      for (int j = 0; j < 3; j++)
        if (chunks[i, j] != null && chunks[i, j].biome == this) {
          curChunkIndex.x = i;
          curChunkIndex.y = j;
          break;
        }


    // float average = 0;
    // int divCount = 0;

    for (int i = 0; i < heightmapSize; i++)
      for (int j = 0; j < heightmapSize; j++) {
        if (!((i > halfRadius && i < heightmapSize - halfRadius) && (j > halfRadius && j < heightmapSize - halfRadius))) {

          // average = -1;
          // divCount = 0;

          float h = sample(chunks, i, j, heightmapSize, curChunkIndex);
          processedHeightmap[i, j] = 0;

          // for (int rx = -halfRadius; rx < halfRadius; rx++)
          //   for (int ry = -halfRadius; ry < halfRadius; ry++) {
          //     float sampleValue = sample(chunks, (rx) + i, (ry) + j, heightmapSize);
          //     if (sampleValue != -1) {
          //       average += sampleValue;
          //       divCount++;
          //     }
          //   }

          // if (average != -1)
          //   processedHeightmap[i, j] = average / divCount;
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

  public Vector2 position {
    get { return _position; }
    set { _position = value; }
  }

  public abstract TerrainLayer[] generateLayers();
  public abstract HeightMap generateHeightmap(int size, Vector2 offset);
}
