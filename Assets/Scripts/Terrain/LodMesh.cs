using UnityEngine;

public class LODMesh {
  public Mesh mesh;
  public bool hasRequestedMesh;
  public bool hasMesh;
  private int lod;
  public event System.Action updateCallback;


  public LODMesh(int lod) {
    this.lod = lod;
  }

  void onMeshDataReceived(object meshDataObject) {
    mesh = (meshDataObject as MeshData).createMesh();
    hasMesh = true;

    updateCallback();
  }

  public void requestMesh(HeightMap heightMap, MeshSettings meshSettings) { // , HeightMapSettings heightMapSettings) {
    hasRequestedMesh = true;

    // if (heightMapSettings.useFalloff) {
    //   float[,] falloff = FalloffGenerator.generateFalloffMap(meshSettings.numVerticesPerLine);
    //   for (int i = 0; i < heightMap.values.GetLength(0); i++)
    //     for (int j = 0; j < heightMap.values.GetLength(1); j++)
    //       heightMap.values[i, j] = heightMap.values[i, j] * (1 - falloff[i, j]);
    // }

    ThreadedDataRequester.requestData(() => MeshGenerator.generateTerrainMesh(heightMap.values, meshSettings, lod), onMeshDataReceived);
  }
}
