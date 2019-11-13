using UnityEngine;

public class HexCell : MonoBehaviour {
  public HexCoordinates coordinates;
  public Color color;
  private int elevation;
  public RectTransform uiRect;

  [SerializeField]
  HexCell[] neighbors;

  public int Elevation {
    get {
      return elevation;
    }
    set {
      elevation = value;
      Vector3 position = transform.localPosition;
      position.y = value * HexMetrics.ELEVATION_STEP;
      transform.localPosition = position;

      Vector3 uiPosition = uiRect.localPosition;
      uiPosition.z = elevation * -HexMetrics.ELEVATION_STEP;
      uiRect.localPosition = uiPosition;
    }
  }

  public HexCell GetNeighbor(HexDirection direction) {
    return neighbors[(int)direction];
  }

  public void SetNeighbor(HexDirection direction, HexCell cell) {
    neighbors[(int)direction] = cell;
    cell.neighbors[(int)direction.Opposite()] = this;
  }

  public HexEdgeType GetEdgeType(HexDirection direction) {
    return HexMetrics.GetEdgeType(
      elevation, neighbors[(int)direction].elevation
    );
  }

  public HexEdgeType GetEdgeType(HexCell otherCell) {
    return HexMetrics.GetEdgeType(
      elevation, otherCell.elevation
    );
  }
}
