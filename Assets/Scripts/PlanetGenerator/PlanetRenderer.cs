using UnityEngine;
using System;
using System.Collections.Generic;

public class PlanetRenderer : MonoBehaviour {
  private Terrain terrain;
  private TerrainCollider terrainCollider;
  private WorldGenerator worldGenerator;
  private Vector3 terrainSize;
  private int heightmapSize = 512;
  private float terrainLength = 200;
  private float terrainHeight = 200;
  private Dictionary<Vector2, GameObject> terrainChunkDictionary = new Dictionary<Vector2, GameObject>();
  private List<GameObject> visibleTerrainChunks = new List<GameObject>();

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
      toReturn = new Grassland();

    toReturn.generate(heightmapSize + 1, new Vector2(position.y, -position.x) * new Vector2(heightmapSize, heightmapSize));
    return toReturn;
  }

  private void updateChunks() {
    float normalizedXPos = (float)Math.Floor(viewer.position.x / terrainLength);
    float normalizedZPos = (float)Math.Floor(viewer.position.z / terrainLength);
    Vector2 cache = new Vector2();

    foreach (GameObject chunk in visibleTerrainChunks)
      chunk.SetActive(false);

    visibleTerrainChunks.Clear();

    for (float x = -1; x < 2; x++) {
      for (float z = -1; z < 2; z++) {
        cache.Set(normalizedXPos + x, normalizedZPos + z);

        if (!terrainChunkDictionary.ContainsKey(cache)) {
          // ThreadedDataRequester.requestData(() => generateTerrain(cache), onTerrainLoaded);
          Biome biome = getBiome(cache);
          GameObject newTerrain = generateTerrain(cache, biome);
          terrainChunkDictionary.Add(cache, newTerrain);
          visibleTerrainChunks.Add(newTerrain);
        } else {
          GameObject terrain = terrainChunkDictionary[cache];
          visibleTerrainChunks.Add(terrain);
        }

      }
    }

    foreach (GameObject chunk in visibleTerrainChunks)
      chunk.SetActive(true);
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
    terrain.terrainData.SetHeights(0, 0, biome.heightmap.values);

    // Create terrain collider
    terrainCollider.terrainData = terrain.terrainData;

    // HeightMapSettings settings = ScriptableObject.CreateInstance("HeightMapSettings") as HeightMapSettings;
    // settings.useFalloff = true;
    // settings.noiseSettings = new NoiseSettings();
    // settings.noiseSettings.scale = 400;
    // settings.noiseSettings.octaves = 4;
    // settings.noiseSettings.lacunarity = 2.2f;
    // settings.noiseSettings.persistance = 0.6f;

    // settings.heightCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 0), new Keyframe(1, 1) });

    // HeightMap heightmap = HeightMapGenerator.generateHeightmap(terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight, settings, new Vector2(position.y, -position.x) * new Vector2(terrain.terrainData.heightmapWidth - 1, terrain.terrainData.heightmapHeight - 1));
    // terrain.terrainData.SetHeights(0, 0, heightmap.values);

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
