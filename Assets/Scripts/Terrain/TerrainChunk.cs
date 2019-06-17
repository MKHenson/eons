using UnityEngine;

public class TerrainChunk {
  const float colliderGenerationDistanceThreshold = 5f;
  public event System.Action<TerrainChunk, bool> onVisibilityChanged;
  public Vector2 coord;

  GameObject meshObject;
  Vector2 sampleCenter;
  Bounds bounds;

  MeshRenderer meshRenderer;
  MeshFilter meshFilter;
  MeshCollider meshCollider;

  LODInfo[] detailLevels;
  LODMesh[] lodMeshes;
  int colliderLODIndex;

  HeightMap heightmap;
  bool heightMapReceived;
  int previousLODIndex = -1;
  bool hasSetCollider;
  float maxViewDst;

  WorldGenerator worldGenerator;
  HeightMapSettings heightMapSettings;
  // MeshSettings meshSettings;
  Transform viewer;
  BiomeData biomeData;

  public TerrainChunk(Vector2 coord, WorldGenerator worldGenerator, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material) {
    this.detailLevels = detailLevels;
    this.worldGenerator = worldGenerator;
    this.colliderLODIndex = colliderLODIndex;
    this.coord = coord;
    this.heightMapSettings = heightMapSettings;
    // this.meshSettings = meshSettings;
    this.viewer = viewer;

    sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
    Vector2 position = coord * meshSettings.meshWorldSize;
    bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

    meshObject = new GameObject("Terrain Chunk");
    meshRenderer = meshObject.AddComponent<MeshRenderer>();
    meshFilter = meshObject.AddComponent<MeshFilter>();
    meshCollider = meshObject.AddComponent<MeshCollider>();

    meshObject.transform.position = new Vector3(position.x, 0, position.y);
    meshObject.transform.parent = parent;


    int layerIdx = LayerMask.NameToLayer("Terrain");
    if (layerIdx == -1)
      Debug.LogError("OceanDepthCache: Invalid layer specified: \"Terrain\". Please specify valid layers for objects/geometry that provide the ocean depth.");

    meshObject.layer = layerIdx;

    setVisible(false);

    lodMeshes = new LODMesh[detailLevels.Length];
    for (int i = 0; i < detailLevels.Length; i++) {
      lodMeshes[i] = new LODMesh(detailLevels[i].lod);
      lodMeshes[i].updateCallback += updateTerrainChunk;

      if (i == colliderLODIndex) {
        lodMeshes[i].updateCallback += updateCollisionMesh;
      }
    }

    maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
  }

  public bool hasMesh {
    get {
      for (int i = 0; i < lodMeshes.Length; i++)
        if (lodMeshes[i].hasMesh)
          return true;

      return false;
    }
  }

  public void load() {
    ThreadedDataRequester.requestData(() => worldGenerator.queryBiom((int)coord.x, (int)coord.y, 16), onBiomDataReceived);
  }

  void onBiomDataReceived(object biomeDataObject) {
    biomeData = biomeDataObject as BiomeData;
    meshRenderer.material = worldGenerator.getMaterialForBiome(biomeData);

    // onHeightMapReceived(worldGenerator.generateBiomeHeightmap(biomeData, sampleCenter));

    ThreadedDataRequester.requestData(() => worldGenerator.generateBiomeHeightmap(biomeData, sampleCenter), onHeightMapReceived);
  }

  void onHeightMapReceived(object heightMapObject) {
    this.heightmap = (HeightMap)heightMapObject;
    heightMapReceived = true;

    worldGenerator.generateMaterialUniforms(meshRenderer.material, biomeData, heightMapSettings);

    updateTerrainChunk();
  }

  Vector2 viewerPosition {
    get {
      return new Vector2(viewer.position.x, viewer.position.z);
    }
  }

  public void updateTerrainChunk() {
    if (heightMapReceived) {
      float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

      bool wasVisible = isVisible();
      bool visible = viewerDstFromNearestEdge <= maxViewDst;

      if (visible) {
        int lodIndex = 0;

        for (int i = 0; i < detailLevels.Length - 1; i++) {
          if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
            lodIndex = i + 1;
          } else {
            break;
          }
        }

        if (lodIndex != previousLODIndex) {
          LODMesh lodMesh = lodMeshes[lodIndex];
          if (lodMesh.hasMesh) {
            previousLODIndex = lodIndex;
            meshFilter.mesh = lodMesh.mesh;
          } else if (!lodMesh.hasRequestedMesh) {
            lodMesh.requestMesh(heightmap, worldGenerator.meshSettings, heightMapSettings);
          }
        }
      }

      if (wasVisible != visible) {

        setVisible(visible);

        if (onVisibilityChanged != null)
          onVisibilityChanged(this, visible);
      }
    }
  }

  public void updateCollisionMesh() {
    if (!hasSetCollider) {
      float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

      if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold) {
        if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
          lodMeshes[colliderLODIndex].requestMesh(heightmap, worldGenerator.meshSettings, heightMapSettings);
      }

      if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
        if (lodMeshes[colliderLODIndex].hasMesh) {
          meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
          hasSetCollider = true;
        }
      }
    }
  }

  public void setVisible(bool visible) {
    meshObject.SetActive(visible);
  }

  public bool isVisible() {
    return meshObject.activeSelf;
  }
}