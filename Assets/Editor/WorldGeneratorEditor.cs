using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor {
  enum PreviewMode { Geography, Temperature, Rainfall };
  static int WORLD_SIZE = 16;
  static int PREVIEW_SIZE = 256;

  Texture2D geographyMap;
  Texture2D temperatureMap;
  Texture2D rainfallMap;
  int previewMode;
  bool showPreview;
  Vector2Int worldPos;

  /// <summary>
  /// Called when the GUI component is mounted
  /// </summary>
  public void OnEnable() {
    updatePreview();
    worldPos = new Vector2Int();
  }

  /// <summary>
  /// Redraws the preview texture
  /// </summary>
  private void updatePreview() {
    WorldGenerator mapPreview = target as WorldGenerator;
    if (mapPreview.temperature != null) {
      // Generate the height values
      float[,] heightValues = Noise.generateNoiseMap(WORLD_SIZE, WORLD_SIZE, mapPreview.geography, new Vector2(worldPos.x, worldPos.y));
      float[,] tempValues = Noise.generateNoiseMap(WORLD_SIZE, WORLD_SIZE, mapPreview.temperature, new Vector2(worldPos.x, worldPos.y));
      float[,] rainfall = Noise.generateNoiseMap(WORLD_SIZE, WORLD_SIZE, mapPreview.rainfall, new Vector2(worldPos.x, worldPos.y));

      Color[] geographyColors = new Color[WORLD_SIZE * WORLD_SIZE];
      Color[] temperatureColors = new Color[WORLD_SIZE * WORLD_SIZE];
      Color[] rainfallColors = new Color[WORLD_SIZE * WORLD_SIZE];

      for (int y = 0; y < WORLD_SIZE; y++)
        for (int x = 0; x < WORLD_SIZE; x++) {
          float height = heightValues[x, y];
          float tempNoise = tempValues[x, y];
          float rainfallNoise = rainfall[x, y];
          float temperature = tempNoise;

          geographyColors[y * WORLD_SIZE + x] = Color.Lerp(Color.black, Color.white, height);


          if (height <= mapPreview.seaLevel) {
            geographyColors[y * WORLD_SIZE + x] = new Color(0, 0, 1);
            rainfallColors[y * WORLD_SIZE + x] = Color.blue;
            temperature = temperature * height;

          } else {
            float heightCooling = height - mapPreview.seaLevel; // Higher values means higher
            temperature -= (heightCooling) * mapPreview.heightCoolingFactor;
            rainfallColors[y * WORLD_SIZE + x] = Color.Lerp(Color.black, new Color(0, 0, 1), rainfallNoise);
          }

          temperature = Mathf.Clamp01(temperature);

          if (temperature <= 0.5f)
            temperatureColors[y * WORLD_SIZE + x] = Color.Lerp(new Color(0, 0, 1), Color.yellow, Mathf.InverseLerp(0, 0.5f, temperature));
          else
            temperatureColors[y * WORLD_SIZE + x] = Color.Lerp(Color.yellow, Color.red, Mathf.InverseLerp(0.5f, 1, temperature));



        }

      // Mark indicator
      int worldX = Mathf.Clamp(worldPos.x, 0, WORLD_SIZE - 1);
      int worldY = Mathf.Clamp(worldPos.y, 0, WORLD_SIZE - 1);


      geographyColors[worldY * WORLD_SIZE + worldX] = new Color(0, 1, 0);
      temperatureColors[worldY * WORLD_SIZE + worldX] = new Color(0, 1, 0);
      rainfallColors[worldY * WORLD_SIZE + worldX] = new Color(0, 1, 0);


      geographyMap = TextureGenerator.textureFromColormap(geographyColors, WORLD_SIZE, WORLD_SIZE);
      temperatureMap = TextureGenerator.textureFromColormap(temperatureColors, WORLD_SIZE, WORLD_SIZE);
      rainfallMap = TextureGenerator.textureFromColormap(rainfallColors, WORLD_SIZE, WORLD_SIZE);
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
      previewMode = EditorGUILayout.Popup("World Preview Type", previewMode, options);
    }



    if (geographyMap != null && showPreview) {

      EditorGUILayout.BeginHorizontal();

      // Set out an area for the texture
      Rect lastRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);

      // Draw the texture
      if (previewMode == (int)PreviewMode.Geography)
        EditorGUI.DrawPreviewTexture(new Rect(lastRect.xMin, lastRect.yMin, PREVIEW_SIZE, PREVIEW_SIZE), geographyMap, null, ScaleMode.ScaleToFit);
      else if (previewMode == (int)PreviewMode.Temperature)
        EditorGUI.DrawPreviewTexture(new Rect(lastRect.xMin, lastRect.yMin, PREVIEW_SIZE, PREVIEW_SIZE), temperatureMap, null, ScaleMode.ScaleToFit);
      else
        EditorGUI.DrawPreviewTexture(new Rect(lastRect.xMin, lastRect.yMin, PREVIEW_SIZE, PREVIEW_SIZE), rainfallMap, null, ScaleMode.ScaleToFit);


      GUILayout.BeginVertical();
      worldPos = EditorGUILayout.Vector2IntField("Sample Biome", worldPos);
      GUILayout.Label("We seem to be at " + worldPos.x);
      GUILayout.EndVertical();


      EditorGUILayout.EndHorizontal();
    }

    if (EditorGUI.EndChangeCheck())
      updatePreview();
  }
}
