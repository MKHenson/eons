using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeightmapGenerator))]
public class HeightmapGeneratorEditor : Editor {
  public override void OnInspectorGUI() {
    HeightmapGenerator mapPreview = target as HeightmapGenerator;
    if (DrawDefaultInspector()) {

    }

    if (GUILayout.Button("Generate")) {
      mapPreview.drawMapInEditor();
    }
  }
}