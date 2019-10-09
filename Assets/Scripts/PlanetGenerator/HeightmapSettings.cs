using UnityEngine;

public class HeightmapSettings {
  public NoiseSettings noiseSettings;
  public bool useFalloff = false;
  public float heightMultiplier = 1;
  public AnimationCurve heightCurve;

  public float minHeight {
    get {
      return heightMultiplier * heightCurve.Evaluate(0);
    }
  }

  public float maxHeight {
    get {
      return heightMultiplier * heightCurve.Evaluate(1);
    }
  }
}
