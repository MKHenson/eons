using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HexMetrics {
  public const float OUTER_RADIUS = 10;
  public const float INNER_RADIUS = OUTER_RADIUS * 0.866025404f;
  public const float SOLID_FACTOR = 0.75f;
  public const float BLEND_FACTOR = 1f - SOLID_FACTOR;

  static Vector3[] corners = {
      new Vector3(0f, 0f, OUTER_RADIUS),
      new Vector3(INNER_RADIUS, 0f, 0.5f * OUTER_RADIUS ),
      new Vector3( INNER_RADIUS, 0f, -0.5f * OUTER_RADIUS ),
      new Vector3(0f, 0f, -OUTER_RADIUS),
      new Vector3(-INNER_RADIUS, 0f, -0.5f * OUTER_RADIUS),
      new Vector3(-INNER_RADIUS, 0f, 0.5f * OUTER_RADIUS ),
      new Vector3(0f, 0f, OUTER_RADIUS) // Back to begining
  };

  public static Vector3 GetFirstCorner(HexDirection direction) {
    return corners[(int)direction];
  }

  public static Vector3 GetSecondCorner(HexDirection direction) {
    return corners[(int)direction + 1];
  }

  public static Vector3 GetFirstSolidCorner(HexDirection direction) {
    return corners[(int)direction] * SOLID_FACTOR;
  }

  public static Vector3 GetSecondSolidCorner(HexDirection direction) {
    return corners[(int)direction + 1] * SOLID_FACTOR;
  }

  public static Vector3 GetBridge(HexDirection direction) {
    return (corners[(int)direction] + corners[(int)direction + 1]) * BLEND_FACTOR;
  }
}
