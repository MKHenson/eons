using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapPreview))]
public class MapPreviewEditor : Editor {
  public override void OnInspectorGUI() {
    MapPreview mapPreview = target as MapPreview;
    if (DrawDefaultInspector())
      if (mapPreview.autoUpdate) {
        mapPreview.drawMapInEditor();
      }

    if (GUILayout.Button("Generate")) {
      mapPreview.drawMapInEditor();
    }
  }
}
