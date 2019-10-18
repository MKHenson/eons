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
      maskSettings.noiseSettings.scale = 5;
      maskSettings.noiseSettings.octaves = 6;
      maskSettings.noiseSettings.lacunarity = 1.8f;
      maskSettings.noiseSettings.persistance = 0.51f;

      maskSettings.heightCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 1), new Keyframe(1, 1) });
      blendMask = HeightMapGenerator.generateHeightmap(512, 512, maskSettings, offset).values;
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

  float distanceToCircle(Vector2 point, Vector2 center, float radius) {
    return Mathf.Abs(Vector2.Distance(point, center) - radius);
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

  /* A utility function to calculate area of triangle formed by (x1, y1),  
   (x2, y2) and (x3, y3) */
  private float area(float x1, float y1, float x2, float y2, float x3, float y3) {
    return Mathf.Abs((x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2)) / 2.0f);
  }

  /* A function to check whether point P(x, y) lies inside the triangle formed  
     by A(x1, y1), B(x2, y2) and C(x3, y3) */
  bool isInside(float x1, float y1, float x2, float y2, float x3, float y3, float x, float y) {
    /* Calculate area of triangle ABC */
    float A = area(x1, y1, x2, y2, x3, y3);

    /* Calculate area of triangle PBC */
    float A1 = area(x, y, x2, y2, x3, y3);

    /* Calculate area of triangle PAC */
    float A2 = area(x1, y1, x, y, x3, y3);

    /* Calculate area of triangle PAB */
    float A3 = area(x1, y1, x2, y2, x, y);

    /* Check if sum of A1, A2 and A3 is same as A */
    return (A == A1 + A2 + A3);
  }

  /** 
   Calculate the distance between
   point pt and the segment p1 --> p2.
  */
  private float findDistanceToSegment(Vector2 pt, Vector2 p1, Vector2 p2) {
    float dx = p2.x - p1.x;
    float dy = p2.y - p1.y;

    if ((dx == 0) && (dy == 0)) {
      // It's a point not a line segment.
      dx = pt.x - p1.x;
      dy = pt.y - p1.y;
      return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // Calculate the t that minimizes the distance.
    float t = ((pt.x - p1.x) * dx + (pt.y - p1.y) * dy) /
        (dx * dx + dy * dy);

    // See if this represents one of the segment's
    // end points or a point in the middle.
    if (t < 0) {
      Vector2 closest = new Vector2(p1.x, p1.y);
      dx = pt.x - p1.x;
      dy = pt.y - p1.y;
    } else if (t > 1) {
      Vector2 closest = new Vector2(p2.x, p2.y);
      dx = pt.x - p2.x;
      dy = pt.y - p2.y;
    } else {
      Vector2 closest = new Vector2(p1.x + t * dx, p1.y + t * dy);
      dx = pt.x - closest.x;
      dy = pt.y - closest.y;
    }

    return Mathf.Sqrt(dx * dx + dy * dy);
  }

  private float sampleBlendMask(float x, float y) {
    return blendMask[(int)(x * 511), (int)(y * 511)];
  }

  private void blendTextures(Terrain terrain) {
    int w = terrain.terrainData.alphamapWidth;
    int h = terrain.terrainData.alphamapHeight;
    int halfWidth = w / 2;
    int halfHeight = h / 2;

    // int numLayers = 2;
    // if (topLeft != null)
    //   numLayers++;

    float[,,] map = new float[w, h, terrain.terrainData.terrainLayers.Count()];
    Vector2 center = new Vector2(halfWidth, halfHeight);

    // For each point on the alphamap...
    for (int y = 0; y < h; y++) {
      for (int x = 0; x < w; x++) {

        Vector2 xyPos = new Vector2(x, y);

        float[] curBiomeLayers = blendLayer(x, y, terrain.terrainData, processedHeightmap.values);


        if (topLeft != null && isInside(0, 0, halfWidth, 0, 0, halfHeight, x, y)) {

          // float distanceToRadius = distanceToCircle(xyPos, center, halfWidth);
          // float t = Mathf.InverseLerp(20, 0, distanceToRadius);
          // float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);



          // Other
          float[] blends = topLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

          float distToPlane = findDistanceToSegment(xyPos, new Vector2(halfWidth, 0), new Vector2(0, halfHeight));
          float t = Mathf.InverseLerp(40, 0, distToPlane);

          // Grass
          map[x, y, 0] = curBiomeLayers[0] * (t);
          map[x, y, 1] = curBiomeLayers[1] * (t);

          map[x, y, 2] = blends[0] * (1 - t);
          map[x, y, 3] = blends[1] * (1 - t);

          for (int i = 0; i < 4; i++)
            map[x, y, i] = Mathf.Min(Mathf.Max(map[x, y, i], 0), 1);

        } else if (topLeft != null && x < halfWidth && y < halfHeight && !PointInsideSphere(xyPos, center, halfWidth)) {

          float distanceToRadius = distanceToCircle(xyPos, center, halfWidth);
          float t = Mathf.InverseLerp(20, 0, distanceToRadius);

          // Grass
          map[x, y, 0] = curBiomeLayers[0] * t;
          map[x, y, 1] = curBiomeLayers[1] * t;

          // Other
          float[] blends = topLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

          map[x, y, 2] = blends[0] * (1 - t);
          map[x, y, 3] = blends[1] * (1 - t);

        } else if (topRight != null && y < halfHeight && !PointInsideSphere(xyPos, center, halfWidth)) {
          float distanceToRadius = distanceToCircle(xyPos, center, halfWidth);
          float t = Mathf.InverseLerp(20, 0, distanceToRadius);

          // Grass
          map[x, y, 0] = curBiomeLayers[0] * t;
          map[x, y, 1] = curBiomeLayers[1] * t;

          float[] blends = topRight.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          map[x, y, 2] = blends[0] * (1 - t);
          map[x, y, 3] = blends[1] * (1 - t);
        } else if (btmLeft != null && x < halfWidth && !PointInsideSphere(xyPos, center, halfWidth)) {
          float distanceToRadius = distanceToCircle(xyPos, center, halfWidth);
          float t = Mathf.InverseLerp(20, 0, distanceToRadius);

          // Grass
          map[x, y, 0] = curBiomeLayers[0] * t;
          map[x, y, 1] = curBiomeLayers[1] * t;

          float[] blends = btmLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          map[x, y, 2] = blends[0] * (1 - t);
          map[x, y, 3] = blends[1] * (1 - t);

        } else if (btmRight != null && !PointInsideSphere(xyPos, center, halfWidth)) {
          float distanceToRadius = distanceToCircle(xyPos, center, halfWidth);
          float t = Mathf.InverseLerp(20, 0, distanceToRadius);

          // Grass
          map[x, y, 0] = curBiomeLayers[0] * t;
          map[x, y, 1] = curBiomeLayers[1] * t;

          float[] blends = btmRight.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          map[x, y, 2] = blends[0] * (1 - t);
          map[x, y, 3] = blends[1] * (1 - t);

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
