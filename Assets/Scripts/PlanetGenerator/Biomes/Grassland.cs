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
  private static Texture2D erosionMap;

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

    sortedList.Sort(delegate (Biome pair1, Biome pair2) {
      return (int)pair1.type;
    });

    foreach (var biome in sortedList)
      layers.AddRange(biome.generateLayers());

    int[] layerIndices = sortedList.Select(biome => biome.getNumLayers()).ToArray();

    // topLeft = getCornerBiome(chunksDict, Corner.TopLeft);
    // if (topLeft != null)
    //   layers.AddRange(topLeft.generateLayers());

    // topRight = getCornerBiome(chunksDict, Corner.TopRight);
    // if (topRight != null)
    //   layers.AddRange(topRight.generateLayers());

    // btmLeft = getCornerBiome(chunksDict, Corner.BottomLeft);
    // if (btmLeft != null)
    //   layers.AddRange(btmLeft.generateLayers());

    // btmRight = getCornerBiome(chunksDict, Corner.BottomRight);
    // if (btmRight != null)
    //   layers.AddRange(btmRight.generateLayers());

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
    float angle = terrainData.GetSteepness(normX, normY);

    // Steepness is given as an angle, 0..90 degrees. Divide
    // by 90 to get an alpha blending value in the range 0..1.
    float frac = angle / 90.0f;
    float normalized = Mathf.InverseLerp(-0.3f, 0.3f, frac);
    toReturn[0] = normalized;
    toReturn[1] = 1f - normalized;

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
    // return erosionMap.GetPixel((int)(x * 511), (int)(y * 511)).r;

    return blendMask[(int)(x * 511), (int)(y * 511)];
  }

  private void diamond(int x, int y, int w, int h, int halfWidth, int halfHeight, Terrain terrain, float[,,] map) {
    Vector2 xyPos = new Vector2(x, y);

    float[] curBiomeLayers = blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
    float gradietToPlaneMin = 10;
    float gradietToPlaneMax = 0;
    float borderStartGradient = 0;
    float borderEndGradient = 150;

    if (topLeft != null && isInside(0, 0, halfWidth, 0, 0, halfHeight, x, y)) {
      float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
      float xMask = Mathf.InverseLerp(borderStartGradient, borderEndGradient, x);
      float yMask = Mathf.InverseLerp(borderStartGradient, borderEndGradient, y);
      float borderMask = Mathf.Min(xMask, yMask);
      mask = Mathf.Min(mask, borderMask);

      // Other
      float[] blends = topLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

      float distToPlane = findDistanceToSegment(xyPos, new Vector2(halfWidth, 0), new Vector2(0, halfHeight));
      float t = Mathf.Max(Mathf.InverseLerp(gradietToPlaneMin, gradietToPlaneMax, distToPlane), mask);

      // Grass
      map[x, y, 0] = curBiomeLayers[0] * (t);
      map[x, y, 1] = curBiomeLayers[1] * (t);

      map[x, y, 2] = blends[0] * (1 - t);
      map[x, y, 3] = blends[1] * (1 - t);

    } else if (topRight != null && isInside(w - halfWidth, 0, w, 0, w, halfHeight, x, y)) {
      float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
      float xMask = Mathf.InverseLerp(w - borderStartGradient, w - borderEndGradient, x);
      float yMask = Mathf.InverseLerp(borderStartGradient, borderEndGradient, y);
      float borderMask = Mathf.Min(xMask, yMask);
      mask = Mathf.Min(mask, borderMask);

      // Other
      float[] blends = topLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

      float distToPlane = findDistanceToSegment(xyPos, new Vector2(w - halfWidth, 0), new Vector2(w, halfHeight));
      float t = Mathf.Max(Mathf.InverseLerp(gradietToPlaneMin, gradietToPlaneMax, distToPlane), mask);

      // Grass
      map[x, y, 0] = curBiomeLayers[0] * (t);
      map[x, y, 1] = curBiomeLayers[1] * (t);

      map[x, y, 2] = blends[0] * (1 - t);
      map[x, y, 3] = blends[1] * (1 - t);

    } else if (btmLeft != null && isInside(0, h - halfHeight, halfWidth, h, 0, h, x, y)) {
      float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
      float xMask = Mathf.InverseLerp(borderStartGradient, borderEndGradient, x);
      float yMask = Mathf.InverseLerp(h - borderStartGradient, h - borderEndGradient, y);
      float borderMask = Mathf.Min(xMask, yMask);
      mask = Mathf.Min(mask, borderMask);

      // Other
      float[] blends = topLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

      float distToPlane = findDistanceToSegment(xyPos, new Vector2(0, h - halfHeight), new Vector2(halfWidth, h));
      float t = Mathf.Max(Mathf.InverseLerp(gradietToPlaneMin, gradietToPlaneMax, distToPlane), mask);

      // Grass
      map[x, y, 0] = curBiomeLayers[0] * (t);
      map[x, y, 1] = curBiomeLayers[1] * (t);

      map[x, y, 2] = blends[0] * (1 - t);
      map[x, y, 3] = blends[1] * (1 - t);

    } else if (btmLeft != null && isInside(w - halfWidth, h, w, h, w, h - halfHeight, x, y)) {
      float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
      float xMask = Mathf.InverseLerp(w - borderStartGradient, w - borderEndGradient, x);
      float yMask = Mathf.InverseLerp(h - borderStartGradient, h - borderEndGradient, y);
      float borderMask = Mathf.Min(xMask, yMask);
      mask = Mathf.Min(mask, borderMask);

      // Other
      float[] blends = topLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

      float distToPlane = findDistanceToSegment(xyPos, new Vector2(w - halfWidth, h), new Vector2(w, h - halfHeight));
      float t = Mathf.Max(Mathf.InverseLerp(gradietToPlaneMin, gradietToPlaneMax, distToPlane), mask);

      // Grass
      map[x, y, 0] = curBiomeLayers[0] * (t);
      map[x, y, 1] = curBiomeLayers[1] * (t);

      map[x, y, 2] = blends[0] * (1 - t);
      map[x, y, 3] = blends[1] * (1 - t);

    } else {
      map[x, y, 0] = curBiomeLayers[0];
      map[x, y, 1] = curBiomeLayers[1];
    }
  }

  private void sphere(int x, int y, int w, int h, int halfWidth, int halfHeight, Terrain terrain, float[,,] map) {
    // if (topLeft != null && x < halfWidth && y < halfHeight && !PointInsideSphere(xyPos, center, halfWidth)) {

    //   // float distanceToRadius = distanceToCircle(xyPos, center, halfWidth);
    //   // float t = Mathf.InverseLerp(20, 0, distanceToRadius);

    //   // float xMask = Mathf.InverseLerp(30, 90, x);
    //   // float yMask = Mathf.InverseLerp(30, 90, y);
    //   // float borderMask = Mathf.Min(xMask, yMask);
    //   // float mask = Mathf.Min(sampleBlendMask((float)x / (float)w, (float)y / (float)h), borderMask);
    //   float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);

    //   // Grass
    //   map[x, y, 0] = curBiomeLayers[0] * mask;
    //   map[x, y, 1] = curBiomeLayers[1] * mask;

    //   // Other
    //   float[] blends = topLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

    //   map[x, y, 2] = blends[0] * (1 - mask);
    //   map[x, y, 3] = blends[1] * (1 - mask);

    // } else if (topRight != null && y < halfHeight && !PointInsideSphere(xyPos, center, halfWidth)) {

    //   float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
    //   float xMask = Mathf.InverseLerp(w - 30, w, x);
    //   float yMask = Mathf.InverseLerp(30, 90, y);
    //   float borderMask = Mathf.Min(xMask, yMask);
    //   mask = Mathf.Min(mask, borderMask);


    //   float distanceToRadius = distanceToCircle(xyPos, center, halfWidth);
    //   float t = Mathf.Max(Mathf.InverseLerp(20, 0, distanceToRadius), mask);

    //   // Grass
    //   map[x, y, 0] = curBiomeLayers[0] * t;
    //   map[x, y, 1] = curBiomeLayers[1] * t;

    //   float[] blends = topRight.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
    //   map[x, y, 2] = blends[0] * (1 - t);
    //   map[x, y, 3] = blends[1] * (1 - t);
    // } else if (btmLeft != null && x < halfWidth && !PointInsideSphere(xyPos, center, halfWidth)) {
    //   float distanceToRadius = distanceToCircle(xyPos, center, halfWidth);
    //   float t = Mathf.InverseLerp(20, 0, distanceToRadius);

    //   // Grass
    //   map[x, y, 0] = curBiomeLayers[0] * t;
    //   map[x, y, 1] = curBiomeLayers[1] * t;

    //   float[] blends = btmLeft.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
    //   map[x, y, 2] = blends[0] * (1 - t);
    //   map[x, y, 3] = blends[1] * (1 - t);

    // } else if (btmRight != null && !PointInsideSphere(xyPos, center, halfWidth)) {
    //   float distanceToRadius = distanceToCircle(xyPos, center, halfWidth);
    //   float t = Mathf.InverseLerp(20, 0, distanceToRadius);

    //   // Grass
    //   map[x, y, 0] = curBiomeLayers[0] * t;
    //   map[x, y, 1] = curBiomeLayers[1] * t;

    //   float[] blends = btmRight.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
    //   map[x, y, 2] = blends[0] * (1 - t);
    //   map[x, y, 3] = blends[1] * (1 - t);

    // }
    //       else {
    //   map[x, y, 0] = curBiomeLayers[0];
    //   map[x, y, 1] = curBiomeLayers[1];
    // }
  }

  private void blendTextures(Terrain terrain, List<Biome> otherBiomes, int[] layerIndices, Dictionary<int[], Chunk> chunksDict) {
    int w = terrain.terrainData.alphamapWidth;
    int h = terrain.terrainData.alphamapHeight;
    int halfWidth = w / 2;
    int halfHeight = h / 2;

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
    chunksDict.TryGetValue(new int[] { -position.x, -position.y - 1 }, out top);
    chunksDict.TryGetValue(new int[] { -position.x + 1, -position.y }, out right);
    chunksDict.TryGetValue(new int[] { -position.x, -position.y + 1 }, out bottom);
    chunksDict.TryGetValue(new int[] { -position.x - 1, -position.y }, out left);
    chunksDict.TryGetValue(new int[] { -position.x - 1, -position.y - 1 }, out topLeft);
    chunksDict.TryGetValue(new int[] { -position.x + 1, -position.y - 1 }, out topRight);
    chunksDict.TryGetValue(new int[] { -position.x - 1, -position.y + 1 }, out btmLeft);
    chunksDict.TryGetValue(new int[] { -position.x + 1, -position.y + 1 }, out btmRight);

    int numLayers = getNumLayers();
    int blendDistance = 400;
    int topLayerIndex = top != null ? otherBiomes.FindIndex(biome => biome.type == top.biome.type) : -1;
    int topLayerTextureIndex = numLayers + topLayerIndex;
    int rightLayerIndex = right != null ? otherBiomes.FindIndex(biome => biome.type == right.biome.type) : -1;
    int rightLayerTextureIndex = numLayers + rightLayerIndex;
    int btmLayerIndex = bottom != null ? otherBiomes.FindIndex(biome => biome.type == bottom.biome.type) : -1;
    int btmLayerTextureIndex = numLayers + btmLayerIndex;
    int leftLayerIndex = left != null ? otherBiomes.FindIndex(biome => biome.type == left.biome.type) : -1;
    int leftLayerTextureIndex = numLayers + leftLayerIndex;
    int topleftLayerIndex = topLeft != null ? otherBiomes.FindIndex(biome => biome.type == topLeft.biome.type) : -1;
    int topleftLayerTextureIndex = numLayers + topleftLayerIndex;
    int toprightLayerIndex = topLeft != null ? otherBiomes.FindIndex(biome => biome.type == topRight.biome.type) : -1;
    int toprightLayerTextureIndex = numLayers + toprightLayerIndex;
    int btmleftLayerIndex = btmLeft != null ? otherBiomes.FindIndex(biome => biome.type == btmLeft.biome.type) : -1;
    int btmleftLayerTextureIndex = numLayers + btmleftLayerIndex;
    int btmrightLayerIndex = btmRight != null ? otherBiomes.FindIndex(biome => biome.type == btmRight.biome.type) : -1;
    int btmrightLayerTextureIndex = numLayers + btmrightLayerIndex;

    bool insideSquare = true;
    // For each point on the alphamap...
    for (int y = 0; y < h; y++) {
      for (int x = 0; x < w; x++) {

        Vector2 xyPos = new Vector2(x, y);
        insideSquare = true;

        float[] curBiomeLayers = blendLayer(x, y, terrain.terrainData, processedHeightmap.values);

        // Top
        if (top != null && y < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(0, blendDistance, y);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * t;

          float[] blends = top.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[topLayerIndex]; i++)
            map[x, y, topLayerTextureIndex + i] = blends[i] * inverseT;

          insideSquare = false;

        }
        // Bottom
        else if (bottom != null && y > h - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(h - blendDistance, h, y);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * inverseT;

          float[] blends = bottom.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmLayerIndex]; i++)
            map[x, y, btmLayerTextureIndex + i] = blends[i] * t;

          insideSquare = false;
        }

        // Left
        if (left != null && x < blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(0, blendDistance, x);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * t;

          float[] blends = left.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmLayerIndex]; i++)
            map[x, y, leftLayerTextureIndex + i] = blends[i] * inverseT;

          insideSquare = false;
        }
        // Right
        else if (right != null && x > w - blendDistance) {
          float mask = sampleBlendMask((float)x / (float)w, (float)y / (float)h);
          float t = Mathf.InverseLerp(w - blendDistance, w, x);
          t = Mathf.Lerp(t, Mathf.Max(t, mask), t);
          float inverseT = 1.0f - t;

          for (var i = 0; i < numLayers; i++)
            map[x, y, i] = curBiomeLayers[i] * inverseT;

          float[] blends = right.biome.blendLayer(x, y, terrain.terrainData, processedHeightmap.values);
          for (var i = 0; i < layerIndices[btmLayerIndex]; i++)
            map[x, y, rightLayerTextureIndex + i] = blends[i] * t;

          insideSquare = false;
        }

        // Top left corner
        if (topLeft != null && y < blendDistance && x < blendDistance) {
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
        else if (topRight != null && y < blendDistance && x > w - blendDistance) {
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
        else if (btmLeft != null && y > h - blendDistance && x < blendDistance) {
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
        else if (btmRight != null && y > h - blendDistance && x > w - blendDistance) {
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
