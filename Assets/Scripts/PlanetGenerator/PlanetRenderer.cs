using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class Chunk {
  public delegate void LoadedHandler(Chunk chunk);
  public event LoadedHandler Loaded;

  public GameObject terrainGO;
  public Terrain terrain;
  public Biome biome;
  public bool geometryChanged;
  public bool requiresStitch;

  public Chunk(GameObject terrainGO, Biome biome) {
    this.terrainGO = terrainGO;
    this.biome = biome;
    geometryChanged = false;
    requiresStitch = false;
    this.terrain = terrainGO.GetComponent<Terrain>();
  }

  public void load() {
    int heightmapSize = terrain.terrainData.heightmapWidth;
    Vector2 offset = new Vector2(biome.position.y, -biome.position.x) * new Vector2(heightmapSize - 1, heightmapSize - 1);

    ThreadedDataRequester.requestData(() => {
      return biome.generate(heightmapSize, offset);
    }, onBiomeLoaded);

    // onBiomeLoaded(biome.generate(heightmapSize, offset));
  }

  private void onBiomeLoaded(object biomeData) {
    Biome biome = biomeData as Biome;

    // Create base later
    TerrainLayer[] layers = biome.generateLayers();
    terrain.terrainData.terrainLayers = layers;

    // Create the heights
    terrain.terrainData.SetHeights(0, 0, biome.processedHeightmap.values);
    terrain.Flush();
    requiresStitch = true;

    if (Loaded != null)
      Loaded(this);
  }
}


public class PlanetRenderer : MonoBehaviour {

  private Terrain terrain;
  private TerrainCollider terrainCollider;
  private WorldGenerator worldGenerator;
  private Vector3 terrainSize;
  private int heightmapSize = 512;
  private float terrainLength = 500;
  private float terrainHeight = 80;
  private Dictionary<Vector2, Chunk> terrainChunkDictionary = new Dictionary<Vector2, Chunk>();
  private List<Chunk> visibleTerrainChunks = new List<Chunk>();
  private List<Chunk> chunksToStitch = new List<Chunk>();
  public WorldSettings worldSettings;
  public Transform viewer;
  private int numBiomesLoading;

  // Start is called before the first frame update
  void Start() {
    numBiomesLoading = 0;
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
    if ((position.x + 1) % 2 == 0 && (position.y + 1) % 2 == 0)
      toReturn = new Grassland();
    else
      toReturn = new Mountains();

    toReturn.position = position;
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
    chunksToStitch.Clear();

    for (float x = -1; x < 2; x++) {
      for (float z = -1; z < 2; z++) {
        cache.Set(normalizedXPos + x, normalizedZPos + z);
        Chunk chunk;

        if (!terrainChunkDictionary.ContainsKey(cache)) {
          Biome biome = getBiome(cache);
          GameObject newTerrain = generateTerrain(cache, biome);
          chunk = new Chunk(newTerrain, biome);
          numBiomesLoading += 1;
          chunk.Loaded += onChunkLoaded;
          chunk.load();
          terrainChunkDictionary.Add(cache, chunk);
          visibleTerrainChunks.Add(chunk);
        } else {
          chunk = terrainChunkDictionary[cache];
          visibleTerrainChunks.Add(chunk);
        }

        chunkMap[(int)x + 1, (int)z + 1] = chunk;
      }
    }

    for (int i = 0, l = visibleTerrainChunks.Count; i < l; i++) {
      if (visibleTerrainChunks[i].requiresStitch)
        visibleTerrainChunks[i].terrainGO.SetActive(false);
      else
        visibleTerrainChunks[i].terrainGO.SetActive(true);
    }

    for (int x = 0; x < 3; x++)
      for (int z = 0; z < 3; z++)
        if (chunkMap[x, z].requiresStitch) {



          if (!chunksToStitch.Contains(chunkMap[x, z]))
            chunksToStitch.Add(chunkMap[x, z]);

          for (int innerX = -1; innerX < 2; innerX++)
            for (int innerZ = -1; innerZ < 2; innerZ++) {
              int normalizedX = innerX + x;
              int normalizedZ = innerZ + z;

              if (normalizedX >= 0 && normalizedZ >= 0 && normalizedX < 3 && normalizedZ < 3) {
                if (chunkMap[normalizedX, normalizedZ].biome.GetType() != chunkMap[x, z].biome.GetType())
                  if (!chunksToStitch.Contains(chunkMap[normalizedX, normalizedZ]))
                    chunksToStitch.Add(chunkMap[normalizedX, normalizedZ]);
              }
            }

        }

    if (chunksToStitch.Count > 0 && numBiomesLoading == 0) {

      int count = chunksToStitch.Count;

      //   ThreadedDataRequester.requestData(() => {
      //     Debug.Log("We are chunking");
      //     Chunk[] stitchedChunks = chunksToStitch.ToArray();
      //     Stitcher.StitchTerrain(chunksToStitch.Select(chunk => chunk.terrain).ToArray(), 20);
      //     return stitchedChunks;
      //   }, onStitchComplete);


      foreach (Chunk chunk in chunksToStitch)
        chunk.requiresStitch = false;


      Terrain[] terrains = chunksToStitch.Select(chunk => chunk.terrain).ToArray();
      Stitcher.StitchTerrain(terrains, 20);
    }
  }

  private void stitchTerrains(Terrain[] _terrains) {
    Vector2 firstPosition;
    Dictionary<int[], float[,]> terrainDict = new Dictionary<int[], float[,]>(new IntArrayComparer());

    firstPosition = new Vector2(_terrains[0].transform.position.x, _terrains[0].transform.position.z);

    int sizeX = (int)_terrains[0].terrainData.size.x;
    int sizeZ = (int)_terrains[0].terrainData.size.z;

    foreach (var terrain in _terrains) {
      int[] posTer = new int[] {
          (int)(Mathf.RoundToInt ((terrain.transform.position.x - firstPosition.x) / sizeX)),
          (int)(Mathf.RoundToInt ((terrain.transform.position.z - firstPosition.y) / sizeZ))
        };

      terrainDict.Add(posTer, terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight));
    }

    Stitcher.StitchTerrain(terrainDict, 20);
    foreach (var terrain in _terrains) {
      terrain.Flush();

    }
  }

  // void onStitchComplete(object chunks) {
  //   Debug.Log("We are done chunnking");
  //   Chunk[] stitchedChunks = chunks as Chunk[];
  //   foreach (Chunk chunk in stitchedChunks)
  //     chunk.requiresStitch = false;
  // }

  void Awake() {
    InitViewpoint();
  }

  void onChunkLoaded(Chunk chunk) {
    numBiomesLoading -= 1;
    chunk.Loaded -= onChunkLoaded;
  }

  GameObject generateTerrain(Vector2 position, Biome biome) {
    string uniqueName = "Terrain_" + position.ToString();

    if (null != GameObject.Find(uniqueName)) {
      Debug.LogWarning("Already have a neighbor on that side");
      return null;
    }


    TerrainData terrainData = new TerrainData();
    terrainData.name = Guid.NewGuid().ToString();
    terrainData.baseMapResolution = 1024;
    terrainData.heightmapResolution = heightmapSize + 1;
    terrainData.alphamapResolution = heightmapSize;
    terrainData.SetDetailResolution(256, 64);
    terrainData.size = terrainSize;

    GameObject terrgainGO = Terrain.CreateTerrainGameObject(terrainData);
    terrgainGO.name = uniqueName;
    terrgainGO.isStatic = true;
    terrgainGO.transform.parent = gameObject.transform;
    terrgainGO.transform.position = new Vector3(position.x, 0, position.y) * terrainLength;

    int layerIdx = LayerMask.NameToLayer("Terrain");
    if (layerIdx == -1)
      Debug.LogError("OceanDepthCache: Invalid layer specified: \"Terrain\". Please specify valid layers for objects/geometry that provide the ocean depth.");

    terrgainGO.layer = layerIdx;

    terrain = terrgainGO.GetComponent<Terrain>();
    terrainCollider = terrgainGO.GetComponent<TerrainCollider>();

    terrain.drawInstanced = false;
    terrain.allowAutoConnect = true;
    terrain.drawTreesAndFoliage = true;
    // terrain.bakeLightProbesForTrees = true;
    // terrain.deringLightProbesForTrees = true;
    terrain.preserveTreePrototypeLayers = false;
    terrain.detailObjectDistance = 80;
    terrain.detailObjectDensity = 1;
    terrain.treeDistance = 5000;
    terrain.treeBillboardDistance = 50;
    terrain.treeCrossFadeLength = 5;
    terrain.treeMaximumFullLODCount = 50;

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
