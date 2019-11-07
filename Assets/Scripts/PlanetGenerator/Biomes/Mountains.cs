﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mountains : Biome {
  private static Texture2D grass;
  private static Texture2D grassNormalMap;
  private static Texture2D snow;
  private static Texture2D snowNormalMap;
  private static (float start, float end)[] layerGradients = new (float start, float end)[] { (0.1f, 0.5f), (0.5f, 1f) };

  public Mountains() : base(BiomeType.Mountains) {
  }

  public override HeightMap generateHeightmap(int size, Vector2 offset) {
    HeightmapSettings settings = new HeightmapSettings();
    settings.useFalloff = true;
    settings.heightMultiplier = 1;
    settings.noiseSettings = new NoiseSettings();
    settings.noiseSettings.scale = 360;
    settings.noiseSettings.octaves = 6;
    settings.noiseSettings.lacunarity = 2.1f;
    settings.noiseSettings.persistance = 0.6f;

    settings.heightCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.5f, 0.6f), new Keyframe(1, 1) });

    return HeightMapGenerator.generateHeightmap(size, size, settings, offset);
  }

  public override TerrainLayer[] generateLayers() {
    TerrainLayer[] toReturn = new TerrainLayer[] {
        new TerrainLayer(),
        new TerrainLayer()
    };

    if (!grass) {
      grass = Resources.Load<Texture2D>("Terrain/Textures/mountain-gravel");
      grassNormalMap = Resources.Load<Texture2D>("Terrain/Textures/mountain-gravel-normal");
      snow = Resources.Load<Texture2D>("Terrain/Textures/snow-seamless-1");
      snowNormalMap = Resources.Load<Texture2D>("Terrain/Textures/snow-seamless-norm-1");
    }

    toReturn[0].diffuseTexture = grass;
    toReturn[0].normalMapTexture = grassNormalMap;
    toReturn[0].normalScale = 0.8f;
    toReturn[0].specular = new Color(1, 1, 1, 1);
    toReturn[0].tileSize = new Vector2(2, 2);
    toReturn[0].smoothness = 0.3f;

    toReturn[1].diffuseTexture = snow;
    toReturn[1].normalMapTexture = snowNormalMap;
    toReturn[1].normalScale = 0.8f;
    toReturn[1].specular = new Color(1, 1, 1, 1);
    toReturn[1].tileSize = new Vector2(1, 1);
    toReturn[1].smoothness = 0.3f;

    return toReturn;
  }

  public override int getNumLayers() {
    return 2;
  }

  public override void generateDetails(Terrain terrain, Dictionary<int[], Chunk> chunksDict) {

    base.generateDetails(terrain, chunksDict);

    // // Create base later
    // TerrainLayer[] layers = this.generateLayers();
    // terrain.terrainData.terrainLayers = layers;
    // blendTextures(terrain, processedHeightmap.values);

    terrain.terrainData.wavingGrassTint = Color.white;
    terrain.terrainData.wavingGrassSpeed = 1;
    terrain.terrainData.wavingGrassAmount = 0.3f; //
    terrain.terrainData.wavingGrassStrength = 1; // Speed

    terrain.terrainData = terrain.terrainData;
    terrain.Flush();
  }

  public override float[] blendLayer(int x, int y, TerrainData terrainData, float[,] heights) {
    float[] toReturn = new float[layerGradients.Length];

    int sampleX = (int)(((float)x / (float)(terrainData.alphamapWidth - 1)) * (float)(terrainData.heightmapWidth - 1));
    int sampleY = (int)(((float)y / (float)(terrainData.alphamapHeight - 1)) * (float)(terrainData.heightmapHeight - 1));
    float h = heights[sampleX, sampleY];

    for (uint i = 0; i < layerGradients.Length; i++) {
      float drawStrength = Mathf.InverseLerp(layerGradients[i].start, layerGradients[i].end, h);
      toReturn[i] = drawStrength < 0 ? 0 : drawStrength;
    }

    float t = Mathf.InverseLerp(0, heightmap.maxValue, h);
    float t2 = Mathf.InverseLerp(0.0f, 1f, t);

    toReturn[0] = 1 - t2;
    toReturn[1] = t2;

    return toReturn;
  }

  // private void blendTextures(Terrain terrain, float[,] heights) {
  //   float[,,] map = new float[terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight, 2];

  //   // For each point on the alphamap...
  //   for (int y = 0; y < terrain.terrainData.alphamapHeight; y++) {
  //     for (int x = 0; x < terrain.terrainData.alphamapWidth; x++) {
  //       float[] blends = blendLayer(x, y, terrain.terrainData, heights);
  //       // int sampleX = (int)(((float)x / (float)terrain.terrainData.alphamapWidth) * (float)terrain.terrainData.heightmapWidth);
  //       // int sampleY = (int)(((float)y / (float)terrain.terrainData.alphamapHeight) * (float)terrain.terrainData.heightmapHeight);
  //       // float h = heights[sampleX, sampleY];

  //       // for (uint i = 0; i < layerGradients.Length; i++) {
  //       //   float drawStrength = Mathf.InverseLerp(-layerGradients[i].start / 2 - 0.0001f, layerGradients[i].start / 2, h - layerGradients[i].end);
  //       //   map[x, y, i] = drawStrength < 0 ? 0 : drawStrength;
  //       // }

  //       // map[x, y, 0] = Mathf.InverseLerp(0, 1, 1 - (h * h));
  //       // map[x, y, 1] = Mathf.InverseLerp(0, 1, (h * h));

  //       for (uint i = 0; i < blends.Length; i++)
  //         map[x, y, i] = blends[i];
  //     }
  //   }

  //   terrain.terrainData.SetAlphamaps(0, 0, map);
  // }
}
