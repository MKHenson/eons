using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor {
  enum PreviewMode { Geography, Temperature, Rainfall, Biome };
  static int WORLD_SIZE = 16;
  static int PREVIEW_SIZE = 256;

  Texture2D geographyMap;
  Texture2D temperatureMap;
  Texture2D rainfallMap;
  Texture2D biomeMap;

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
      Color[] biomeColors = new Color[WORLD_SIZE * WORLD_SIZE];

      for (int y = 0; y < WORLD_SIZE; y++)
        for (int x = 0; x < WORLD_SIZE; x++) {
          float height = heightValues[x, y];
          float temperature = mapPreview.getTemp(tempValues[x, y], height);
          float rianfall = rainfall[x, y];

          BiomeType biomeType = mapPreview.getBiomeType(temperature, rianfall, height);
          Color biomeColor;

          if (biomeType == BiomeType.DeepOcean)
            biomeColor = new Color(0, 0, 0.7f);
          else if (biomeType == BiomeType.Ocean)
            biomeColor = new Color(0, 0, 1f);
          else if (biomeType == BiomeType.Dessert)
            biomeColor = new Color(1f, 1f, 0);
          else if (biomeType == BiomeType.Grassland)
            biomeColor = new Color(0f, 0.6f, 0);
          else if (biomeType == BiomeType.Jungle)
            biomeColor = new Color(0.0f, 0.3f, 0.0f);
          else if (biomeType == BiomeType.TemperateForest)
            biomeColor = new Color(0.0f, 0.6f, 0.6f);
          else if (biomeType == BiomeType.SnowyPeaks)
            biomeColor = new Color(0.9f, 0.9f, 0.9f);
          else // Mountains
            biomeColor = new Color(0.7f, 0.6f, 0.3f);


          biomeColors[y * WORLD_SIZE + x] = biomeColor;

          if (height <= mapPreview.seaLevel) {
            geographyColors[y * WORLD_SIZE + x] = Color.blue;
            rainfallColors[y * WORLD_SIZE + x] = Color.blue;
          } else {
            geographyColors[y * WORLD_SIZE + x] = Color.Lerp(Color.black, Color.white, height);
            rainfallColors[y * WORLD_SIZE + x] = Color.Lerp(Color.black, Color.blue, rianfall);
          }

          if (temperature <= 0.5f)
            temperatureColors[y * WORLD_SIZE + x] = Color.Lerp(Color.blue, Color.yellow, Mathf.InverseLerp(0, 0.5f, temperature));
          else
            temperatureColors[y * WORLD_SIZE + x] = Color.Lerp(Color.yellow, Color.red, Mathf.InverseLerp(0.5f, 1, temperature));
        }

      int halfWorld = (WORLD_SIZE / 2);

      geographyColors[halfWorld * WORLD_SIZE + halfWorld] = new Color(0, 1, 0);
      temperatureColors[halfWorld * WORLD_SIZE + halfWorld] = new Color(0, 1, 0);
      rainfallColors[halfWorld * WORLD_SIZE + halfWorld] = new Color(0, 1, 0);
      biomeColors[halfWorld * WORLD_SIZE + halfWorld] = new Color(0, 1, 0);

      geographyMap = TextureGenerator.textureFromColormap(geographyColors, WORLD_SIZE, WORLD_SIZE);
      temperatureMap = TextureGenerator.textureFromColormap(temperatureColors, WORLD_SIZE, WORLD_SIZE);
      rainfallMap = TextureGenerator.textureFromColormap(rainfallColors, WORLD_SIZE, WORLD_SIZE);
      biomeMap = TextureGenerator.textureFromColormap(biomeColors, WORLD_SIZE, WORLD_SIZE);
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
      else if (previewMode == (int)PreviewMode.Biome)
        EditorGUI.DrawPreviewTexture(new Rect(lastRect.xMin, lastRect.yMin, PREVIEW_SIZE, PREVIEW_SIZE), biomeMap, null, ScaleMode.ScaleToFit);
      else
        EditorGUI.DrawPreviewTexture(new Rect(lastRect.xMin, lastRect.yMin, PREVIEW_SIZE, PREVIEW_SIZE), rainfallMap, null, ScaleMode.ScaleToFit);

      BiomeData biom = mapPreview.queryBiomAt(worldPos.x, worldPos.y, WORLD_SIZE);

      GUILayout.BeginVertical();
      worldPos = EditorGUILayout.Vector2IntField("Tile Position", worldPos);
      GUILayout.Label("Biome Class: " + Enum.GetName(typeof(BiomeType), biom.biomeType));
      GUILayout.Label("Geography Class: " + Enum.GetName(typeof(GeographyType), biom.geographyType));
      GUILayout.Label("Height: " + biom.height);
      GUILayout.Label("Temperature: " + biom.temperature);
      GUILayout.Label("Rainfall: " + biom.rainfall);
      GUILayout.EndVertical();


      EditorGUILayout.EndHorizontal();
    }

    if (EditorGUI.EndChangeCheck())
      updatePreview();
  }
}
