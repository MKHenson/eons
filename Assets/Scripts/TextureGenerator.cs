using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {

  public static Texture2D textureFromColormap(Color[] colors, int width, int height) {
    Texture2D texture = new Texture2D(width, height);
    texture.filterMode = FilterMode.Point;
    texture.wrapMode = TextureWrapMode.Clamp;
    texture.SetPixels(colors);
    texture.Apply();

    return texture;
  }

  public static Texture2D textureFromHeightmap(HeightMap heightmap) {
    int width = heightmap.values.GetLength(0);
    int height = heightmap.values.GetLength(1);

    Color[] colorMap = new Color[width * height];
    for (int y = 0; y < height; y++)
      for (int x = 0; x < width; x++)
        colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightmap.minValue, heightmap.maxValue, heightmap.values[x, y]));

    return textureFromColormap(colorMap, width, height);
  }
}
