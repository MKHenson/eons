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

  public Grassland() : base(BiomeType.Grassland) {
    if (!grass) {
      grassDetail1 = Resources.Load<Texture2D>("Terrain/Textures/grass-billboard-1");
      grass = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless");
      grassNormalMap = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless-norm");
      grass2 = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless-2");
      grassNormalMap2 = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless-2-norm");
      erosionMap = Resources.Load<Texture2D>("Terrain/Textures/erosion-map2");
    }
  }

  public override HeightmapSettings generateHeightmap() {
    HeightmapSettings settings = new HeightmapSettings();
    settings.heightMultiplier = 0.2f;
    settings.useFalloff = true;
    settings.noiseSettings = new NoiseSettings();
    settings.noiseSettings.scale = 200;
    settings.noiseSettings.octaves = 5;
    settings.noiseSettings.lacunarity = 2.1f;
    settings.noiseSettings.persistance = 0.6f;

    settings.heightCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 0), new Keyframe(1, 1) });
    return settings;
  }

  public override TerrainLayer[] generateLayers() {
    TerrainLayer[] toReturn = new TerrainLayer[] {
        new TerrainLayer(),
        new TerrainLayer()
    };

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

  public override Biome generateDetails(Terrain terrain, Dictionary<int[], Chunk> chunksDict) {

    base.generateDetails(terrain, chunksDict);

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
    terrain.terrainData = terrain.terrainData;
    return this;
  }

  public override float[] blendLayer(int x, int y, TerrainData terrainData, float[,] heights) {
    float[] toReturn = new float[2] { 0, 0 };

    int sampleX = (int)(((float)x / (float)(terrainData.alphamapWidth - 1)) * (float)(terrainData.heightmapWidth - 1));
    int sampleY = (int)(((float)y / (float)(terrainData.alphamapHeight - 1)) * (float)(terrainData.heightmapHeight - 1));
    float test1 = getSteepness(sampleX, sampleY, terrainData.heightmapWidth, terrainData.heightmapHeight, heights, terrainData.size);

    float normalized = Mathf.InverseLerp(0, 1, test1);
    toReturn[0] = normalized;
    toReturn[1] = 1f - normalized;
    return toReturn;
  }
}
