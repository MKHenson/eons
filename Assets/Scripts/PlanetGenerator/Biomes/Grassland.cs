using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Grassland : Biome {
  private static Texture2D grass;
  private static Texture2D grassNormalMap;
  private static Texture2D grass2;
  private static Texture2D grassNormalMap2;
  private static Texture2D grassDetail1;
  private static Texture2D erosionMap;
  private static float[,] blendMask;

  public Grassland() : base(BiomeType.Grassland) {
  }

  public override HeightMap generateHeightmap(int size, Vector2 offset) {
    HeightmapSettings settings = new HeightmapSettings();
    settings.heightMultiplier = 0.2f;
    settings.useFalloff = true;
    settings.noiseSettings = new NoiseSettings();
    settings.noiseSettings.scale = 200;
    settings.noiseSettings.octaves = 5;
    settings.noiseSettings.lacunarity = 2.1f;
    settings.noiseSettings.persistance = 0.6f;

    settings.heightCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 0), new Keyframe(1, 1) });

    if (blendMask == null) {
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
    }

    return HeightMapGenerator.generateHeightmap(size, size, settings, offset);
  }

  public override TerrainLayer[] generateLayers() {
    TerrainLayer[] toReturn = new TerrainLayer[] {
        new TerrainLayer(),
        new TerrainLayer()
    };

    if (!grass) {
      grassDetail1 = Resources.Load<Texture2D>("Terrain/Textures/grass-billboard-1");
      grass = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless");
      grassNormalMap = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless-norm");
      grass2 = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless-2");
      grassNormalMap2 = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless-2-norm");
      erosionMap = Resources.Load<Texture2D>("Terrain/Textures/erosion-map2");
    }

    toReturn[0].diffuseTexture = grass;
    toReturn[0].normalMapTexture = grassNormalMap;
    toReturn[0].normalScale = 0.3f;
    toReturn[0].tileSize = new Vector2(2, 2);

    toReturn[1].diffuseTexture = grass2;
    toReturn[1].normalMapTexture = grassNormalMap2;
    toReturn[1].normalScale = 0.5f;
    toReturn[1].tileSize = new Vector2(2, 2);

    return toReturn;
  }

  public override int getNumLayers() {
    return 2;
  }

  public override void generateDetails(Terrain terrain, Dictionary<int[], Chunk> chunksDict) {

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

    sortedList.OrderBy(delegate (Biome pair1) {
      return (int)pair1.type;
    });

    foreach (var biome in sortedList)
      layers.AddRange(biome.generateLayers());

    int[] layerIndices = sortedList.Select(biome => biome.getNumLayers()).ToArray();

    terrain.terrainData.terrainLayers = layers.ToArray();

    // Create the grass meshes
    DetailPrototype[] detailPrototypes = new DetailPrototype[] {
      new DetailPrototype()
    };

    detailPrototypes[0].prototypeTexture = grassDetail1;
    detailPrototypes[0].renderMode = DetailRenderMode.Grass;
    detailPrototypes[0].healthyColor = Color.white;
    detailPrototypes[0].dryColor = new Color(1, 1, 0.7f, 1);
    detailPrototypes[0].minWidth = 2;
    detailPrototypes[0].maxWidth = 2.5f;
    detailPrototypes[0].minHeight = 1;
    detailPrototypes[0].maxHeight = 2;

    // Now define the grass locations on the map
    int[,] grass1Locations = new int[terrain.terrainData.detailWidth, terrain.terrainData.detailHeight];
    for (int y = 0; y < terrain.terrainData.detailHeight; y++) {
      for (int x = 0; x < terrain.terrainData.detailWidth; x++) {
        grass1Locations[x, y] = Random.Range(0, 2);
      }
    }

    terrain.terrainData.wavingGrassTint = Color.white;
    terrain.terrainData.wavingGrassSpeed = 1;
    terrain.terrainData.wavingGrassAmount = 0.3f; //
    terrain.terrainData.wavingGrassStrength = 1; // Speed
    terrain.terrainData.detailPrototypes = detailPrototypes;
    terrain.terrainData.SetDetailLayer(0, 0, 0, grass1Locations);

    // Blend the terrain layers
    blendTextures(terrain, sortedList, layerIndices, chunksDict);

    terrain.terrainData = terrain.terrainData;
  }

  public override float[] blendLayer(int x, int y, TerrainData terrainData, float[,] heights) {
    float[] toReturn = new float[2] { 0, 0 };

    // Get the normalized terrain coordinate that
    // corresponds to the the point.
    float normX = x * 1.0f / (terrainData.alphamapWidth - 1);
    float normY = y * 1.0f / (terrainData.alphamapHeight - 1);

    // Get the steepness value at the normalized coordinate.
    float angle = terrainData.GetSteepness(normX, normY);

    // Steepness is given as an angle, 0..90 degrees. Divide
    // by 90 to get an alpha blending value in the range 0..1.
    float frac = angle / 90.0f;
    float normalized = Mathf.InverseLerp(-0.3f, 0.3f, frac);
    toReturn[0] = normalized;
    toReturn[1] = 1f - normalized;

    return toReturn;
  }

  private float sampleBlendMask(float x, float y) {
    return blendMask[(int)(x * 511), (int)(y * 511)];
  }

  private void blendTextures(Terrain terrain, List<Biome> otherBiomes, int[] layerIndices, Dictionary<int[], Chunk> chunksDict) {
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
    int topLayerTextureIndex = numLayers + otherBiomes.Sum(biome => biome.type < top.biome.type ? biome.getNumLayers() : 0);
    int rightLayerIndex = right != null ? otherBiomes.FindIndex(biome => biome.type == right.biome.type) : -1;
    int rightLayerTextureIndex = numLayers + otherBiomes.Sum(biome => biome.type < right.biome.type ? biome.getNumLayers() : 0);
    int btmLayerIndex = bottom != null ? otherBiomes.FindIndex(biome => biome.type == bottom.biome.type) : -1;
    int btmLayerTextureIndex = numLayers + otherBiomes.Sum(biome => biome.type < bottom.biome.type ? biome.getNumLayers() : 0);
    int leftLayerIndex = left != null ? otherBiomes.FindIndex(biome => biome.type == left.biome.type) : -1;
    int leftLayerTextureIndex = numLayers + otherBiomes.Sum(biome => biome.type < left.biome.type ? biome.getNumLayers() : 0);
    int topleftLayerIndex = topLeft != null ? otherBiomes.FindIndex(biome => biome.type == topLeft.biome.type) : -1;
    int topleftLayerTextureIndex = numLayers + otherBiomes.Sum(biome => biome.type < topLeft.biome.type ? biome.getNumLayers() : 0);
    int toprightLayerIndex = topLeft != null ? otherBiomes.FindIndex(biome => biome.type == topRight.biome.type) : -1;
    int toprightLayerTextureIndex = numLayers + otherBiomes.Sum(biome => biome.type < topRight.biome.type ? biome.getNumLayers() : 0);
    int btmleftLayerIndex = btmLeft != null ? otherBiomes.FindIndex(biome => biome.type == btmLeft.biome.type) : -1;
    int btmleftLayerTextureIndex = numLayers + otherBiomes.Sum(biome => biome.type < btmLeft.biome.type ? biome.getNumLayers() : 0);
    int btmrightLayerIndex = btmRight != null ? otherBiomes.FindIndex(biome => biome.type == btmRight.biome.type) : -1;
    int btmrightLayerTextureIndex = numLayers + otherBiomes.Sum(biome => biome.type < btmRight.biome.type ? biome.getNumLayers() : 0);

    bool insideSquare = true;
    // For each point on the alphamap...
    for (int y = 0; y < h; y++) {
      for (int x = 0; x < w; x++) {

        Vector2 xyPos = new Vector2(x, y);
        insideSquare = true;

        float[] curBiomeLayers = blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

        // left
        if (leftLayerIndex != -1 && y < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(0, blendDistance, y);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * t;

          float[] blends = left.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[leftLayerIndex]; i++)
            map[x, y, leftLayerTextureIndex + i] = blends[i] * inverseT;

          insideSquare = false;

        }
        // right
        else if (rightLayerIndex != -1 && y > h - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(h - blendDistance, h, y);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * inverseT;

          float[] blends = right.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[rightLayerIndex]; i++)
            map[x, y, rightLayerTextureIndex + i] = blends[i] * t;

          insideSquare = false;
        }

        // top
        if (topLayerIndex != -1 && x < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(0, blendDistance, x);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * t;

          float[] blends = top.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[topLayerIndex]; i++)
            map[x, y, topLayerTextureIndex + i] = blends[i] * inverseT;

          insideSquare = false;
        }
        // btm
        else if (btmLayerIndex != -1 && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(w - blendDistance, w, x);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * inverseT;

          float[] blends = bottom.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmLayerIndex]; i++)
            map[x, y, btmLayerTextureIndex + i] = blends[i] * t;

          insideSquare = false;
        }

        // Top left corner
        if (topleftLayerIndex != -1 && y < blendDistance && x < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Min(Mathf.InverseLerp(0, blendDistance, y), Mathf.InverseLerp(0, blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * t;

          float[] blends = right.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[topleftLayerIndex]; i++)
            map[x, y, topleftLayerTextureIndex + i] = blends[i] * inverseT;

          insideSquare = false;
        }
        // Top Right corner
        else if (toprightLayerIndex != -1 && y < blendDistance && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Min(Mathf.InverseLerp(0, blendDistance, y), Mathf.InverseLerp(w, w - blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * t;

          float[] blends = topRight.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[toprightLayerIndex]; i++)
            map[x, y, toprightLayerTextureIndex + i] = blends[i] * inverseT;

          insideSquare = false;
        }
        // Btm left corner
        else if (btmleftLayerIndex != -1 && y > h - blendDistance && x < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Min(Mathf.InverseLerp(h, h - blendDistance, y), Mathf.InverseLerp(0, blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * t;

          float[] blends = btmLeft.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmleftLayerIndex]; i++)
            map[x, y, btmleftLayerTextureIndex + i] = blends[i] * inverseT;

          insideSquare = false;
        }
        // Btm Right corner
        else if (btmrightLayerIndex != -1 && y > h - blendDistance && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.Min(Mathf.InverseLerp(h, h - blendDistance, y), Mathf.InverseLerp(w, w - blendDistance, x));
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * t;

          float[] blends = btmRight.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmrightLayerIndex]; i++)
            map[x, y, btmrightLayerTextureIndex + i] = blends[i] * inverseT;

          insideSquare = false;
        }

        if (insideSquare) {
          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i];
        }
      }
    }

    terrain.terrainData.SetAlphamaps(0, 0, map);
  }
}
