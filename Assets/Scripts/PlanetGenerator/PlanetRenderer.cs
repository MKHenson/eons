using UnityEngine;
using System;

public class PlanetRenderer : MonoBehaviour {
  private Terrain terrain;
  private TerrainCollider terrainCollider;
  private WorldGenerator worldGenerator;
  private Vector3 terrainSize;

  public WorldSettings worldSettings;
  public Transform viewer;

  // Start is called before the first frame update
  void Start() {
    worldGenerator = new WorldGenerator(worldSettings);
    terrainSize = new Vector3(1000, 600, 1000);
    generateTerrain(Vector2.zero);
    generateTerrain(Vector2.zero + new Vector2(1, 0));
    generateTerrain(Vector2.zero + new Vector2(-1, 0));
    generateTerrain(Vector2.zero + new Vector2(0, 1));
    generateTerrain(Vector2.zero + new Vector2(0, -1));
  }

  // Update is called once per frame
  void Update() {

  }

  void Awake() {
    InitViewpoint();
  }

  void generateTerrain(Vector2 position) {
    string uniqueName = "Terrain_" + position.ToString();

    if (null != GameObject.Find(uniqueName)) {
      Debug.LogWarning("Already have a neighbor on that side");
      return;
    }




    GameObject terrgainGO = new GameObject(uniqueName);
    terrgainGO.isStatic = true;
    terrgainGO.transform.parent = gameObject.transform;
    terrgainGO.transform.position = new Vector3(-position.y, 0, position.x) * terrainSize.x;

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
    terrain.terrainData.heightmapResolution = 513;
    terrain.terrainData.alphamapResolution = 512;
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
    Grassland grassland = new Grassland();
    terrain.terrainData.terrainLayers = grassland.generateLayers();

    // Create terrain collider
    terrainCollider.terrainData = terrain.terrainData;

    HeightMapSettings settings = ScriptableObject.CreateInstance("HeightMapSettings") as HeightMapSettings;
    settings.useFalloff = true;
    settings.noiseSettings = new NoiseSettings();
    settings.noiseSettings.scale = 400;
    settings.noiseSettings.octaves = 4;
    settings.noiseSettings.lacunarity = 2.2f;
    settings.noiseSettings.persistance = 0.6f;

    settings.heightCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 0), new Keyframe(1, 1) });

    HeightMap heightmap = HeightMapGenerator.generateHeightmap(terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight, settings, position * new Vector2(terrain.terrainData.heightmapWidth - 1, terrain.terrainData.heightmapHeight - 1));
    terrain.terrainData.SetHeights(0, 0, heightmap.values);
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
