using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldSettings {
  public NoiseSettings geography;
  public NoiseSettings temperature;
  public NoiseSettings rainfall;

  [Range(0, 1)]
  public float seaLevel = 0.2f;

  [Range(0, 4)]
  public float heightCoolingFactor = 0.4f;
}
