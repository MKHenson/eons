using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerator {

  public static HeightMap generateBlendedHeightmap(int width, int height, HeightMapSettings[] settings, Vector2 sampleCenter) {
    float[,] mainValues = Noise.generateNoiseMap(width, height, settings[0].noiseSettings, sampleCenter);
    float[,] northValues = Noise.generateNoiseMap(width, height, settings[1].noiseSettings, sampleCenter);
    float[,] eastValues = Noise.generateNoiseMap(width, height, settings[2].noiseSettings, sampleCenter);
    float[,] southValues = Noise.generateNoiseMap(width, height, settings[3].noiseSettings, sampleCenter);
    float[,] westValues = Noise.generateNoiseMap(width, height, settings[4].noiseSettings, sampleCenter);

    AnimationCurve mainCurve = new AnimationCurve(settings[0].heightCurve.keys);
    AnimationCurve northCurve = new AnimationCurve(settings[1].heightCurve.keys);
    AnimationCurve eastCurve = new AnimationCurve(settings[2].heightCurve.keys);
    AnimationCurve southCurve = new AnimationCurve(settings[3].heightCurve.keys);
    AnimationCurve westCurve = new AnimationCurve(settings[4].heightCurve.keys);

    float minValue = float.MaxValue;
    float maxValue = float.MinValue;

    const int numBlendIndices = 50;
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < height; j++) {
        float rootValue = mainValues[i, j] * mainCurve.Evaluate(mainValues[i, j]) * settings[0].heightMultiplier;

        // East
        if (i < numBlendIndices) {
          float eastValue = eastValues[i, j] * eastCurve.Evaluate(eastValues[i, j]) * settings[2].heightMultiplier;
          float average = (rootValue + eastValue) / 2;
          float strength = (Mathf.InverseLerp(0, numBlendIndices, i));
          rootValue = Mathf.Lerp(0, rootValue, strength);
        }
        // West
        else if (i > width - numBlendIndices) {
          float westValue = westValues[i, j] * westCurve.Evaluate(westValues[i, j]) * settings[4].heightMultiplier;
          float average = (rootValue + westValue) / 2;
          float strength = (Mathf.InverseLerp(width - numBlendIndices, width, i));
          rootValue = Mathf.Lerp(rootValue, 0, strength);
        }

        // North
        if (j < numBlendIndices) {
          float northValue = (rootValue + northValues[i, j] * northCurve.Evaluate(northValues[i, j]) * settings[1].heightMultiplier) / 2;
          float strength = (Mathf.InverseLerp(0, numBlendIndices, j));
          float average = (rootValue + northValue) / 2;
          rootValue = Mathf.Lerp(0, rootValue, strength);
        }
        // South
        else if (j > height - numBlendIndices) {
          float southValue = (rootValue + southValues[i, j] * southCurve.Evaluate(southValues[i, j]) * settings[3].heightMultiplier) / 2;
          float strength = (Mathf.InverseLerp(height - numBlendIndices, height, j));
          float average = (rootValue + southValue) / 2;
          rootValue = Mathf.Lerp(rootValue, 0, strength);
        }

        mainValues[i, j] = rootValue;

        if (mainValues[i, j] > maxValue)
          maxValue = mainValues[i, j];
        if (mainValues[i, j] < minValue)
          minValue = mainValues[i, j];
      }
    }

    return new HeightMap(mainValues, minValue, maxValue);
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