using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public enum BiomeType {
  DeepOcean,
  Ocean,
  Grassland,
  TemperateForest,
  Jungle,
  Mountains,
  SnowyPeaks,
  Dessert
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

  protected float getSteepness(int x, int y, int width, int height, float[,] heights, Vector3 size) {
    float slopeX = heights[x < width - 1 ? x + 1 : x, y] - heights[x > 0 ? x - 1 : x, y];
    float slopeZ = heights[x, y < height - 1 ? y + 1 : y] - heights[x, y > 0 ? y - 1 : y];

    if (x == 0 || x == width - 1)
      slopeX *= 2;
    if (y == 0 || y == height - 1)
      slopeZ *= 2;

    // Heightmap width = heightmap height
    Vector3 normal = new Vector3(-slopeX, heightmap.maxValue, slopeZ);
    normal.Normalize();

    float steepness = Mathf.Acos(Vector3.Dot(normal, Vector3.up));
    return Mathf.Rad2Deg * steepness;
  }

  protected void blendTextures(Terrain terrain, List<Biome> otherBiomes, int[] layerIndices, Dictionary<int[], Chunk> chunksDict) {
    int w = terrain.terrainData.alphamapWidth;
    int h = terrain.terrainData.alphamapHeight;
    int halfWidth = w / 2;
    int halfHeight = h / 2;
    int[] positinInDictionary = chunksDict.FirstOrDefault(x => x.Value.biome == this).Key;
    int totalNumLayers = terrain.terrainData.terrainLayers.Count();
    float[,,] map = new float[w, h, totalNumLayers];
    Vector2 center = new Vector2(halfWidth, halfHeight);
    int numLayers = getNumLayers();
    int blendDistance = 200;

    Chunk[] chunks = new Chunk[] {
      null, // Top - 0
      null, // Right - 1
      null, // Btm - 2
      null, // left - 3
      null, // Top Left - 4
      null, // Top Right - 5
      null, // Btm Left - 6
      null // Btm Right - 7
    };

    chunksDict.TryGetValue(new int[] { positinInDictionary[0], positinInDictionary[1] - 1 }, out chunks[0]);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] + 1, positinInDictionary[1] }, out chunks[1]);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0], positinInDictionary[1] + 1 }, out chunks[2]);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] - 1, positinInDictionary[1] }, out chunks[3]);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] - 1, positinInDictionary[1] - 1 }, out chunks[4]);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] + 1, positinInDictionary[1] - 1 }, out chunks[5]);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] - 1, positinInDictionary[1] + 1 }, out chunks[6]);
    chunksDict.TryGetValue(new int[] { positinInDictionary[0] + 1, positinInDictionary[1] + 1 }, out chunks[7]);

    // Find predominants for each corner
    if (chunks[4] != null && chunks[0] != null && chunks[3] != null) {
      chunks[4] = (new List<Chunk> { chunks[4], chunks[0], chunks[3] }).OrderByDescending(chunk => chunk.biome.type).ToArray().First();
    }
    if (chunks[5] != null && chunks[0] != null && chunks[1] != null) {
      chunks[5] = (new List<Chunk> { chunks[5], chunks[0], chunks[1] }).OrderByDescending(chunk => chunk.biome.type).ToArray().First();
    }
    if (chunks[6] != null && chunks[2] != null && chunks[3] != null) {
      chunks[6] = (new List<Chunk> { chunks[6], chunks[2], chunks[3] }).OrderByDescending(chunk => chunk.biome.type).ToArray().First();
    }
    if (chunks[7] != null && chunks[2] != null && chunks[1] != null) {
      chunks[7] = (new List<Chunk> { chunks[7], chunks[2], chunks[1] }).OrderByDescending(chunk => chunk.biome.type).ToArray().First();
    }

    for (int i = 0, l = chunks.Length; i < l; i++)
      if (chunks[i] != null && chunks[i].biome.type <= type)
        chunks[i] = null;


    int topLayerIndex = chunks[0] != null ? otherBiomes.FindIndex(biome => biome.type == chunks[0].biome.type) : -1;
    int topLayerTextureIndex = chunks[0] != null ? numLayers + otherBiomes.Sum(biome => biome.type < chunks[0].biome.type ? biome.getNumLayers() : 0) : -1;
    int rightLayerIndex = chunks[1] != null ? otherBiomes.FindIndex(biome => biome.type == chunks[1].biome.type) : -1;
    int rightLayerTextureIndex = chunks[1] != null ? numLayers + otherBiomes.Sum(biome => biome.type < chunks[1].biome.type ? biome.getNumLayers() : 0) : -1;
    int btmLayerIndex = chunks[2] != null ? otherBiomes.FindIndex(biome => biome.type == chunks[2].biome.type) : -1;
    int btmLayerTextureIndex = chunks[2] != null ? numLayers + otherBiomes.Sum(biome => biome.type < chunks[2].biome.type ? biome.getNumLayers() : 0) : -1;
    int leftLayerIndex = chunks[3] != null ? otherBiomes.FindIndex(biome => biome.type == chunks[3].biome.type) : -1;
    int leftLayerTextureIndex = chunks[3] != null ? numLayers + otherBiomes.Sum(biome => biome.type < chunks[3].biome.type ? biome.getNumLayers() : 0) : -1;

    int topleftLayerIndex = chunks[4] != null ? otherBiomes.FindIndex(biome => biome.type == chunks[4].biome.type) : -1;
    int topleftLayerTextureIndex = chunks[4] != null ? numLayers + otherBiomes.Sum(biome => biome.type < chunks[4].biome.type ? biome.getNumLayers() : 0) : -1;
    int toprightLayerIndex = chunks[5] != null ? otherBiomes.FindIndex(biome => biome.type == chunks[5].biome.type) : -1;
    int toprightLayerTextureIndex = chunks[5] != null ? numLayers + otherBiomes.Sum(biome => biome.type < chunks[5].biome.type ? biome.getNumLayers() : 0) : -1;
    int btmleftLayerIndex = chunks[6] != null ? otherBiomes.FindIndex(biome => biome.type == chunks[6].biome.type) : -1;
    int btmleftLayerTextureIndex = chunks[6] != null ? numLayers + otherBiomes.Sum(biome => biome.type < chunks[6].biome.type ? biome.getNumLayers() : 0) : -1;
    int btmrightLayerIndex = chunks[7] != null ? otherBiomes.FindIndex(biome => biome.type == chunks[7].biome.type) : -1;
    int btmrightLayerTextureIndex = chunks[7] != null ? numLayers + otherBiomes.Sum(biome => biome.type < chunks[7].biome.type ? biome.getNumLayers() : 0) : -1;

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

          float[] blends = chunks[3].biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[leftLayerIndex]; i++)
            map[x, y, leftLayerTextureIndex + i] = blends[i] * inverseT;


        }
        // right
        else if (rightLayerIndex != -1 && y > h - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(h - blendDistance - 1, h - 1, y);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = map[x, y, i] * inverseT;

          float[] blends = chunks[1].biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
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

          float[] blends = chunks[0].biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[topLayerIndex]; i++)
            map[x, y, topLayerTextureIndex + i] = Mathf.Max(map[x, y, topLayerTextureIndex + i], blends[i] * inverseT);

        }
        // btm
        else if (btmLayerIndex != -1 && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(w - blendDistance - 1, w - 1, x);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = Mathf.Min(map[x, y, i], curBiomeLayers[i] * inverseT);

          float[] blends = chunks[2].biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmLayerIndex]; i++)
            map[x, y, btmLayerTextureIndex + i] = Mathf.Max(map[x, y, btmLayerTextureIndex + i], blends[i] * t);


        }

        if (topleftLayerIndex != -1 && y < blendDistance && x < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Max(Mathf.InverseLerp(blendDistance / 2, blendDistance, x), Mathf.InverseLerp(blendDistance / 2, blendDistance, y));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < totalNumLayers; i++)
            if (i < topleftLayerTextureIndex || i >= (topleftLayerTextureIndex + layerIndices[topleftLayerIndex]))
              map[x, y, i] = Mathf.Min(map[x, y, i], map[x, y, i] * t);

          float[] blends = chunks[4].biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[topleftLayerIndex]; i++)
            map[x, y, topleftLayerTextureIndex + i] = Mathf.Max(map[x, y, topleftLayerTextureIndex + i], blends[i] * inverseT);


        } else if (toprightLayerIndex != -1 && y > h - blendDistance && x < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Max(Mathf.InverseLerp(h - 1 - (blendDistance / 2), h - 1 - blendDistance, y), Mathf.InverseLerp(blendDistance / 2, blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < totalNumLayers; i++)
            if (i < toprightLayerTextureIndex || i >= (toprightLayerTextureIndex + layerIndices[toprightLayerIndex]))
              map[x, y, i] = Mathf.Min(map[x, y, i], map[x, y, i] * t);

          float[] blends = chunks[5].biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[toprightLayerIndex]; i++)
            map[x, y, toprightLayerTextureIndex + i] = Mathf.Max(map[x, y, toprightLayerTextureIndex + i], blends[i] * inverseT);


        } else if (btmleftLayerIndex != -1 && y < blendDistance && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Max(Mathf.InverseLerp(blendDistance / 2, blendDistance, y), Mathf.InverseLerp(w - 1 - (blendDistance / 2), w - 1 - blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < totalNumLayers; i++)
            if (i < btmleftLayerTextureIndex || i >= (btmleftLayerTextureIndex + layerIndices[btmleftLayerIndex]))
              map[x, y, i] = Mathf.Min(map[x, y, i], map[x, y, i] * t);

          float[] blends = chunks[6].biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmleftLayerIndex]; i++)
            map[x, y, btmleftLayerTextureIndex + i] = Mathf.Max(map[x, y, btmleftLayerTextureIndex + i], blends[i] * inverseT);


        } else if (btmrightLayerIndex != -1 && y > h - blendDistance && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Max(Mathf.InverseLerp(h - 1 - (blendDistance / 2), h - 1 - blendDistance, y), Mathf.InverseLerp(w - 1 - (blendDistance / 2), w - 1 - blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < totalNumLayers; i++)
            if (i < btmrightLayerTextureIndex || i >= (btmrightLayerTextureIndex + layerIndices[btmrightLayerIndex]))
              map[x, y, i] = Mathf.Min(map[x, y, i], map[x, y, i] * t);

          float[] blends = chunks[7].biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
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

      if (chunk.Value.biome.type <= this.type)
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
