using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise3D {

  public float frequency = 16f;
  public int octaves = 16;
  public float lacunarity = 2f;
  public float persistence = 0.5f;
  public int dimensions = 3;
  public NoiseMethodType type;
  public Gradient coloring;

  public Noise3D() {
    coloring = new Gradient();
    coloring.colorKeys = new GradientColorKey[] { new GradientColorKey(Color.blue, 0), new GradientColorKey(Color.green, 0.5f), new GradientColorKey(Color.red, 1) };
  }


  public Color[,] Generate(Transform transform, int resolution) {
    Vector3 point00 = transform.TransformPoint(new Vector3(-0.5f, -0.5f));
    Vector3 point10 = transform.TransformPoint(new Vector3(0.5f, -0.5f));
    Vector3 point01 = transform.TransformPoint(new Vector3(-0.5f, 0.5f));
    Vector3 point11 = transform.TransformPoint(new Vector3(0.5f, 0.5f));

    NoiseMethod method = PerlinNoise.methods[(int)type][dimensions - 1];

    float stepSize = 1f / resolution;

    Color[,] values = new Color[resolution, resolution];

    for (int y = 0; y < resolution; y++) {
      Vector3 point0 = Vector3.Lerp(point00, point01, (y + 0.5f) * stepSize);
      Vector3 point1 = Vector3.Lerp(point10, point11, (y + 0.5f) * stepSize);
      for (int x = 0; x < resolution; x++) {
        Vector3 point = Vector3.Lerp(point0, point1, (x + 0.5f) * stepSize);
        float sample = PerlinNoise.Sum(method, point, frequency, octaves, lacunarity, persistence);
        if (type != NoiseMethodType.Value) {
          sample = sample * 0.5f + 0.5f;
        }

        Color color = coloring.Evaluate(sample);
        values[x, y] = color;
      }
    }

    return values;
  }

  public Texture2D GenerateTexture(Transform transform, int resolution) {
    Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, true);
    texture.name = "Procedural Texture";
    texture.wrapMode = TextureWrapMode.Clamp;
    texture.filterMode = FilterMode.Trilinear;
    texture.anisoLevel = 9;
    var colors = Generate(transform, resolution);
    for (int y = 0; y < resolution; y++)
      for (int x = 0; x < resolution; x++)
        texture.SetPixel(x, y, colors[x, y]);

    return texture;
  }
}
