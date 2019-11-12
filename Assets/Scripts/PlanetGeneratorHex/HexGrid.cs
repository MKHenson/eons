using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class HexGrid : MonoBehaviour {
  public int width = 6;
  public int height = 6;
  public HexCell cellPrefab;
  public Text cellLabelPrefab;
  public Color defaultColor = Color.white;
  public Color touchedColor = Color.magenta;

  private HexCell[] cells;
  private Canvas gridCanvas;
  private HexMesh hexMesh;


  void Awake() {
    cells = new HexCell[height * width];
    gridCanvas = GetComponentInChildren<Canvas>();
    hexMesh = GetComponentInChildren<HexMesh>();

    for (int z = 0, i = 0; z < height; z++) {
      for (int x = 0; x < width; x++) {
        createCell(x, z, i++);
      }
    }
  }

  void Start() {
    hexMesh.Triangulate(cells);
  }

  public void ColorCell(Vector3 position, Color color) {
    position = transform.InverseTransformPoint(position);
    HexCoordinates coordinates = HexCoordinates.FromPosition(position);
    Debug.Log("Touched at " + coordinates.ToString());

    int index = coordinates.x + coordinates.z * width + coordinates.z / 2;
    HexCell cell = cells[index];
    cell.color = color;
    hexMesh.Triangulate(cells);
  }

  private void createCell(int x, int z, int i) {
    Vector3 position = new Vector3();
    position.x = (x + z * 0.5f - z / 2) * (HexMetrics.INNER_RADIUS * 2f);
    position.y = 0f;
    position.z = z * (HexMetrics.OUTER_RADIUS * 1.5f);

    HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
    cell.transform.SetParent(transform, false);
    cell.transform.localPosition = position;
    cell.coordinates = HexCoordinates.fromOffsetCoordinates(x, z);
    cell.color = defaultColor;

    if (x > 0) {
      cell.SetNeighbor(HexDirection.W, cells[i - 1]);
    }
    if (z > 0) {
      if ((z & 1) == 0) {
        cell.SetNeighbor(HexDirection.SE, cells[i - width]);
        if (x > 0) {
          cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
        }
      } else {
        cell.SetNeighbor(HexDirection.SW, cells[i - width]);
        if (x < width - 1) {
          cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
        }
      }
    }

    Text label = Instantiate<Text>(cellLabelPrefab);
    label.rectTransform.SetParent(gridCanvas.transform, false);
    label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
    label.text = cell.coordinates.toStringOnSeparateLines();
  }
}
