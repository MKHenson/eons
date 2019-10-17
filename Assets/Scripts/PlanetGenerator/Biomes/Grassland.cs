using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Grassland : Biome {
  protected enum Corner {
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
  }

  private static Texture2D grass;
  private static Texture2D grassNormalMap;
  private static Texture2D grass2;
  private static Texture2D grassNormalMap2;
  private static Texture2D grassDetail1;

  private Biome topLeft;
  private Biome topRight;
  private Biome btmLeft;
  private Biome btmRight;

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

  public override void generateDetails(Terrain terrain, Dictionary<int[], Chunk> chunksDict) {

    // Create texture layers
    List<TerrainLayer> layers = new List<TerrainLayer>();
    TerrainLayer[] baseLayers = this.generateLayers();
    layers.AddRange(baseLayers);

    topLeft = getCornerBiome(chunksDict, Corner.TopLeft);
    if (topLeft != null)
      layers.AddRange(topLeft.generateLayers());

    topRight = getCornerBiome(chunksDict, Corner.TopRight);
    if (topRight != null)
      layers.AddRange(topRight.generateLayers());

    btmLeft = getCornerBiome(chunksDict, Corner.BottomLeft);
    if (btmLeft != null)
      layers.AddRange(btmLeft.generateLayers());

    btmRight = getCornerBiome(chunksDict, Corner.BottomRight);
    if (btmRight != null)
      layers.AddRange(btmRight.generateLayers());

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
    blendTextures(terrain);

    terrain.terrainData = terrain.terrainData;
  }

  protected Biome getCornerBiome(Dictionary<int[], Chunk> chunksDict, Corner corner) {
    Chunk[] neighbours = new Chunk[3] { null, null, null };

    switch (corner) {
      case Corner.TopLeft:
        Chunk TLLeft = null;
        Chunk TLTopLeft = null;
        Chunk TLTop = null;
        if (chunksDict.TryGetValue(new int[] { -1 + -position.x, -position.y }, out TLLeft))
          neighbours[0] = TLLeft;
        if (chunksDict.TryGetValue(new int[] { -1 + -position.x, -1 + -position.y }, out TLTopLeft))
          neighbours[1] = TLTopLeft;
        if (chunksDict.TryGetValue(new int[] { -position.x, -1 + -position.y }, out TLTop))
          neighbours[2] = TLTop;
        break;

      case Corner.TopRight:
        Chunk TRTop = null;
        Chunk TRTopRight = null;
        Chunk TRRight = null;
        if (chunksDict.TryGetValue(new int[] { -position.x, -position.y - 1 }, out TRTop))
          neighbours[0] = TRTop;
        if (chunksDict.TryGetValue(new int[] { -position.x + 1, -position.y - 1 }, out TRTopRight))
          neighbours[1] = TRTopRight;
        if (chunksDict.TryGetValue(new int[] { -position.x + 1, -position.y }, out TRRight))
          neighbours[2] = TRRight;
        break;

      case Corner.BottomLeft:
        Chunk BLLeft = null;
        Chunk BLBtmLeft = null;
        Chunk BLBtm = null;
        if (chunksDict.TryGetValue(new int[] { -1 + -position.x, -position.y }, out BLLeft))
          neighbours[0] = BLLeft;
        if (chunksDict.TryGetValue(new int[] { -1 + -position.x, +1 + -position.y }, out BLBtmLeft))
          neighbours[1] = BLBtmLeft;
        if (chunksDict.TryGetValue(new int[] { -position.x, +1 + -position.y }, out BLBtm))
          neighbours[2] = BLBtm;
        break;

      case Corner.BottomRight:
        Chunk BRRight = null;
        Chunk BRBtmRight = null;
        Chunk BRBtm = null;
        if (chunksDict.TryGetValue(new int[] { -position.x + 1, -position.y }, out BRRight))
          neighbours[0] = BRRight;
        if (chunksDict.TryGetValue(new int[] { -position.x + 1, -position.y + 1 }, out BRBtmRight))
          neighbours[1] = BRBtmRight;
        if (chunksDict.TryGetValue(new int[] { -position.x, -position.y + 1 }, out BRBtm))
          neighbours[2] = BRBtm;
        break;
    }

    var biomeTypeWithHighestPresident = neighbours.Max(chunk => chunk != null ? chunk.biome.type : 0);
    var chunkWithHighestPresident = neighbours.Where(chunk => chunk != null && chunk.biome.type == biomeTypeWithHighestPresident ? true : false);

    if (chunkWithHighestPresident.Count() != 0)
      return chunkWithHighestPresident.First().biome;

    return null;
  }

  bool PointInsideSphere(Vector2 point, Vector2 center, float radius) {
    return Vector2.Distance(point, center) < radius;
  }

  public override float[] blendLayer(int x, int y, TerrainData terrainData, float[,] heights) {
    float[] toReturn = new float[2] { 0, 0 };

    // Get the normalized terrain coordinate that
    // corresponds to the the point.
    float normX = x * 1.0f / (terrainData.alphamapWidth - 1);
    float normY = y * 1.0f / (terrainData.alphamapHeight - 1);

    // Get the steepness value at the normalized coordinate.
    var angle = terrainData.GetSteepness(normX, normY);

    // Steepness is given as an angle, 0..90 degrees. Divide
    // by 90 to get an alpha blending value in the range 0..1.
    var frac = angle / 10.0;
    toReturn[0] = (float)frac;
    toReturn[1] = (float)(1 - frac);

    return toReturn;
  }

  private void blendTextures(Terrain terrain) {
    int halfWidth = terrain.terrainData.alphamapWidth / 2;
    int halfHeight = terrain.terrainData.alphamapHeight / 2;

    // int numLayers = 2;
    // if (topLeft != null)
    //   numLayers++;

    float[,,] map = new float[terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight, terrain.terrainData.terrainLayers.Count()];
    Vector2 center = new Vector2(halfWidth, halfHeight);

    // For each point on the alphamap...
    for (int y = 0; y < terrain.terrainData.alphamapHeight; y++) {
      for (int x = 0; x < terrain.terrainData.alphamapWidth; x++) {

        Vector2 xyPos = new Vector2(x, y);
        float[] curBiomeLayers = blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

        if (topLeft != null && x < halfWidth && y < halfHeight && !PointInsideSphere(xyPos, center, halfWidth)) {
          map[x, y, 0] = 0;
          map[x, y, 1] = 0;

          float[] blends = topLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          map[x, y, 2] = blends[0];
          map[x, y, 3] = blends[1];

        } else if (topRight != null && y < halfHeight && !PointInsideSphere(xyPos, center, halfWidth)) {
          map[x, y, 0] = 0;
          map[x, y, 1] = 0;
          map[x, y, 2] = 1;

          float[] blends = topRight.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          map[x, y, 2] = blends[0];
          map[x, y, 3] = blends[1];
        } else if (btmLeft != null && x < halfWidth && !PointInsideSphere(xyPos, center, halfWidth)) {
          map[x, y, 0] = 0;
          map[x, y, 1] = 0;

          float[] blends = btmLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          map[x, y, 2] = blends[0];
          map[x, y, 3] = blends[1];

        } else if (btmRight != null && !PointInsideSphere(xyPos, center, halfWidth)) {
          map[x, y, 0] = 0;
          map[x, y, 1] = 0;

          float[] blends = btmRight.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          map[x, y, 2] = blends[0];
          map[x, y, 3] = blends[1];

        } else {
          // // Get the normalized terrain coordinate that
          // // corresponds to the the point.
          // float normX = x * 1.0f / (terrain.terrainData.alphamapWidth - 1);
          // float normY = y * 1.0f / (terrain.terrainData.alphamapHeight - 1);

          // // Get the steepness value at the normalized coordinate.
          // var angle = terrain.terrainData.GetSteepness(normX, normY);

          // // Steepness is given as an angle, 0..90 degrees. Divide
          // // by 90 to get an alpha blending value in the range 0..1.
          // var frac = angle / 10.0;
          // map[x, y, 0] = (float)frac;
          // map[x, y, 1] = (float)(1 - frac);


          map[x, y, 0] = curBiomeLayers[0];
          map[x, y, 1] = curBiomeLayers[1];
        }
      }
    }

    terrain.terrainData.SetAlphamaps(0, 0, map);
  }
}
