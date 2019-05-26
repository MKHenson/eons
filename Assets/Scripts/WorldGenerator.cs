using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour {
  public NoiseSettings geography;
  public NoiseSettings temperature;
  public NoiseSettings rainfall;

  [Range(0, 1)]
  public float seaLevel;

  [Range(0, 4)]
  public float heightCoolingFactor;
}
