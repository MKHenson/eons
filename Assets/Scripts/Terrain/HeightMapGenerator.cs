using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerator {

  private class HeightMapSampler {
    const int mainIndex = 0;
    const int northIndex = 1;
    const int northEastIndex = 2;
    const int eastIndex = 3;
    const int southEastIndex = 4;
    const int southIndex = 5;
    const int southWestIndex = 6;
    const int westIndex = 7;
    const int northWestIndex = 8;

    Terrain t;

    HeightMapSettings[] settings;
    AnimationCurve[] curves;
    float[][,] heightmaps;
    int width;
    int height;
    Vector2 coord;
    float scale;

    public void init(int width, int height, HeightMapSettings[] settings, Vector2 coord, float scale) {
      this.settings = settings;
      this.curves = new AnimationCurve[9];
      this.width = width;
      this.height = height;
      this.coord = coord;
      this.scale = scale;

      List<float[,]> heightmaps = new List<float[,]>();
      // float[,] mainValues = Noise.generateNoiseMap(width, height, settings[0].noiseSettings, coord * scale);

      heightmaps.Add(Noise.generateNoiseMap(width, height, settings[mainIndex].noiseSettings, (coord) * scale));
      heightmaps.Add(Noise.generateNoiseMap(width, height, settings[northIndex].noiseSettings, (coord + new Vector2(0, 1)) * scale));
      heightmaps.Add(Noise.generateNoiseMap(width, height, settings[northEastIndex].noiseSettings, (coord + new Vector2(1, 1)) * scale));
      heightmaps.Add(Noise.generateNoiseMap(width, height, settings[eastIndex].noiseSettings, (coord + new Vector2(1, 0)) * scale));
      heightmaps.Add(Noise.generateNoiseMap(width, height, settings[southEastIndex].noiseSettings, (coord + new Vector2(1, -1)) * scale));
      heightmaps.Add(Noise.generateNoiseMap(width, height, settings[southIndex].noiseSettings, (coord + new Vector2(0, -1)) * scale));
      heightmaps.Add(Noise.generateNoiseMap(width, height, settings[southWestIndex].noiseSettings, (coord + new Vector2(-1, -1)) * scale));
      heightmaps.Add(Noise.generateNoiseMap(width, height, settings[westIndex].noiseSettings, (coord + new Vector2(-1, 0)) * scale));
      heightmaps.Add(Noise.generateNoiseMap(width, height, settings[northWestIndex].noiseSettings, (coord + new Vector2(-1, 1)) * scale));

      for (int i = 0; i < settings.Length; i++) {
        this.curves[i] = new AnimationCurve(settings[i].heightCurve.keys);
      }

      this.heightmaps = heightmaps.ToArray();
    }

    public float sampleAt(int x, int y) {

      if (x < 0 && y < 0) {
        // North West
        float value = this.heightmaps[northWestIndex][width + x, height + y];
        float valueMultiplier = value * curves[northWestIndex].Evaluate(value) * settings[northWestIndex].heightMultiplier;
        return valueMultiplier;
      } else if (x >= 0 && x < width && y < 0) {
        // North
        // float northValue = Noise.getNoiseAtPoint(x, height + y, width, height, settings[northIndex].noiseSettings, (coord + new Vector2(0, 1)) * scale);
        float northValue = this.heightmaps[northIndex][x, height + y];
        float northBorder = northValue * curves[northIndex].Evaluate(northValue) * settings[northIndex].heightMultiplier;
        return northBorder;
      } else if (x >= width && y < 0) {
        // North East
        float value = this.heightmaps[northEastIndex][x - width, height + y];
        float valueMultiplier = value * curves[northEastIndex].Evaluate(value) * settings[northEastIndex].heightMultiplier;
        return valueMultiplier;
      } else if (x >= width && y >= 0 && y < height) {
        // East
        // float eastValue = Noise.getNoiseAtPoint(x - width, y, width, height, settings[eastIndex].noiseSettings, (coord + new Vector2(1, 0)) * scale);
        float eastValue = this.heightmaps[eastIndex][x - width, y];
        float eastBorder = eastValue * curves[eastIndex].Evaluate(eastValue) * settings[eastIndex].heightMultiplier;
        return eastBorder;
      } else if (x >= width && y >= height) {
        // South East
        float value = this.heightmaps[southEastIndex][x - width, y - height];
        float valueMultiplier = value * curves[southEastIndex].Evaluate(value) * settings[southEastIndex].heightMultiplier;
        return valueMultiplier;
      } else if (x >= 0 && x < width && y >= height) {
        // South
        // float southValue = Noise.getNoiseAtPoint(x, y - height, width, height, settings[southIndex].noiseSettings, (coord + new Vector2(0, -1)) * scale);
        float southValue = this.heightmaps[southIndex][x, y - height];
        float southBorder = southValue * curves[southIndex].Evaluate(southValue) * settings[southIndex].heightMultiplier;
        return southBorder;
      } else if (x < 0 && y >= height) {
        // South West
        float value = this.heightmaps[southWestIndex][width + x, y - height];
        float valueMultiplier = value * curves[southWestIndex].Evaluate(value) * settings[southWestIndex].heightMultiplier;
        return valueMultiplier;
      } else if (x < 0 && y >= 0 && y < height) {
        // West
        // float westValue = Noise.getNoiseAtPoint(width + x, y, width, height, settings[westIndex].noiseSettings, (coord + new Vector2(-1, 0)) * scale);
        float westValue = this.heightmaps[westIndex][width + x, y];
        float westBorder = westValue * curves[westIndex].Evaluate(westValue) * settings[westIndex].heightMultiplier;
        return westBorder;
      } else {
        // Center
        // float value = Noise.getNoiseAtPoint(x, y, width, height, settings[mainIndex].noiseSettings, coord * scale);
        float value = this.heightmaps[mainIndex][x, y];
        float valueMultiplier = value * curves[mainIndex].Evaluate(value) * settings[mainIndex].heightMultiplier;
        return valueMultiplier;
      }
    }
  }

  private static float sampleAverage(int i, int j, HeightMapSampler sampler) {
    return (sampler.sampleAt(i, j - 1) +
    sampler.sampleAt(i + 1, j - 1) +
    sampler.sampleAt(i + 1, j) +
    sampler.sampleAt(i + 1, j + 1) +
    sampler.sampleAt(i, j + 1) +
    sampler.sampleAt(i - 1, j + 1) +
    sampler.sampleAt(i - 1, j) +
    sampler.sampleAt(i - 1, j - 1) +
    sampler.sampleAt(i, j)) / 9;
  }

  public static HeightMap generateBlendedHeightmap(int width, int height, HeightMapSettings[] settings, Vector2 coord, float scale) {
    float[,] toRet = new float[width, height];
    const int mainIndex = 0;

    AnimationCurve mainCurve = new AnimationCurve(settings[mainIndex].heightCurve.keys);
    HeightMapSampler sampler = new HeightMapSampler();
    sampler.init(width, height, settings, coord, scale);

    float minValue = float.MaxValue;
    float maxValue = float.MinValue;

    const int t = 2;

    const int numBlendIndices = 20;
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        float rootValue = sampler.sampleAt(i, j);

        Vector2 curPos = new Vector2(i, j);

        //
        //       |
        //    -------
        //       | X
        if (i < numBlendIndices && j < numBlendIndices) {
          float value = (sampler.sampleAt(-1, 0) + sampler.sampleAt(0, 0) + sampler.sampleAt(0, -1) + sampler.sampleAt(-1, -1)) * 0.25f;
          float distFromCorner = Vector2.Distance(Vector2.zero, curPos);
          float distFromMax = Vector2.Distance(Vector2.zero, new Vector2(numBlendIndices, numBlendIndices));
          float strength = (Mathf.InverseLerp(distFromMax / 2, distFromMax, distFromCorner));
          rootValue = Mathf.Lerp(value, rootValue, strength);

          rootValue = value;
        }
        //
        //       | X
        //    -------
        //       |
        else if (i < numBlendIndices && j > height - 1 - numBlendIndices) {
          float value = (sampler.sampleAt(0, height - 1) + sampler.sampleAt(0, height) + sampler.sampleAt(-1, height) + sampler.sampleAt(-1, height - 1)) * 0.25f;
          float distFromCorner = Vector2.Distance(new Vector2(0, height - 1), curPos);
          float distFromMax = Vector2.Distance(new Vector2(0, height - 1), new Vector2(numBlendIndices, height - numBlendIndices));
          float strength = (Mathf.InverseLerp(distFromMax / 2, distFromMax, distFromCorner));
          rootValue = Mathf.Lerp(value, rootValue, strength);

          rootValue = value;
        }
        //
        //     X |
        //    -------
        //       |
        else if (i > width - 1 - numBlendIndices && j > height - 1 - numBlendIndices) {
          float value = (sampler.sampleAt(width - 1, height - 1) + sampler.sampleAt(width, height - 1) + sampler.sampleAt(width, height) + sampler.sampleAt(width - 1, height)) * 0.25f;
          float distFromCorner = Vector2.Distance(new Vector2(width - 1, height - 1), curPos);
          float distFromMax = Vector2.Distance(new Vector2(width - 1, height - 1), new Vector2(width - numBlendIndices, height - 1 - numBlendIndices));
          float strength = (Mathf.InverseLerp(distFromMax / 2, distFromMax, distFromCorner));
          rootValue = Mathf.Lerp(value, rootValue, strength);

          rootValue = value;
        }
        //
        //       |
        //    -------
        //     x |
        else if (i > width - 1 - numBlendIndices && j < numBlendIndices) {
          float value = (sampler.sampleAt(width - 1, 0) + sampler.sampleAt(width, 0) + sampler.sampleAt(width, -1) + sampler.sampleAt(width - 1, -1)) * 0.25f;
          float distFromCorner = Vector2.Distance(new Vector2(width - 1, 0), curPos);
          float distFromMax = Vector2.Distance(new Vector2(width - 1, 0), new Vector2(width - numBlendIndices, numBlendIndices));
          float strength = (Mathf.InverseLerp(distFromMax / 2, distFromMax, distFromCorner));
          rootValue = Mathf.Lerp(value, rootValue, strength);

          rootValue = value;
        } else {


          // East
          if (i < numBlendIndices) {
            float value = (sampler.sampleAt(-1, j) + sampler.sampleAt(0, j)) * 0.5f;
            // float value = 0;

            // if (i == 0) {
            //   if (j == 0)
            //     value = (sampler.sampleAt(-1, 0) + sampler.sampleAt(-1, -1) + sampler.sampleAt(0, -1) + sampler.sampleAt(0, 0)) * 0.25f;
            //   else if (j == height - 1)
            //     value = (sampler.sampleAt(-1, j) + sampler.sampleAt(-1, height) + sampler.sampleAt(0, height) + sampler.sampleAt(0, j)) * 0.25f;
            //   else
            //     value = (sampler.sampleAt(-1, j) + sampler.sampleAt(0, j)) * 0.5f;
            // } else
            //   value = (sampler.sampleAt(-1, j) + sampler.sampleAt(0, j)) * 0.5f;

            // value = sampleAverage(-1, j, sampler);

            float strength = (Mathf.InverseLerp(0, numBlendIndices - t, i - t));
            rootValue = Mathf.Lerp(value, rootValue, strength);
          }
          // West
          else if (i > width - 1 - numBlendIndices) {
            float value = (sampler.sampleAt(width, j) + sampler.sampleAt(width - 1, j)) * 0.5f;
            // float value = 0;

            // if (i == width - 1) {
            //   if (j == 0)
            //     value = (sampler.sampleAt(i, -1) + sampler.sampleAt(width, -1) + sampler.sampleAt(width, 0) + sampler.sampleAt(i, j)) * 0.25f;
            //   else if (j == height - 1)
            //     value = (sampler.sampleAt(i, height) + sampler.sampleAt(width, height) + sampler.sampleAt(width, j) + sampler.sampleAt(i, j)) * 0.25f;
            //   else
            //     value = (sampler.sampleAt(width, j) + sampler.sampleAt(width - 1, j)) * 0.5f;
            // } else
            //   value = (sampler.sampleAt(width, j) + sampler.sampleAt(width - 1, j)) * 0.5f;

            // value = sampleAverage(width, j, sampler);

            float strength = (Mathf.InverseLerp(width - numBlendIndices - t, width - t, i));
            rootValue = Mathf.Lerp(rootValue, value, strength);
          }

          // North
          if (j < numBlendIndices) {
            float value = (sampler.sampleAt(i, -1) + sampler.sampleAt(i, 0)) * 0.5f;
            // float value = 0;

            // if (i == 0) {
            //   if (j == 0)
            //     value = (sampler.sampleAt(-1, 0) + sampler.sampleAt(-1, -1) + sampler.sampleAt(0, -1) + sampler.sampleAt(0, 0)) * 0.25f;
            //   else if (j == height - 1)
            //     value = (sampler.sampleAt(-1, j) + sampler.sampleAt(-1, height) + sampler.sampleAt(0, height) + sampler.sampleAt(0, j)) * 0.25f;
            //   else
            //     value = (sampler.sampleAt(i, -1) + sampler.sampleAt(i, 0)) * 0.5f;
            // } else
            //   value = (sampler.sampleAt(i, -1) + sampler.sampleAt(i, 0)) * 0.5f;

            // value = sampleAverage(i, -1, sampler);

            float strength = (Mathf.InverseLerp(0, numBlendIndices - t, j - t));
            rootValue = Mathf.Lerp(value, rootValue, strength);
          }
          // South
          else if (j > height - 1 - numBlendIndices) {
            float value = (sampler.sampleAt(i, height) + sampler.sampleAt(i, height - 1)) * 0.5f;

            // value = (sampler.sampleAt(width - 1, j) + sampler.sampleAt(width - 1, j - 1) + sampler.sampleAt(width, j - 1) + sampler.sampleAt(width, j)) * 0.25f;

            // float value = 0;
            // if (i == width - 1) {
            //   if (j == 0)
            //     value = (sampler.sampleAt(i, -1) + sampler.sampleAt(width, -1) + sampler.sampleAt(width, 0) + sampler.sampleAt(i, j)) * 0.25f;
            //   else if (j == height - 1)
            //     value = (sampler.sampleAt(i, height) + sampler.sampleAt(width, height) + sampler.sampleAt(width, j) + sampler.sampleAt(i, j)) * 0.25f;
            //   else
            //     value = (sampler.sampleAt(i, height) + sampler.sampleAt(i, height - 1)) * 0.5f;
            // } else
            //   value = (sampler.sampleAt(i, height) + sampler.sampleAt(i, height - 1)) * 0.5f;

            // value = sampleAverage(i, height, sampler);

            float strength = (Mathf.InverseLerp(height - numBlendIndices - t, height - t, j));
            rootValue = Mathf.Lerp(rootValue, value, strength);
          }


        }

        toRet[i, j] = rootValue;

        if (toRet[i, j] > maxValue)
          maxValue = toRet[i, j];
        if (toRet[i, j] < minValue)
          minValue = toRet[i, j];
      }
    }

    return new HeightMap(toRet, minValue, maxValue);
  }

  public static HeightMap generateHeightmap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter) {
    float[,] values = Noise.generateNoiseMap(width, height, settings.noiseSettings, sampleCenter);

    AnimationCurve heightCurveThreadsafe = new AnimationCurve(settings.heightCurve.keys);

    float minValue = float.MaxValue;
    float maxValue = float.MinValue;

    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        values[i, j] *= heightCurveThreadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;

        if (values[i, j] > maxValue)
          maxValue = values[i, j];
        if (values[i, j] < minValue)
          minValue = values[i, j];
      }
    }

    return new HeightMap(values, minValue, maxValue);
  }
}

public struct HeightMap {
  public readonly float[,] values;
  public readonly float minValue;
  public readonly float maxValue;

  public HeightMap(float[,] values, float minValue, float maxValue) {
    this.values = values;
    this.minValue = minValue;
    this.maxValue = maxValue;
  }
}