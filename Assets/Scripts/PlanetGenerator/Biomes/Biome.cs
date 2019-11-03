using System.Linq;
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
  private float[,] blendMask;

  public Biome(BiomeType type) {
    this._type = type;
  }

  public Biome generate(int size, Vector2 offset) {

    HeightmapSettings maskSettings = new HeightmapSettings();
    maskSettings.heightMultiplier = 1f;
    maskSettings.useFalloff = true;
    maskSettings.noiseSettings = new NoiseSettings();
    maskSettings.noiseSettings.scale = 35;
    maskSettings.noiseSettings.octaves = 6;
    maskSettings.noiseSettings.lacunarity = 1.8f;
    maskSettings.noiseSettings.persistance = 0.51f;

    maskSettings.heightCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 1), new Keyframe(1, 1) });
    HeightMap maskHeightmap = HeightMapGenerator.generateHeightmap(512, 512, maskSettings, offset);
    blendMask = maskHeightmap.values;
    for (int x = 0; x < 512; x++)
      for (int y = 0; y < 512; y++)
        blendMask[x, y] = blendMask[x, y] / maskHeightmap.maxValue;


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

  protected float sampleBlendMask(float x, float y) {
    return blendMask[(int)(x * 511), (int)(y * 511)];
  }

  protected void blendTextures(Terrain terrain, List<Biome> otherBiomes, int[] layerIndices, Dictionary<int[], Chunk> chunksDict) {
    int w = terrain.terrainData.alphamapWidth;
    int h = terrain.terrainData.alphamapHeight;
    int halfWidth = w / 2;
    int halfHeight = h / 2;
    int[] positinInDictionary = chunksDict.FirstOrDefault(x => x.Value.biome == this).Key;

    float[,,] map = new float[w, h, terrain.terrainData.terrainLayers.Count()];
    Vector2 center = new Vector2(halfWidth, halfHeight);

    Chunk top = null;
    Chunk right = null;
    Chunk bottom = null;
    Chunk left = null;
    Chunk topLeft = null;
    Chunk topRight = null;
    Chunk btmLeft = null;
    Chunk btmRight = null;
    chunksDict.TryGetValue(new int[] { positinInDictionary[0], positinInDictionary[1] - 1 }, out top);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] + 1, positinInDictionary[1] }, out right);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0], positinInDictionary[1] + 1 }, out bottom);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] - 1, positinInDictionary[1] }, out left);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] - 1, positinInDictionary[1] - 1 }, out topLeft);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] + 1, positinInDictionary[1] - 1 }, out topRight);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] - 1, positinInDictionary[1] + 1 }, out btmLeft);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] + 1, positinInDictionary[1] + 1 }, out btmRight);

    int numLayers = getNumLayers();
    int blendDistance = 400;
    int topLayerIndex = top != null ? otherBiomes.FindIndex(biome => biome.type == top.biome.type) : -1;
    int topLayerTextureIndex = top != null ? numLayers + otherBiomes.Sum(biome => biome.type < top.biome.type ? biome.getNumLayers() : 0) : -1;
    int rightLayerIndex = right != null ? otherBiomes.FindIndex(biome => biome.type == right.biome.type) : -1;
    int rightLayerTextureIndex = right != null ? numLayers + otherBiomes.Sum(biome => biome.type < right.biome.type ? biome.getNumLayers() : 0) : -1;
    int btmLayerIndex = bottom != null ? otherBiomes.FindIndex(biome => biome.type == bottom.biome.type) : -1;
    int btmLayerTextureIndex = bottom != null ? numLayers + otherBiomes.Sum(biome => biome.type < bottom.biome.type ? biome.getNumLayers() : 0) : -1;
    int leftLayerIndex = left != null ? otherBiomes.FindIndex(biome => biome.type == left.biome.type) : -1;
    int leftLayerTextureIndex = left != null ? numLayers + otherBiomes.Sum(biome => biome.type < left.biome.type ? biome.getNumLayers() : 0) : -1;
    int topleftLayerIndex = topLeft != null ? otherBiomes.FindIndex(biome => biome.type == topLeft.biome.type) : -1;
    int topleftLayerTextureIndex = topLeft != null ? numLayers + otherBiomes.Sum(biome => biome.type < topLeft.biome.type ? biome.getNumLayers() : 0) : -1;
    int toprightLayerIndex = topRight != null ? otherBiomes.FindIndex(biome => biome.type == topRight.biome.type) : -1;
    int toprightLayerTextureIndex = topRight != null ? numLayers + otherBiomes.Sum(biome => biome.type < topRight.biome.type ? biome.getNumLayers() : 0) : -1;
    int btmleftLayerIndex = btmLeft != null ? otherBiomes.FindIndex(biome => biome.type == btmLeft.biome.type) : -1;
    int btmleftLayerTextureIndex = btmLeft != null ? numLayers + otherBiomes.Sum(biome => biome.type < btmLeft.biome.type ? biome.getNumLayers() : 0) : -1;
    int btmrightLayerIndex = btmRight != null ? otherBiomes.FindIndex(biome => biome.type == btmRight.biome.type) : -1;
    int btmrightLayerTextureIndex = btmRight != null ? numLayers + otherBiomes.Sum(biome => biome.type < btmRight.biome.type ? biome.getNumLayers() : 0) : -1;

    // For each point on the alphamap...
    for (int y = 0; y < h; y++) {
      for (int x = 0; x < w; x++) {
        Vector2 xyPos = new Vector2(x, y);

        float[] curBiomeLayers = blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

        for (var i = 0; i < numLayers; i++)
          map[x, y, i] = curBiomeLayers[i];

        // left
        if (leftLayerIndex != -1 && y < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(0, blendDistance, y);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = map[x, y, i] * t;

          float[] blends = left.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[leftLayerIndex]; i++)
            map[x, y, leftLayerTextureIndex + i] = blends[i] * inverseT;


        }
        // right
        else if (rightLayerIndex != -1 && y > h - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(h - blendDistance, h, y);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = map[x, y, i] * inverseT;

          float[] blends = right.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[rightLayerIndex]; i++)
            map[x, y, rightLayerTextureIndex + i] = blends[i] * t;
        }

        // top
        if (topLayerIndex != -1 && x < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(0, blendDistance, x);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = Mathf.Min(map[x, y, i], curBiomeLayers[i] * t);

          float[] blends = top.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[topLayerIndex]; i++)
            map[x, y, topLayerTextureIndex + i] = Mathf.Max(map[x, y, topLayerTextureIndex + i], blends[i] * inverseT);

        }
        // btm
        else if (btmLayerIndex != -1 && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(w - blendDistance, w, x);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = Mathf.Min(map[x, y, i], curBiomeLayers[i] * inverseT);

          float[] blends = bottom.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmLayerIndex]; i++)
            map[x, y, btmLayerTextureIndex + i] = Mathf.Max(map[x, y, btmLayerTextureIndex + i], blends[i] * t);


        }

        if (topleftLayerIndex != -1 && y < blendDistance && x < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Max(Mathf.InverseLerp(blendDistance / 2, blendDistance, x), Mathf.InverseLerp(blendDistance / 2, blendDistance, y));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = Mathf.Min(map[x, y, i], curBiomeLayers[i] * t);

          float[] blends = topLeft.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[topleftLayerIndex]; i++)
            map[x, y, topleftLayerTextureIndex + i] = Mathf.Max(map[x, y, topleftLayerTextureIndex + i], blends[i] * inverseT);


        } else if (toprightLayerIndex != -1 && y > h - blendDistance && x < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Max(Mathf.InverseLerp(h - (blendDistance / 2), h - blendDistance, y), Mathf.InverseLerp(blendDistance / 2, blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = Mathf.Min(map[x, y, i], curBiomeLayers[i] * t);

          float[] blends = topRight.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[toprightLayerIndex]; i++)
            map[x, y, toprightLayerTextureIndex + i] = Mathf.Max(map[x, y, toprightLayerTextureIndex + i], blends[i] * inverseT);


        } else if (btmleftLayerIndex != -1 && y < blendDistance && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Max(Mathf.InverseLerp(blendDistance / 2, blendDistance, y), Mathf.InverseLerp(w - (blendDistance / 2), w - blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = Mathf.Min(map[x, y, i], curBiomeLayers[i] * t);

          float[] blends = btmLeft.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmleftLayerIndex]; i++)
            map[x, y, btmleftLayerTextureIndex + i] = Mathf.Max(map[x, y, btmleftLayerTextureIndex + i], blends[i] * inverseT);


        } else if (btmrightLayerIndex != -1 && y > h - blendDistance && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Max(Mathf.InverseLerp(h - (blendDistance / 2), h - blendDistance, y), Mathf.InverseLerp(w - (blendDistance / 2), w - blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = Mathf.Min(map[x, y, i], curBiomeLayers[i] * t);

          float[] blends = btmRight.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmrightLayerIndex]; i++)
            map[x, y, btmrightLayerTextureIndex + i] = Mathf.Max(map[x, y, btmrightLayerTextureIndex + i], blends[i] * inverseT);
        }
      }
    }

    terrain.terrainData.SetAlphamaps(0, 0, map);
  }

  public abstract float[] blendLayer(int x, int y, TerrainData terrainData, float[,] heights);
  public abstract TerrainLayer[] generateLayers();
  public abstract int getNumLayers();

  public virtual void generateDetails(Terrain terrain, Dictionary<int[], Chunk> chunksDict) {
    // Create texture layers
    List<TerrainLayer> layers = new List<TerrainLayer>();
    TerrainLayer[] baseLayers = this.generateLayers();
    layers.AddRange(baseLayers);

    List<Biome> sortedList = new List<Biome>();
    bool texturesAlreadyAdded = false;
    foreach (var chunk in chunksDict) {
      texturesAlreadyAdded = false;

      if (chunk.Value.biome.type == this.type)
        continue;

      foreach (var innerChunk in sortedList)
        if (chunk.Value.biome.type == innerChunk.type) {
          texturesAlreadyAdded = true;
          continue;
        }

      if (!texturesAlreadyAdded) {
        sortedList.Add(chunk.Value.biome);
      }
    }

    sortedList = sortedList.OrderBy(delegate (Biome pair1) {
      return (int)pair1.type;
    }).ToList();

    foreach (var biome in sortedList)
      layers.AddRange(biome.generateLayers());

    int[] layerIndices = sortedList.Select(biome => biome.getNumLayers()).ToArray();

    terrain.terrainData.terrainLayers = layers.ToArray();

    // Blend the terrain layers
    blendTextures(terrain, sortedList, layerIndices, chunksDict);
  }

  public abstract HeightMap generateHeightmap(int size, Vector2 offset);
}
