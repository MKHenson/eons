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
  public bool loadingBiome;
  public bool requiresStitch;

  public Chunk(GameObject terrainGO, Biome biome) {
    this.terrainGO = terrainGO;
    this.biome = biome;
    loadingBiome = false;
    requiresStitch = false;
    loadingBiome = true;
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

    // // Create base later
    // TerrainLayer[] layers = biome.generateLayers();
    // terrain.terrainData.terrainLayers = layers;

    // // Create the heights
    // terrain.terrainData.SetHeights(0, 0, biome.processedHeightmap.values);
    // terrain.Flush();
    requiresStitch = true;
    loadingBiome = false;
    if (Loaded != null)
      Loaded(this);
  }
}

public class TerrainStitchToken {
  public string name;
  public float[,] data;
  public Terrain terrain;

  public TerrainStitchToken(string name, float[,] data, Terrain terrain) {
    this.name = name;
    this.data = data;
    this.terrain = terrain;
  }
}

public class PlanetRenderer : MonoBehaviour {
  public delegate void FirstChunksLoadedHundler();
  public event FirstChunksLoadedHundler OnFirstChunksLoaded;

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
  private Vector2 prevNormalizedPos;
  private bool forUpdate;

  // Start is called before the first frame update
  void Start() {
    numBiomesLoading = 0;
    forUpdate = true;
    prevNormalizedPos = new Vector2();
    worldGenerator = new WorldGenerator(worldSettings);
    terrainSize = new Vector3(terrainLength, terrainHeight, terrainLength);
    updateChunks();
  }

  // Update is called once per frame
  void Update() {
    updateChunks();
  }

  private Biome getBiome(Vector2Int position) {
    Biome toReturn = null;
    if (position.x == -2 && position.y == -1)
      toReturn = new Dessert();
    else if ((position.x + 1) % 2 == 0 && (position.y + 1) % 2 == 0)
      toReturn = new Grassland();
    else
      toReturn = new Mountains();

    toReturn.position = position;
    return toReturn;
  }

  private void updateChunks() {

    int normalizedXPos = (int)Math.Floor(viewer.position.x / terrainLength);
    int normalizedZPos = (int)Math.Floor(viewer.position.z / terrainLength);

    if (!forUpdate && prevNormalizedPos.x == normalizedXPos && prevNormalizedPos.y == normalizedZPos)
      return;

    Vector2Int cache = new Vector2Int();
    Chunk[,] chunkMap = new Chunk[3, 3];

    foreach (Chunk chunk in visibleTerrainChunks)
      chunk.terrainGO.SetActive(false);

    visibleTerrainChunks.Clear();
    chunksToStitch.Clear();

    for (int x = -1; x < 2; x++) {
      for (int z = -1; z < 2; z++) {
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
      if (visibleTerrainChunks[i].requiresStitch || visibleTerrainChunks[i].loadingBiome)
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
                if (chunkMap[normalizedX, normalizedZ].biome.type != chunkMap[x, z].biome.type)
                  if (!chunksToStitch.Contains(chunkMap[normalizedX, normalizedZ]))
                    chunksToStitch.Add(chunkMap[normalizedX, normalizedZ]);
              }
            }

        }

    if (chunksToStitch.Count > 0 && numBiomesLoading == 0) {

      int count = chunksToStitch.Count;

      foreach (Chunk chunk in chunksToStitch)
        chunk.requiresStitch = false;

      // Chunk[] terrains = chunksToStitch.Select(chunk => chunk).ToArray();

      // stitchTerrains(terrains);
      stitchTerrains(visibleTerrainChunks.ToArray());
    }

    prevNormalizedPos.Set(normalizedXPos, normalizedZPos);
    forUpdate = false;
  }

  private void stitchTerrains(Chunk[] _terrains) {
    Vector2 firstPosition;
    Dictionary<int[], Chunk> terrainDataDict = new Dictionary<int[], Chunk>(new IntArrayComparer());

    firstPosition = new Vector2(_terrains[0].terrain.transform.position.x, _terrains[0].terrain.transform.position.z);

    int sizeX = (int)_terrains[0].terrain.terrainData.size.x;
    int sizeZ = (int)_terrains[0].terrain.terrainData.size.z;

    foreach (var terrain in _terrains) {
      int[] posTer = new int[] {
          (int)(Mathf.RoundToInt ((terrain.terrain.transform.position.x - firstPosition.x) / sizeX)),
          (int)(Mathf.RoundToInt ((terrain.terrain.transform.position.z - firstPosition.y) / sizeZ))
        };

      terrainDataDict.Add(posTer, terrain);
    }

    // Set the terrain neighbours
    foreach (var item in terrainDataDict) {
      int[] posTer = item.Key;
      Chunk top = null;
      Chunk left = null;
      Chunk right = null;
      Chunk bottom = null;
      terrainDataDict.TryGetValue(new int[] {
            posTer [0],
            posTer [1] + 1
          }, out top);
      terrainDataDict.TryGetValue(new int[] {
            posTer [0] - 1,
            posTer [1]
          }, out left);
      terrainDataDict.TryGetValue(new int[] {
            posTer [0] + 1,
            posTer [1]
          }, out right);
      terrainDataDict.TryGetValue(new int[] {
            posTer [0],
            posTer [1] - 1
          }, out bottom);

      item.Value.terrain.SetNeighbors(left != null ? left.terrain : null, top != null ? top.terrain : null, right != null ? right.terrain : null, bottom != null ? bottom.terrain : null);
    }

    // Perform the stitches
    ThreadedDataRequester.requestData(() => {
      return Stitcher.StitchTerrain(terrainDataDict, 20);
    }, onStitchComplete);
  }

  void onStitchComplete(object chunks) {
    Dictionary<int[], Chunk> chunksDict = chunks as Dictionary<int[], Chunk>;
    foreach (var chunk in chunksDict) {

      chunk.Value.terrain.terrainData.SetHeights(0, 0, chunk.Value.biome.processedHeightmap.values);

      // Blend the two terrain textures according to the steepness of
      // the slope at each point.
      chunk.Value.biome.generateDetails(chunk.Value.terrain, chunksDict);

      chunk.Value.terrain.Flush();
    }

    forUpdate = true;
    if (terrainChunkDictionary.Count == 9 && OnFirstChunksLoaded != null)
      OnFirstChunksLoaded();
  }

  void Awake() {
    InitViewpoint();
  }

  void onChunkLoaded(Chunk chunk) {
    numBiomesLoading -= 1;
    chunk.Loaded -= onChunkLoaded;
    forUpdate = true;
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
    terrainData.SetDetailResolution(512, 16);
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
    terrain.preserveTreePrototypeLayers = false;
    terrain.detailObjectDistance = 80;
    terrain.detailObjectDensity = 1;
    terrain.treeDistance = 5000;
    terrain.treeBillboardDistance = 50;
    terrain.treeCrossFadeLength = 5;
    terrain.treeMaximumFullLODCount = 50;
    terrain.drawInstanced = false;
    terrain.materialTemplate = Resources.Load<Material>("Terrain/TerrainMaterial");
    terrain.terrainData.alphamapResolution = 1024;

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
