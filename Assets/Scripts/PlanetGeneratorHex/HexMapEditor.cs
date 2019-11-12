﻿using UnityEngine.EventSystems;
using UnityEngine;

public class HexMapEditor : MonoBehaviour {
  public Color[] colors;
  public HexGrid hexGrid;
  private Color activeColor;
  private int activeElevation;

  void Awake() {
    SelectColor(0);
  }

  void Update() {
    // If we left click and the pointer is not over a UI component
    if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) {
      HandleInput();
    }
  }

  void HandleInput() {
    Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
    RaycastHit hit;
    if (Physics.Raycast(inputRay, out hit)) {
      EditCell(hexGrid.GetCell(hit.point));
    }
  }

  void EditCell(HexCell cell) {
    cell.color = activeColor;
    cell.Elevation = activeElevation;
    hexGrid.Refresh();
  }

  public void SelectColor(int index) {
    activeColor = colors[index];
  }

  public void SetElevation(float elevation) {
    activeElevation = (int)elevation;
  }
}
