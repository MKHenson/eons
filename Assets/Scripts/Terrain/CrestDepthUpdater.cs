using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crest;

/// <summary>
/// Creates a renderer that follows the player on the terrain and renders to
/// the Crest Ocean Depth render target. The renderer will redraw each time the player moves past
/// a certain diameter as well as reposition the renderer above the player. In order for this to work
/// The child game objects of the parent node must be tagged with the layer "Terrain"
/// </summary>
public class CrestDepthUpdater : MonoBehaviour {
  public Transform player;
  GameObject depthRenderer;
  OceanDepthCache cache;
  float distanceToUpdate = 100;
  float rendererScale = 500;
  // TerrainGenerator terrainGenerator;

  private void Start() {
    depthRenderer = new GameObject("Crest Depth Renderer");
    depthRenderer.transform.parent = transform;
    depthRenderer.transform.localScale = new Vector3(rendererScale, 1, rendererScale);
    cache = depthRenderer.AddComponent<OceanDepthCache>();
    cache._layerNames = new string[] { "Terrain" };
    // terrainGenerator = transform.GetComponent<TerrainGenerator>();
    // terrainGenerator.onLoaded += onChunksLoaded;
  }

  private void onChunksLoaded() {
    // terrainGenerator.onLoaded -= onChunksLoaded;
    cache.PopulateCache();
  }

  void Awake() {
    InitViewpoint();
  }

  void InitViewpoint() {
    if (player == null) {
      var camMain = Camera.main;
      if (camMain != null) {
        player = camMain.transform;
      } else {
        Debug.LogError("Please provide the viewpoint transform, or tag the primary camera as MainCamera.", this);
      }
    }
  }

  private void Update() {
    Vector3 deltaPosition = depthRenderer.transform.position - player.position;
    float distanceFromRenderer = deltaPosition.magnitude;

    if (distanceFromRenderer > distanceToUpdate) {
      depthRenderer.transform.position = player.position;
      cache.PopulateCache();
    }
  }
}
