using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {
  Mesh hexMesh;
  List<Vector3> vertices;
  List<int> triangles;
  List<Color> colors;
  MeshCollider meshCollider;

  // Start is called before the first frame update
  void Awake() {
    GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
    GetComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/VertexColors"));
    meshCollider = gameObject.AddComponent<MeshCollider>();
    hexMesh.name = "HexMesh";
    vertices = new List<Vector3>();
    triangles = new List<int>();
    colors = new List<Color>();
  }

  public void triangulate(HexCell[] cells) {
    hexMesh.Clear();
    vertices.Clear();
    triangles.Clear();
    colors.Clear();

    for (int i = 0; i < cells.Length; i++)
      triangulate(cells[i]);

    hexMesh.vertices = vertices.ToArray();
    hexMesh.triangles = triangles.ToArray();
    hexMesh.colors = colors.ToArray();
    hexMesh.RecalculateNormals();

    meshCollider.sharedMesh = hexMesh;
  }

  public void triangulate(HexCell cell) {
    Vector3 centre = cell.transform.localPosition;
    for (int i = 0; i < 6; i++) {
      addTriangle(
          centre,
          centre + HexMetrics.corners[i],
          centre + HexMetrics.corners[i + 1]
      );
      AddTriangleColor(cell.color);
    }
  }

  void AddTriangleColor(Color color) {
    colors.Add(color);
    colors.Add(color);
    colors.Add(color);
  }

  void addTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
    int vertexIndex = vertices.Count;
    vertices.Add(v1);
    vertices.Add(v2);
    vertices.Add(v3);
    triangles.Add(vertexIndex);
    triangles.Add(vertexIndex + 1);
    triangles.Add(vertexIndex + 2);
  }
}
