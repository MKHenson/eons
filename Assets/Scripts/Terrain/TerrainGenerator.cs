using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crest;

public class TerrainGenerator : MonoBehaviour {

  const float viewerMoveThresholdForChunkUpdate = 25f;
  const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
  public event Action onLoaded;

  public int colliderLODIndex;
  public LODInfo[] detailLevels;

  public MeshSettings meshSettings;
  public HeightMapSettings heightMapSettings;
  public TextureData textureSettings;

  public Transform viewer;
  public Material mapMaterial;

  public WorldGenerator worldGenerator;

  Vector2 viewerPosition;
  Vector2 viewerPositionOld;

  float meshWorldSize;
  int chunksVisibleInViewDst;

  Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
  List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

  bool initialMeshesLoaded = false;

  private void Start() {
    textureSettings.applyToMaterial(mapMaterial);
    textureSettings.updateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

    worldGenerator.load(heightMapSettings);

    float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
    meshWorldSize = meshSettings.meshWorldSize;
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

    if (!initialMeshesLoaded) {
      int numLoaded = 0;
      foreach (TerrainChunk chunk in terrainChunkDictionary.Values) {
        if (chunk.hasMesh)
          numLoaded++;
        if (numLoaded == chunksVisibleInViewDst * 2 * chunksVisibleInViewDst * 2) {
          initialMeshesLoaded = true;

          if (onLoaded != null)
            onLoaded();
        }
      }
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
          } else {
            TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, worldGenerator, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
            terrainChunkDictionary.Add(viewedChunkCoord, newChunk);

            newChunk.onVisibilityChanged += onTerrainChunkVisibilityChanged;
            newChunk.load();
          }
        }
      }
    }
  }

  void onTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
    if (isVisible)
      visibleTerrainChunks.Add(chunk);
    else
      visibleTerrainChunks.Remove(chunk);
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