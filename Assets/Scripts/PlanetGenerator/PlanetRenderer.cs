using UnityEngine;
using System;
using System.Collections.Generic;

public class Chunk {
  public GameObject terrainGO;
  public Terrain terrain;
  public Biome biome;

  public Chunk(GameObject terrainGO, Terrain terrain, Biome biome) {
    this.terrainGO = terrainGO;
    this.biome = biome;
    this.terrain = terrain;
  }
}


public class PlanetRenderer : MonoBehaviour {

  private Terrain terrain;
  private TerrainCollider terrainCollider;
  private WorldGenerator worldGenerator;
  private Vector3 terrainSize;
  private int heightmapSize = 512;
  private float terrainLength = 200;
  private float terrainHeight = 200;
  private Dictionary<Vector2, Chunk> terrainChunkDictionary = new Dictionary<Vector2, Chunk>();
  private List<Chunk> visibleTerrainChunks = new List<Chunk>();
  public WorldSettings worldSettings;
  public Transform viewer;

  // Start is called before the first frame update
  void Start() {
    worldGenerator = new WorldGenerator(worldSettings);
    terrainSize = new Vector3(terrainLength, terrainHeight, terrainLength);
    updateChunks();
  }

  // Update is called once per frame
  void Update() {
    updateChunks();
  }

  private Biome getBiome(Vector2 position) {
    Biome toReturn = null;
    if (position.x == 0 && position.y == 0)
      toReturn = new Grassland();
    else
      toReturn = new Mountains();

    toReturn.generate(heightmapSize + 1, new Vector2(position.y, -position.x) * new Vector2(heightmapSize, heightmapSize));
    return toReturn;
  }

  private void updateChunks() {
    float normalizedXPos = (float)Math.Floor(viewer.position.x / terrainLength);
    float normalizedZPos = (float)Math.Floor(viewer.position.z / terrainLength);
    Vector2 cache = new Vector2();
    Chunk[,] chunkMap = new Chunk[3, 3];

    foreach (Chunk chunk in visibleTerrainChunks)
      chunk.terrainGO.SetActive(false);

    visibleTerrainChunks.Clear();
    bool geometryChanged = false;

    for (float x = -1; x < 2; x++) {
      for (float z = -1; z < 2; z++) {

        if ((x == 0 && z == 0) || (x == -1 && z == 0)) {
          cache.Set(normalizedXPos + x, normalizedZPos + z);
          Chunk chunk;

          if (!terrainChunkDictionary.ContainsKey(cache)) {
            // ThreadedDataRequester.requestData(() => generateTerrain(cache), onTerrainLoaded);
            Biome biome = getBiome(cache);
            GameObject newTerrain = generateTerrain(cache, biome);
            chunk = new Chunk(newTerrain, newTerrain.GetComponent<Terrain>(), biome);
            geometryChanged = true;

            terrainChunkDictionary.Add(cache, chunk);
            visibleTerrainChunks.Add(chunk);
          } else {
            chunk = terrainChunkDictionary[cache];
            visibleTerrainChunks.Add(chunk);
          }

          chunkMap[(int)x + 1, (int)z + 1] = chunk;
        }
      }
    }

    for (int i = 0, l = visibleTerrainChunks.Count; i < l; i++) {
      visibleTerrainChunks[i].terrainGO.SetActive(true);

      if (geometryChanged) {
        visibleTerrainChunks[i].biome.blendEdges(chunkMap);
        visibleTerrainChunks[i].terrain.terrainData.SetHeights(0, 0, visibleTerrainChunks[i].biome.processedHeightmap.values);
      }
    }
  }

  // void onTerrainLoaded(object terrainGO) {
  // GameObject newTerrain = terrainGO as GameObject;
  // terrainChunkDictionary.Add(cache, newTerrain);
  // visibleTerrainChunks.Add(newTerrain);
  // }

  void Awake() {
    InitViewpoint();
  }

  GameObject generateTerrain(Vector2 position, Biome biome) {
    string uniqueName = "Terrain_" + position.ToString();

    if (null != GameObject.Find(uniqueName)) {
      Debug.LogWarning("Already have a neighbor on that side");
      return null;
    }

    GameObject terrgainGO = new GameObject(uniqueName);
    terrgainGO.isStatic = true;
    terrgainGO.transform.parent = gameObject.transform;
    terrgainGO.transform.position = new Vector3(position.x, 0, position.y) * terrainLength;

    int layerIdx = LayerMask.NameToLayer("Terrain");
    if (layerIdx == -1)
      Debug.LogError("OceanDepthCache: Invalid layer specified: \"Terrain\". Please specify valid layers for objects/geometry that provide the ocean depth.");

    terrgainGO.layer = layerIdx;

    terrain = terrgainGO.AddComponent<Terrain>();
    terrainCollider = terrgainGO.AddComponent<TerrainCollider>();

    // Create base terrain
    terrain.terrainData = new TerrainData();
    terrain.terrainData.name = Guid.NewGuid().ToString();
    terrain.terrainData.baseMapResolution = 1024;
    terrain.terrainData.heightmapResolution = heightmapSize + 1;
    terrain.terrainData.alphamapResolution = heightmapSize;
    terrain.terrainData.SetDetailResolution(512, 8);
    terrain.drawInstanced = false;
    terrain.allowAutoConnect = true;
    terrain.drawTreesAndFoliage = true;
    terrain.bakeLightProbesForTrees = true;
    terrain.deringLightProbesForTrees = true;
    terrain.preserveTreePrototypeLayers = false;
    terrain.detailObjectDistance = 80;
    terrain.detailObjectDensity = 1;
    terrain.treeDistance = 5000;
    terrain.treeBillboardDistance = 50;
    terrain.treeCrossFadeLength = 5;
    terrain.treeMaximumFullLODCount = 50;
    terrain.terrainData.size = terrainSize;

    // Create base later
    terrain.terrainData.terrainLayers = biome.layers;
    terrain.terrainData.SetHeights(0, 0, biome.processedHeightmap.values);

    // Create terrain collider
    terrainCollider.terrainData = terrain.terrainData;
    return terrgainGO;
  }

  void InitViewpoint() {
    if (viewer == null) {
      var camMain = Camera.main;
      if (camMain != null) {
        viewer = camMain.transform;
      } else {
        Debug.LogError("Please provide the viewpoint transform, or tag the primary camera as MainCamera.", this);
      }
    }
  }
}
