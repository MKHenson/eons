using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor {
  enum PreviewMode { Geography, Temperature, Rainfall };
  static int PREVIEW_SIZE = 256;

  Texture2D geographyMap;
  Texture2D temperatureMap;
  int previewMode;
  bool showPreview;

  /// <summary>
  /// Called when the GUI component is mounted
  /// </summary>
  public void OnEnable() {
    updatePreview();
  }

  /// <summary>
  /// Redraws the preview texture
  /// </summary>
  private void updatePreview() {
    WorldGenerator mapPreview = target as WorldGenerator;
    if (mapPreview.temperature != null) {
      // Generate the height values
      float[,] heightValues = Noise.generateNoiseMap(PREVIEW_SIZE, PREVIEW_SIZE, mapPreview.geography, new Vector2());
      float[,] tempValues = Noise.generateNoiseMap(PREVIEW_SIZE, PREVIEW_SIZE, mapPreview.temperature, new Vector2());

      Color[] geographyColors = new Color[PREVIEW_SIZE * PREVIEW_SIZE];
      Color[] temperatureColors = new Color[PREVIEW_SIZE * PREVIEW_SIZE];

      for (int y = 0; y < PREVIEW_SIZE; y++)
        for (int x = 0; x < PREVIEW_SIZE; x++) {
          float height = heightValues[x, y];
          float tempNoise = tempValues[x, y];
          float temperature = tempNoise;

          geographyColors[y * PREVIEW_SIZE + x] = Color.Lerp(Color.black, Color.white, height);

          if (height <= mapPreview.seaLevel) {
            geographyColors[y * PREVIEW_SIZE + x] = new Color(0, 0, 100);
            temperature = temperature * height;
          } else {
            float heightCooling = height - mapPreview.seaLevel; // Higher values means higher
            temperature -= (heightCooling) * mapPreview.heightCoolingFactor;
          }

          temperature = Mathf.Clamp01(temperature);

          if (temperature <= 0.5f)
            temperatureColors[y * PREVIEW_SIZE + x] = Color.Lerp(new Color(0, 0, 200), Color.yellow, Mathf.InverseLerp(0, 0.5f, temperature));
          else
            temperatureColors[y * PREVIEW_SIZE + x] = Color.Lerp(Color.yellow, Color.red, Mathf.InverseLerp(0.5f, 1, temperature));
        }

      geographyMap = TextureGenerator.textureFromColormap(geographyColors, PREVIEW_SIZE, PREVIEW_SIZE);
      temperatureMap = TextureGenerator.textureFromColormap(temperatureColors, PREVIEW_SIZE, PREVIEW_SIZE);
    }
  }


  public override void OnInspectorGUI() {
    WorldGenerator mapPreview = target as WorldGenerator;

    EditorGUI.BeginChangeCheck();

    // Show the default editors
    base.OnInspectorGUI();

    showPreview = EditorGUILayout.Foldout(showPreview, "Preview");

    if (showPreview) {
      string[] options = Enum.GetNames(typeof(PreviewMode));
      previewMode = EditorGUILayout.Popup("Preview Mode", previewMode, options);
    }

    if (EditorGUI.EndChangeCheck())
      updatePreview();

    if (geographyMap != null && showPreview) {
      // Set out an area for the texture
      Rect lastRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);

      // Draw the texture
      if (previewMode == (int)PreviewMode.Geography)
        EditorGUI.DrawPreviewTexture(new Rect(lastRect.xMin, lastRect.yMin, PREVIEW_SIZE, PREVIEW_SIZE), geographyMap, null, ScaleMode.ScaleToFit);
      else
        EditorGUI.DrawPreviewTexture(new Rect(lastRect.xMin, lastRect.yMin, PREVIEW_SIZE, PREVIEW_SIZE), temperatureMap, null, ScaleMode.ScaleToFit);
    }
  }
}
