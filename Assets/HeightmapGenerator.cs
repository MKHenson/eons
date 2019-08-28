using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class HeightmapGenerator : MonoBehaviour {
  public Renderer textureRenderer;
  public MeshFilter meshFilter;
  public MeshRenderer meshRenderer;
  public HeightMapSettings heightMapSettings;
  public HeightMapSettings heightMapSettings2;
  public HeightMap[] generated;

  // Start is called before the first frame update
  void Start() {
    drawMapInEditor();
  }

  public void drawMapInEditor() {
    int textureSize = 512;

    if (generated == null) {
      HeightMap[] temp = {
        HeightMapGenerator.generateHeightmap(textureSize, textureSize, heightMapSettings2, Vector2.zero),
        HeightMapGenerator.generateHeightmap(textureSize, textureSize, heightMapSettings2, Vector2.zero),
        HeightMapGenerator.generateHeightmap(textureSize, textureSize, heightMapSettings2, Vector2.zero),
        HeightMapGenerator.generateHeightmap(textureSize, textureSize, heightMapSettings2, Vector2.zero),
        HeightMapGenerator.generateHeightmap(textureSize, textureSize, heightMapSettings, Vector2.zero),
        HeightMapGenerator.generateHeightmap(textureSize, textureSize, heightMapSettings2, Vector2.zero),
        HeightMapGenerator.generateHeightmap(textureSize, textureSize, heightMapSettings2, Vector2.zero),
        HeightMapGenerator.generateHeightmap(textureSize, textureSize, heightMapSettings2, Vector2.zero),
        HeightMapGenerator.generateHeightmap(textureSize, textureSize, heightMapSettings2, Vector2.zero)
    };
      generated = temp;
    }

    HeightMap[,] heightMapsMacro = {
        {generated[0], generated[1], generated[2]},
        {generated[3], generated[4], generated[5]},
        {generated[6], generated[7], generated[8]}
    };

    float min = generated.Min(heitmap => heitmap.minValue);
    float max = generated.Max(heitmap => heitmap.maxValue);

    int combinedWidth = textureSize * 3;
    int combinedHeight = textureSize * 3;

    Color[] colorMap = new Color[combinedWidth * combinedHeight];

    for (int y = 0; y < combinedHeight; y++)
      for (int x = 0; x < combinedWidth; x++) {
        int arrayX = 0;
        int arrayY = 0;

        if (x >= textureSize && x < textureSize * 2)
          arrayX = 1;
        else if (x >= textureSize * 2)
          arrayX = 2;

        if (y >= textureSize && y < textureSize * 2)
          arrayY = 1;
        else if (y >= textureSize * 2)
          arrayY = 2;

        colorMap[y * combinedWidth + x] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(min, max, heightMapsMacro[arrayX, arrayY].values[x % textureSize, y % textureSize]));
      }

    Color[] blurredColorMapFlattened = GaussianBlur.FastGaussianBlur(colorMap, combinedWidth, combinedHeight, 30);
    drawTexture(TextureGenerator.textureFromColormap(blurredColorMapFlattened, combinedWidth, combinedHeight));
  }

  public void drawTexture(Texture2D texture) {
    textureRenderer.sharedMaterial.mainTexture = texture;
    textureRenderer.gameObject.SetActive(true);
  }

  void onValuesUpdated() {
    if (!Application.isPlaying)
      drawMapInEditor();
  }

  void OnValidate() {
    if (heightMapSettings != null) {
      heightMapSettings.onValuesUpdated -= onValuesUpdated;
      heightMapSettings.onValuesUpdated += onValuesUpdated;
    }
  }
}
