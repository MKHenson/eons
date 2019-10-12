using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grassland : Biome {
  private static Texture2D grass;
  private static Texture2D grassNormalMap;
  private static Texture2D grass2;
  private static Texture2D grassNormalMap2;
  private static Texture2D grassDetail1;

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

  public override void generateDetails(Terrain terrain) {
    DetailPrototype[] detailPrototypes = new DetailPrototype[] {
      new DetailPrototype()
    };

    int[,] grass1Locations = new int[terrain.terrainData.detailWidth, terrain.terrainData.detailHeight];
    for (int y = 0; y < terrain.terrainData.detailHeight; y++) {
      for (int x = 0; x < terrain.terrainData.detailWidth; x++) {
        grass1Locations[x, y] = Random.Range(0, 2);
      }
    }

    detailPrototypes[0].prototypeTexture = grassDetail1;
    detailPrototypes[0].renderMode = DetailRenderMode.Grass;
    detailPrototypes[0].healthyColor = Color.white;
    detailPrototypes[0].dryColor = Color.white;
    detailPrototypes[0].minWidth = 2;
    detailPrototypes[0].maxWidth = 2.5f;
    detailPrototypes[0].minHeight = 1;
    detailPrototypes[0].maxHeight = 2;


    terrain.terrainData.detailPrototypes = detailPrototypes;
    terrain.terrainData.SetDetailLayer(0, 0, 0, grass1Locations);
    blendTextures(terrain);

    terrain.terrainData.wavingGrassTint = Color.white;
    terrain.terrainData.wavingGrassSpeed = 1;
    terrain.terrainData.wavingGrassAmount = 0.3f; //
    terrain.terrainData.wavingGrassStrength = 1; // Speed


    terrain.terrainData = terrain.terrainData;
    terrain.Flush();
  }

  private void blendTextures(Terrain terrain) {
    float[,,] map = new float[terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight, 2];

    // For each point on the alphamap...
    for (int y = 0; y < terrain.terrainData.alphamapHeight; y++) {
      for (int x = 0; x < terrain.terrainData.alphamapWidth; x++) {
        // Get the normalized terrain coordinate that
        // corresponds to the the point.
        float normX = x * 1.0f / (terrain.terrainData.alphamapWidth - 1);
        float normY = y * 1.0f / (terrain.terrainData.alphamapHeight - 1);

        // Get the steepness value at the normalized coordinate.
        var angle = terrain.terrainData.GetSteepness(normX, normY);

        // Steepness is given as an angle, 0..90 degrees. Divide
        // by 90 to get an alpha blending value in the range 0..1.
        var frac = angle / 10.0;
        map[x, y, 0] = (float)frac;
        map[x, y, 1] = (float)(1 - frac);
      }
    }

    terrain.terrainData.SetAlphamaps(0, 0, map);
  }
}
