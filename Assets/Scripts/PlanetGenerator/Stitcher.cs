using UnityEngine;
using System.Collections.Generic;
using CSML;
using System.Linq;

public class Stitcher {

  static float power = 7.0f;
  static float levelSmooth = 16;

  public enum Side {
    Left,
    Right,
    Top,
    Bottom
  }

  // public static void repairCorners(Terrain center, Terrain top, Terrain right, Terrain bottom, Terrain left, Terrain rightBottom, int checkLength) {
  //   int temptLength = checkLength;
  //   checkLength = 0;

  //   if (right != null)
  //     StitchTerrains(center.terrainData.GetHeights(0, 0, center.terrainData.heightmapWidth, center.terrainData.heightmapHeight), right.terrainData.GetHeights(0, 0, right.terrainData.heightmapWidth, right.terrainData.heightmapHeight), Side.Right, checkLength, false);

  //   if (top != null)
  //     StitchTerrains(center.terrainData.GetHeights(0, 0, center.terrainData.heightmapWidth, center.terrainData.heightmapHeight), top.terrainData.GetHeights(0, 0, top.terrainData.heightmapWidth, top.terrainData.heightmapHeight), Side.Top, checkLength, false);

  //   checkLength = temptLength;

  //   if (right != null && bottom != null) {
  //     if (rightBottom != null)
  //       StitchTerrainsRepair(center, right, bottom, rightBottom);
  //   }
  // }

  private static void StitchTerrains(float[,] heights, float[,] secondHeights, Side side, int checkLength, bool smooth = true) {
    // TerrainData terrainData = terrain.terrainData;
    // TerrainData secondData = second.terrainData;
    // float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
    // float[,] secondHeights = secondData.GetHeights(0, 0, secondData.heightmapWidth, secondData.heightmapHeight);

    if (side == Side.Right) {
      int y = heights.GetLength(0) - 1;
      int x = 0;
      int y2 = 0;

      for (x = 0; x < heights.GetLength(1); x++) {
        heights[x, y] = average(heights[x, y], secondHeights[x, y2]);

        if (smooth)
          heights[x, y] += Mathf.Abs(heights[x, y - 1] - secondHeights[x, y2 + 1]) / levelSmooth;

        secondHeights[x, y2] = heights[x, y];

        for (int i = 1; i < checkLength; i++) {
          heights[x, y - i] = (average(heights[x, y - i], heights[x, y - i + 1]) + Mathf.Abs(heights[x, y - i] - heights[x, y - i + 1]) / levelSmooth) * (checkLength - i) / checkLength + heights[x, y - i] * i / checkLength;
          secondHeights[x, y2 + i] = (average(secondHeights[x, y2 + i], secondHeights[x, y2 + i - 1]) + Mathf.Abs(secondHeights[x, y2 + i] - secondHeights[x, y2 + i - 1]) / levelSmooth) * (checkLength - i) / checkLength + secondHeights[x, y2 + i] * i / checkLength;
        }
      }
    } else {
      if (side == Side.Top) {
        int y = 0;
        int x = heights.GetLength(0) - 1;
        int x2 = 0;

        for (y = 0; y < heights.GetLength(1); y++) {
          heights[x, y] = average(heights[x, y], secondHeights[x2, y]);

          if (smooth)
            heights[x, y] += Mathf.Abs(heights[x - 1, y] - secondHeights[x2 + 1, y]) / levelSmooth;

          secondHeights[x2, y] = heights[x, y];

          for (int i = 1; i < checkLength; i++) {
            heights[x - i, y] = (average(heights[x - i, y], heights[x - i + 1, y]) + Mathf.Abs(heights[x - i, y] - heights[x - i + 1, y]) / levelSmooth) * (checkLength - i) / checkLength + heights[x - i, y] * i / checkLength;
            secondHeights[x2 + i, y] = (average(secondHeights[x2 + i, y], secondHeights[x2 + i - 1, y]) + Mathf.Abs(secondHeights[x2 + i, y] - secondHeights[x2 + i - 1, y]) / levelSmooth) * (checkLength - i) / checkLength + secondHeights[x2 + i, y] * i / checkLength;
          }
        }
      }
    }

    // terrainData.SetHeights(0, 0, heights);
    // terrain.terrainData = terrainData;

    // secondData.SetHeights(0, 0, secondHeights);
    // second.terrainData = secondData;

    // terrain.Flush();
    // second.Flush();
  }

  //static void StitchTerrainsRepair(Terrain terrain11, Terrain terrain21, Terrain terrain12, Terrain terrain22) {
  static void StitchTerrainsRepair(Terrain terrain11, Terrain terrain21, Terrain terrain12, Terrain terrain22, int size) {
    // int size = terrain11.terrainData.heightmapHeight - 1;
    int size0 = 0;
    List<float> heights = new List<float>();

    heights.Add(terrain11.terrainData.GetHeights(size, size0, 1, 1)[0, 0]);
    heights.Add(terrain21.terrainData.GetHeights(size0, size0, 1, 1)[0, 0]);
    heights.Add(terrain12.terrainData.GetHeights(size, size, 1, 1)[0, 0]);
    heights.Add(terrain22.terrainData.GetHeights(size0, size, 1, 1)[0, 0]);


    float[,] height = new float[1, 1];
    height[0, 0] = heights.Max();

    terrain11.terrainData.SetHeights(size, size0, height);
    terrain21.terrainData.SetHeights(size0, size0, height);
    terrain12.terrainData.SetHeights(size, size, height);
    terrain22.terrainData.SetHeights(size0, size, height);

    terrain11.Flush();
    terrain12.Flush();
    terrain21.Flush();
    terrain22.Flush();
  }

  static float average(float first, float second) {
    return Mathf.Pow((Mathf.Pow(first, power) + Mathf.Pow(second, power)) / 2.0f, 1 / power);
  }

  public static void StitchTerrainsTrend(float[,] heights, float[,] secondHeights, Side side, int checkLength) {
    // TerrainData terrainData = terrain.terrainData;
    // TerrainData secondData = second.terrainData;
    // float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
    // float[,] secondHeights = secondData.GetHeights(0, 0, secondData.heightmapWidth, secondData.heightmapHeight);

    if (side == Side.Right) {
      //int y = heights.GetLength (0) - 1;
      int x = 0;

      //int x2 = 0;
      //int y2 = 0;
      string matrixAT = "";
      string matrixATID = "";
      string matrixATOnes = "";

      for (int i = 1; i <= checkLength; i++) {
        matrixAT += (i * i);
        matrixATID += i;
        matrixATOnes += "1";

        matrixAT += ",";
        matrixATID += ",";
        matrixATOnes += ",";

      }
      for (int i = checkLength; i <= checkLength * 2; i++) {
        matrixAT += (i * i);
        matrixATID += i;
        matrixATOnes += "1";

        if (i < checkLength * 2) {
          matrixAT += ",";
          matrixATID += ",";
          matrixATOnes += ",";
        }
      }

      Matrix AT = new Matrix(matrixAT + ";" + matrixATID + ";" + matrixATOnes);
      Matrix A = AT.Transpose();
      Matrix ATA = AT * A;
      Matrix ATA1 = ATA.Inverse();

      for (x = 0; x < heights.GetLength(1); x++) {
        string matrixZ = "";

        for (int i = heights.GetLength(0) - checkLength; i < heights.GetLength(0); i++) {
          matrixZ += heights[x, i] + ";";
        }

        for (int i = 0; i <= checkLength; i++) {
          matrixZ += secondHeights[x, i];
          if (i < checkLength) {
            matrixZ += ";";
          }
        }

        Matrix Z = new Matrix(matrixZ);
        Matrix ATZ = AT * Z;
        Matrix X = ATA1 * ATZ;
        double trendAverage = checkLength * checkLength * X[1, 1].Re + checkLength * X[2, 1].Re + X[3, 1].Re;
        Matrix sAT = new Matrix("1," + (checkLength * checkLength) + "," + Mathf.Pow(2 * checkLength, 2) + ";1," + checkLength + "," + (checkLength * 2) + ";1,1,1");
        Matrix sA = sAT.Transpose();
        Matrix sATA = sAT * sA;
        Matrix sATA1 = sATA.Inverse();
        Matrix sZ = new Matrix(heights[x, heights.GetLength(0) - checkLength] + ";" + trendAverage + ";" + secondHeights[x, checkLength]);
        Matrix sATZ = sAT * sZ;
        Matrix sX = sATA1 * sATZ;
        double[] heightTrend = new double[checkLength];
        double[] secondHeightTrend = new double[checkLength + 1];

        for (int i = 1; i <= checkLength; i++) {
          heightTrend[i - 1] = i * i * sX[1, 1].Re + i * sX[2, 1].Re + sX[3, 1].Re;
        }

        int j = 0;
        for (int i = checkLength; i <= checkLength * 2; i++) {
          secondHeightTrend[j] = i * i * sX[1, 1].Re + i * sX[2, 1].Re + sX[3, 1].Re;
          j++;
        }

        for (int i = 0; i < checkLength; i++) {
          heights[x, heights.GetLength(1) - i - 1] = (float)heightTrend[checkLength - i - 1] * (checkLength - i) / checkLength + heights[x, heights.GetLength(1) - i - 1] * i / checkLength;
        }

        for (int i = 0; i <= checkLength; i++) {
          secondHeights[x, i] = (float)secondHeightTrend[i] * (checkLength - i) / checkLength + secondHeights[x, i] * i / checkLength;
        }
      }
    } else {
      if (side == Side.Top) {
        int y = 0;
        string matrixAT = "";
        string matrixATID = "";
        string matrixATOnes = "";

        for (int i = 1; i <= checkLength; i++) {
          matrixAT += (i * i);
          matrixATID += i;
          matrixATOnes += "1";

          matrixAT += ",";
          matrixATID += ",";
          matrixATOnes += ",";

        }
        for (int i = checkLength; i <= checkLength * 2; i++) {
          matrixAT += (i * i);
          matrixATID += i;
          matrixATOnes += "1";

          if (i < checkLength * 2) {
            matrixAT += ",";
            matrixATID += ",";
            matrixATOnes += ",";
          }
        }

        Matrix AT = new Matrix(matrixAT + ";" + matrixATID + ";" + matrixATOnes);
        Matrix A = AT.Transpose();
        Matrix ATA = AT * A;
        Matrix ATA1 = ATA.Inverse();

        for (y = 0; y < heights.GetLength(1); y++) {
          string matrixZ = "";

          for (int i = heights.GetLength(0) - checkLength; i < heights.GetLength(0); i++) {
            matrixZ += heights[i, y] + ";";
          }

          for (int i = 0; i <= checkLength; i++) {
            matrixZ += secondHeights[i, y];
            if (i < checkLength) {
              matrixZ += ";";
            }
          }

          Matrix Z = new Matrix(matrixZ);
          Matrix ATZ = AT * Z;
          Matrix X = ATA1 * ATZ;
          double trendAverage = checkLength * checkLength * X[1, 1].Re + checkLength * X[2, 1].Re + X[3, 1].Re;
          Matrix sAT = new Matrix("1," + (checkLength * checkLength) + "," + Mathf.Pow(2 * checkLength, 2) + ";1," + checkLength + "," + (checkLength * 2) + ";1,1,1");
          Matrix sA = sAT.Transpose();
          Matrix sATA = sAT * sA;
          Matrix sATA1 = sATA.Inverse();
          Matrix sZ = new Matrix(heights[heights.GetLength(0) - checkLength, y] + ";" + trendAverage + ";" + secondHeights[checkLength, y]);
          Matrix sATZ = sAT * sZ;
          Matrix sX = sATA1 * sATZ;
          double[] heightTrend = new double[checkLength];
          double[] secondHeightTrend = new double[checkLength + 1];

          for (int i = 1; i <= checkLength; i++) {
            heightTrend[i - 1] = i * i * sX[1, 1].Re + i * sX[2, 1].Re + sX[3, 1].Re;
          }

          int j = 0;

          for (int i = checkLength; i <= checkLength * 2; i++) {
            secondHeightTrend[j] = i * i * sX[1, 1].Re + i * sX[2, 1].Re + sX[3, 1].Re;
            j++;
          }

          for (int i = 0; i < checkLength; i++) {
            heights[heights.GetLength(0) - i - 1, y] = (float)heightTrend[checkLength - i - 1] * (checkLength - i) / checkLength + heights[heights.GetLength(0) - i - 1, y] * i / checkLength;
          }

          for (int i = 0; i <= checkLength; i++) {
            secondHeights[i, y] = (float)secondHeightTrend[i] * (checkLength - i) / checkLength + secondHeights[i, y] * i / checkLength;
          }
        }
      }
    }

    // terrainData.SetHeights(0, 0, heights);
    // terrain.terrainData = terrainData;

    // secondData.SetHeights(0, 0, secondHeights);
    // second.terrainData = secondData;

    // terrain.Flush();
    // second.Flush();
  }

  public static void StitchTerrain(Terrain[] _terrains, int checkLength) {
    Vector2 firstPosition;
    Dictionary<int[], Terrain> _terrainDict = new Dictionary<int[], Terrain>(new IntArrayComparer());

    if (_terrains.Length > 0) {
      firstPosition = new Vector2(_terrains[0].transform.position.x, _terrains[0].transform.position.z);

      int sizeX = (int)_terrains[0].terrainData.size.x;
      int sizeZ = (int)_terrains[0].terrainData.size.z;

      foreach (var terrain in _terrains) {
        int[] posTer = new int[] {
            (int)(Mathf.RoundToInt ((terrain.transform.position.x - firstPosition.x) / sizeX)),
            (int)(Mathf.RoundToInt ((terrain.transform.position.z - firstPosition.y) / sizeZ))
          };
        _terrainDict.Add(posTer, terrain);
      }

      //Checks neighbours and stitches them
      foreach (var item in _terrainDict) {
        int[] posTer = item.Key;
        Terrain top = null;
        Terrain left = null;
        Terrain right = null;
        Terrain bottom = null;
        _terrainDict.TryGetValue(new int[] {
            posTer [0],
            posTer [1] + 1
          }, out top);
        _terrainDict.TryGetValue(new int[] {
            posTer [0] - 1,
            posTer [1]
          }, out left);
        _terrainDict.TryGetValue(new int[] {
            posTer [0] + 1,
            posTer [1]
          }, out right);
        _terrainDict.TryGetValue(new int[] {
            posTer [0],
            posTer [1] - 1
          }, out bottom);

        item.Value.SetNeighbors(left, top, right, bottom);

        item.Value.Flush();
        if (top != null) {
          float[,] itemHights = item.Value.terrainData.GetHeights(0, 0, item.Value.terrainData.heightmapWidth, item.Value.terrainData.heightmapHeight);
          float[,] topHeights = top.terrainData.GetHeights(0, 0, top.terrainData.heightmapWidth, top.terrainData.heightmapHeight);

          StitchTerrainsTrend(itemHights, topHeights, Side.Top, checkLength);

          item.Value.terrainData.SetHeights(0, 0, itemHights);
          top.terrainData.SetHeights(0, 0, topHeights);
          item.Value.Flush();
          top.Flush();
        }

        if (right != null) {
          float[,] itemHights = item.Value.terrainData.GetHeights(0, 0, item.Value.terrainData.heightmapWidth, item.Value.terrainData.heightmapHeight);
          float[,] rightHeights = right.terrainData.GetHeights(0, 0, right.terrainData.heightmapWidth, right.terrainData.heightmapHeight);

          StitchTerrainsTrend(itemHights, rightHeights, Side.Right, checkLength);

          item.Value.terrainData.SetHeights(0, 0, itemHights);
          right.terrainData.SetHeights(0, 0, rightHeights);
          item.Value.Flush();
          right.Flush();
        }
      }

      //Repairs corners
      foreach (var item in _terrainDict) {
        int[] posTer = item.Key;
        Terrain top = null;
        Terrain left = null;
        Terrain right = null;
        Terrain bottom = null;
        _terrainDict.TryGetValue(new int[] {
            posTer [0],
            posTer [1] + 1
          }, out top);
        _terrainDict.TryGetValue(new int[] {
            posTer [0] - 1,
            posTer [1]
          }, out left);
        _terrainDict.TryGetValue(new int[] {
            posTer [0] + 1,
            posTer [1]
          }, out right);
        _terrainDict.TryGetValue(new int[] {
            posTer [0],
            posTer [1] - 1
          }, out bottom);


        int temptLength = checkLength;
        checkLength = 0;

        if (right != null) {
          float[,] itemHights = item.Value.terrainData.GetHeights(0, 0, item.Value.terrainData.heightmapWidth, item.Value.terrainData.heightmapHeight);
          float[,] rightHeights = right.terrainData.GetHeights(0, 0, right.terrainData.heightmapWidth, right.terrainData.heightmapHeight);

          StitchTerrains(itemHights, rightHeights, Side.Right, checkLength, false);
          item.Value.terrainData.SetHeights(0, 0, itemHights);
          right.terrainData.SetHeights(0, 0, rightHeights);
          item.Value.Flush();
          right.Flush();
        }

        if (top != null) {
          float[,] itemHights = item.Value.terrainData.GetHeights(0, 0, item.Value.terrainData.heightmapWidth, item.Value.terrainData.heightmapHeight);
          float[,] topHeights = top.terrainData.GetHeights(0, 0, top.terrainData.heightmapWidth, top.terrainData.heightmapHeight);

          StitchTerrains(itemHights, topHeights, Side.Top, checkLength, false);
          item.Value.terrainData.SetHeights(0, 0, itemHights);
          top.terrainData.SetHeights(0, 0, topHeights);
          item.Value.Flush();
          top.Flush();
        }
        checkLength = temptLength;

        if (right != null && bottom != null) {
          Terrain rightBottom = null;
          _terrainDict.TryGetValue(new int[] {
              posTer [0] + 1,
              posTer [1] - 1
            }, out rightBottom);
          if (rightBottom != null)
            StitchTerrainsRepair(item.Value, right, bottom, rightBottom, item.Value.terrainData.heightmapHeight - 1);
        }

      }

    }
  }
}
