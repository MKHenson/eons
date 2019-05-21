using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

  const float viewerMoveThresholdForChunkUpdate = 25f;
  const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
  const float colliderGenerationDistanceThreshold = 5f;

  public LODInfo[] detailLevels;
  public static float maxViewDst = 450;

  public Transform viewer;
  public Material mapMaterial;

  public static Vector2 viewerPosition;
  Vector2 viewerPositionOld;
  public static MapGenerator mapGenerator;

  public int colliderLODIndex;
  float meshWorldSize;
  int chunksVisibleInViewDst;

  Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
  static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

  private void Start() {
    mapGenerator = FindObjectOfType<MapGenerator>();

    maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
    meshWorldSize = mapGenerator.meshSettings.meshWorldSize;
    chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

    updateVisibleChunks();
  }

  private void Update() {
    viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

    if (viewerPosition != viewerPositionOld) {
      foreach (TerrainChunk chunk in visibleTerrainChunks)
        chunk.updateCollisionMesh();
    }

    if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
      viewerPositionOld = viewerPosition;
      updateVisibleChunks();
    }
  }

  void updateVisibleChunks() {
    HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();

    for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--) {
      alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
      visibleTerrainChunks[i].updateTerrainChunk();
    }

    int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
    int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

    for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
      for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
        Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

        if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) {
          if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
            terrainChunkDictionary[viewedChunkCoord].updateTerrainChunk();
          }
          else {
            terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, meshWorldSize, detailLevels, colliderLODIndex, transform, mapMaterial));
          }
        }
      }
    }
  }

  public class TerrainChunk {

    public Vector2 coord;
    GameObject meshObject;
    Vector2 sampleCenter;
    Bounds bounds;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    LODMesh collisionLODMesh;

    HeightMap mapData;
    bool mapDataReceived;
    int previousLODIndex = -1;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    int colliderLODIndex;
    bool hasSetCollider;

    public TerrainChunk(Vector2 coord, float meshWorldSize, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material) {
      this.detailLevels = detailLevels;
      this.colliderLODIndex = colliderLODIndex;
      this.coord = coord;

      sampleCenter = coord * meshWorldSize / mapGenerator.meshSettings.meshScale;
      Vector2 position = coord * meshWorldSize;
      bounds = new Bounds(position, Vector2.one * meshWorldSize);

      meshObject = new GameObject("Terrain Chunk");
      meshRenderer = meshObject.AddComponent<MeshRenderer>();
      meshFilter = meshObject.AddComponent<MeshFilter>();
      meshCollider = meshObject.AddComponent<MeshCollider>();

      meshRenderer.material = material;

      meshObject.transform.position = new Vector3(position.x, 0, position.y);
      meshObject.transform.parent = parent;

      setVisible(false);

      lodMeshes = new LODMesh[detailLevels.Length];
      for (int i = 0; i < detailLevels.Length; i++) {
        lodMeshes[i] = new LODMesh(detailLevels[i].lod);
        lodMeshes[i].updateCallback += updateTerrainChunk;

        if (i == colliderLODIndex) {
          lodMeshes[i].updateCallback += updateCollisionMesh;
        }
      }

      mapGenerator.requestHeightMap(sampleCenter, onMapDataReceived);
    }

    void onMapDataReceived(HeightMap mapData) {
      this.mapData = mapData;
      mapDataReceived = true;

      updateTerrainChunk();
    }

    public void updateTerrainChunk() {
      if (mapDataReceived) {
        float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

        bool wasVisible = isVisible();
        bool visible = viewerDstFromNearestEdge <= maxViewDst;

        if (visible) {
          int lodIndex = 0;

          for (int i = 0; i < detailLevels.Length - 1; i++) {
            if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
              lodIndex = i + 1;
            }
            else {
              break;
            }
          }

          if (lodIndex != previousLODIndex) {
            LODMesh lodMesh = lodMeshes[lodIndex];
            if (lodMesh.hasMesh) {
              previousLODIndex = lodIndex;
              meshFilter.mesh = lodMesh.mesh;
            }
            else if (!lodMesh.hasRequestedMesh) {
              lodMesh.requestMesh(mapData);
            }
          }

          visibleTerrainChunks.Add(this);
        }

        if (wasVisible != visible) {
          if (visible)
            visibleTerrainChunks.Add(this);
          else {
            visibleTerrainChunks.Remove(this);
          }

          setVisible(visible);
        }
      }
    }

    public void updateCollisionMesh() {
      if (!hasSetCollider) {
        float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

        if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold) {
          if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
            lodMeshes[colliderLODIndex].requestMesh(mapData);
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

  public class LODMesh {
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    int lod;
    public event System.Action updateCallback;


    public LODMesh(int lod) {
      this.lod = lod;
    }

    void onMeshDataReceived(MeshData meshData) {
      mesh = meshData.createMesh();
      hasMesh = true;

      updateCallback();
    }

    public void requestMesh(HeightMap mapData) {
      hasRequestedMesh = true;
      mapGenerator.requestMeshData(mapData, lod, onMeshDataReceived);
    }
  }

  [System.Serializable]
  public struct LODInfo {
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float visibleDstThreshold;

    public float sqrVisibleDstThreshold {
      get {
        return visibleDstThreshold * visibleDstThreshold;
      }
    }
  }
}
