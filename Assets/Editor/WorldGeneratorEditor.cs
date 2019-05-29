using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(WorldGenerator))]
public class WorldGeneratorEditor : Editor {
  enum PreviewMode { Geography, Temperature, Rainfall, Biome };
  enum PreviewSize { Small, Medium, Large };

  static int PREVIEW_SIZE = 256;

  Texture2D geographyMap;
  Texture2D temperatureMap;
  Texture2D rainfallMap;
  Texture2D biomeMap;

  int previewSize;
  int previewSizePixels = 16;
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
    WorldGenerator worldGenerator = target as WorldGenerator;
    if (worldGenerator.temperature != null) {
      // Generate the height values
      float[,] heightValues = Noise.generateNoiseMap(previewSizePixels, previewSizePixels, worldGenerator.geography, new Vector2(worldPos.x, worldPos.y));
      float[,] tempValues = Noise.generateNoiseMap(previewSizePixels, previewSizePixels, worldGenerator.temperature, new Vector2(worldPos.x, worldPos.y));
      float[,] rainfall = Noise.generateNoiseMap(previewSizePixels, previewSizePixels, worldGenerator.rainfall, new Vector2(worldPos.x, worldPos.y));

      Color[] geographyColors = new Color[previewSizePixels * previewSizePixels];
      Color[] temperatureColors = new Color[previewSizePixels * previewSizePixels];
      Color[] rainfallColors = new Color[previewSizePixels * previewSizePixels];
      Color[] biomeColors = new Color[previewSizePixels * previewSizePixels];

      for (int y = 0; y < previewSizePixels; y++)
        for (int x = 0; x < previewSizePixels; x++) {
          float height = heightValues[x, y];
          float temperature = worldGenerator.getTemp(tempValues[x, y], height);
          float rianfall = rainfall[x, y];

          BiomeType biomeType = worldGenerator.getBiomeType(temperature, rianfall, height);
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


          biomeColors[y * previewSizePixels + x] = biomeColor;

          if (height <= worldGenerator.seaLevel) {
            geographyColors[y * previewSizePixels + x] = Color.blue;
            rainfallColors[y * previewSizePixels + x] = Color.blue;
          } else {
            geographyColors[y * previewSizePixels + x] = Color.Lerp(Color.black, Color.white, height);
            rainfallColors[y * previewSizePixels + x] = Color.Lerp(Color.black, Color.blue, rianfall);
          }

          if (temperature <= 0.5f)
            temperatureColors[y * previewSizePixels + x] = Color.Lerp(Color.blue, Color.yellow, Mathf.InverseLerp(0, 0.5f, temperature));
          else
            temperatureColors[y * previewSizePixels + x] = Color.Lerp(Color.yellow, Color.red, Mathf.InverseLerp(0.5f, 1, temperature));
        }

      int halfWorld = (previewSizePixels / 2);

      geographyColors[halfWorld * previewSizePixels + halfWorld] = new Color(0, 1, 0);
      temperatureColors[halfWorld * previewSizePixels + halfWorld] = new Color(0, 1, 0);
      rainfallColors[halfWorld * previewSizePixels + halfWorld] = new Color(0, 1, 0);
      biomeColors[halfWorld * previewSizePixels + halfWorld] = new Color(0, 1, 0);

      geographyMap = TextureGenerator.textureFromColormap(geographyColors, previewSizePixels, previewSizePixels);
      temperatureMap = TextureGenerator.textureFromColormap(temperatureColors, previewSizePixels, previewSizePixels);
      rainfallMap = TextureGenerator.textureFromColormap(rainfallColors, previewSizePixels, previewSizePixels);
      biomeMap = TextureGenerator.textureFromColormap(biomeColors, previewSizePixels, previewSizePixels);
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

      string[] previewSizeOptions = Enum.GetNames(typeof(PreviewSize));
      previewSize = EditorGUILayout.Popup("World Preview Size", previewSize, previewSizeOptions);

      if (previewSize == (int)PreviewSize.Small)
        previewSizePixels = 16;
      else if (previewSize == (int)PreviewSize.Medium)
        previewSizePixels = 32;
      else if (previewSize == (int)PreviewSize.Large)
        previewSizePixels = 64;
    }



    if (geographyMap != null && showPreview) {

      EditorGUILayout.BeginHorizontal();

      // Set out an area for the texture
      // float curWidth = EditorGUIUtility.currentViewWidth;
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

      BiomeData biom = mapPreview.queryBiomAt(worldPos.x, worldPos.y, previewSizePixels);

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
