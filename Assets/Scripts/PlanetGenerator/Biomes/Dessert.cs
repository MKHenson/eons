using System.Collections.Generic;
using UnityEngine;

public class Dessert : Biome {
  private static Texture2D grass;
  private static Texture2D grassNormalMap;
  private static Texture2D snow;
  private static Texture2D snowNormalMap;
  private static (float start, float end)[] layerGradients = new (float start, float end)[] { (-.2f, 0.5f), (0.5f, 1f) };

  public Dessert() : base(BiomeType.Dessert) {
  }

  public override HeightMap generateHeightmap(int size, Vector2 offset) {
    HeightmapSettings settings = new HeightmapSettings();
    settings.heightMultiplier = 0.2f;
    settings.useFalloff = true;
    settings.noiseSettings = new NoiseSettings();
    settings.noiseSettings.scale = 300;
    settings.noiseSettings.octaves = 5;
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
      grass = Resources.Load<Texture2D>("Terrain/Textures/sand");
      grassNormalMap = Resources.Load<Texture2D>("Terrain/Textures/sand-normal");
      snow = Resources.Load<Texture2D>("Terrain/Textures/sand-seamless-2");
      snowNormalMap = Resources.Load<Texture2D>("Terrain/Textures/sand-seamless-2-normal");
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

    // Create base later
    TerrainLayer[] layers = this.generateLayers();
    terrain.terrainData.terrainLayers = layers;

    blendTextures(terrain, processedHeightmap.values);

    terrain.terrainData.wavingGrassTint = Color.white;
    terrain.terrainData.wavingGrassSpeed = 1;
    terrain.terrainData.wavingGrassAmount = 0.3f; //
    terrain.terrainData.wavingGrassStrength = 1; // Speed

    terrain.terrainData = terrain.terrainData;
    terrain.Flush();
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

  private void blendTextures(Terrain terrain, float[,] heights) {
    float[,,] map = new float[terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight, 2];

    // For each point on the alphamap...
    for (int y = 0; y < terrain.terrainData.alphamapHeight; y++) {
      for (int x = 0; x < terrain.terrainData.alphamapWidth; x++) {
        float[] blends = blendLayer(x, y, terrain.terrainData, heights);
        for (uint i = 0; i < blends.Length; i++)
          map[x, y, i] = blends[i];
      }
    }

    terrain.terrainData.SetAlphamaps(0, 0, map);
  }
}
