using System;
using UnityEngine;

public class GaussianBlur : MonoBehaviour {
  public static Color[] FastGaussianBlur(Color[] src, int width, int height, int Raduis) {
    var bxs = boxesForGaussian(Raduis, 3);
    Color[] img = FastBoxBlur(src, width, height, bxs[0]);
    Color[] img_2 = FastBoxBlur(img, width, height, bxs[1]);
    Color[] img_3 = FastBoxBlur(img_2, width, height, bxs[2]);
    return img_3;
  }

  private static int[] boxesForGaussian(double sigma, int n) {
    double wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
    double wl = Math.Floor(wIdeal);

    if (wl % 2 == 0) wl--;
    double wu = wl + 2;

    double mIdeal = (12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
    double m = Math.Round(mIdeal);

    int[] sizes = new int[n];
    for (int i = 0; i < n; i++) {
      if (i < m) {
        sizes[i] = (int)wl;
      } else {
        sizes[i] = (int)wu;
      }
    }
    return sizes;
  }

  private static Color[] FastBoxBlur(Color[] img, int width, int height, int radius) {

    int kSize = radius;

    if (kSize % 2 == 0) kSize++;
    Color[] Hblur = img.Clone() as Color[];

    float Avg = (float)1 / kSize;

    for (int j = 0; j < height; j++) {

      float[] hSum = new float[] {
      0f, 0f, 0f, 0f
      };

      float[] iAvg = new float[] {
      0f, 0f, 0f, 0f
      };

      for (int x = 0; x < kSize; x++) {
        Color tmpColor = img[j * width + x];
        hSum[0] += tmpColor.a;
        hSum[1] += tmpColor.r;
        hSum[2] += tmpColor.g;
        hSum[3] += tmpColor.b;
      }
      iAvg[0] = hSum[0] * Avg;
      iAvg[1] = hSum[1] * Avg;
      iAvg[2] = hSum[2] * Avg;
      iAvg[3] = hSum[3] * Avg;

      for (int i = 0; i < width; i++) {

        if (i - kSize / 2 >= 0 && i + 1 + kSize / 2 < width) {
          Color tmp_pColor = img[j * width + (i - kSize / 2)];
          hSum[0] -= tmp_pColor.a;
          hSum[1] -= tmp_pColor.r;
          hSum[2] -= tmp_pColor.g;
          hSum[3] -= tmp_pColor.b;
          Color tmp_nColor = img[j * width + (i + 1 + kSize / 2)];
          hSum[0] += tmp_nColor.a;
          hSum[1] += tmp_nColor.r;
          hSum[2] += tmp_nColor.g;
          hSum[3] += tmp_nColor.b;
          //
          iAvg[0] = hSum[0] * Avg;
          iAvg[1] = hSum[1] * Avg;
          iAvg[2] = hSum[2] * Avg;
          iAvg[3] = hSum[3] * Avg;
        }

        Hblur[j * width + i] = new Color();
        Hblur[j * width + i].a = iAvg[0];
        Hblur[j * width + i].r = iAvg[1];
        Hblur[j * width + i].g = iAvg[2];
        Hblur[j * width + i].b = iAvg[3];
      }
    }

    Color[] total = Hblur.Clone() as Color[];
    for (int i = 0; i < width; i++) {
      float[] tSum = new float[] {
      0f, 0f, 0f, 0f
      };
      float[] iAvg = new float[] {
      0f, 0f, 0f, 0f
      };

      for (int y = 0; y < kSize; y++) {
        Color tmpColor = Hblur[y * width + i];
        tSum[0] += tmpColor.a;
        tSum[1] += tmpColor.r;
        tSum[2] += tmpColor.g;
        tSum[3] += tmpColor.b;
      }
      iAvg[0] = tSum[0] * Avg;
      iAvg[1] = tSum[1] * Avg;
      iAvg[2] = tSum[2] * Avg;
      iAvg[3] = tSum[3] * Avg;

      for (int j = 0; j < height; j++) {
        if (j - kSize / 2 >= 0 && j + 1 + kSize / 2 < height) {
          Color tmp_pColor = Hblur[(j - kSize / 2) * width + i];
          tSum[0] -= tmp_pColor.a;
          tSum[1] -= tmp_pColor.r;
          tSum[2] -= tmp_pColor.g;
          tSum[3] -= tmp_pColor.b;
          Color tmp_nColor = Hblur[(j + 1 + kSize / 2) * width + i];
          tSum[0] += tmp_nColor.a;
          tSum[1] += tmp_nColor.g;
          tSum[2] += tmp_nColor.g;
          tSum[3] += tmp_nColor.b;
          //
          iAvg[0] = tSum[0] * Avg;
          iAvg[1] = tSum[1] * Avg;
          iAvg[2] = tSum[2] * Avg;
          iAvg[3] = tSum[3] * Avg;
        }

        total[j * width + i] = new Color();
        total[j * width + i].a = iAvg[0];
        total[j * width + i].r = iAvg[1];
        total[j * width + i].g = iAvg[2];
        total[j * width + i].b = iAvg[3];
      }
    }
    return total;
  }

  // private Color[,] FastGaussianBlur(Color[,] src, int width, int height, int Raduis) {
  //   var bxs = boxesForGaussian(Raduis, 3);
  //   Color[,] img = FastBoxBlur(src, width, height, bxs[0]);
  //   Color[,] img_2 = FastBoxBlur(img, width, height, bxs[1]);
  //   Color[,] img_3 = FastBoxBlur(img_2, width, height, bxs[2]);
  //   return img_3;
  // }

  // private int[] boxesForGaussian(double sigma, int n) {
  //   double wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
  //   double wl = Math.Floor(wIdeal);

  //   if (wl % 2 == 0) wl--;
  //   double wu = wl + 2;

  //   double mIdeal = (12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
  //   double m = Math.Round(mIdeal);

  //   int[] sizes = new int[n];
  //   for (int i = 0; i < n; i++) {
  //     if (i < m) {
  //       sizes[i] = (int)wl;
  //     } else {
  //       sizes[i] = (int)wu;
  //     }
  //   }
  //   return sizes;
  // }

  // private Color[,] FastBoxBlur(Color[,] img, int width, int height, int radius) {

  //   int kSize = radius;

  //   if (kSize % 2 == 0) kSize++;
  //   Color[,] Hblur = img.Clone() as Color[,];

  //   float Avg = (float)1 / kSize;

  //   for (int j = 0; j < height; j++) {

  //     float[] hSum = new float[] {
  //     0f, 0f, 0f, 0f
  //     };

  //     float[] iAvg = new float[] {
  //     0f, 0f, 0f, 0f
  //     };

  //     for (int x = 0; x < kSize; x++) {
  //       Color tmpColor = img[x, j];
  //       hSum[0] += tmpColor.a;
  //       hSum[1] += tmpColor.r;
  //       hSum[2] += tmpColor.g;
  //       hSum[3] += tmpColor.b;
  //     }
  //     iAvg[0] = hSum[0] * Avg;
  //     iAvg[1] = hSum[1] * Avg;
  //     iAvg[2] = hSum[2] * Avg;
  //     iAvg[3] = hSum[3] * Avg;

  //     for (int i = 0; i < width; i++) {

  //       if (i - kSize / 2 >= 0 && i + 1 + kSize / 2 < width) {
  //         Color tmp_pColor = img[i - kSize / 2, j];
  //         hSum[0] -= tmp_pColor.a;
  //         hSum[1] -= tmp_pColor.r;
  //         hSum[2] -= tmp_pColor.g;
  //         hSum[3] -= tmp_pColor.b;
  //         Color tmp_nColor = img[i + 1 + kSize / 2, j];
  //         hSum[0] += tmp_nColor.a;
  //         hSum[1] += tmp_nColor.r;
  //         hSum[2] += tmp_nColor.g;
  //         hSum[3] += tmp_nColor.b;
  //         //
  //         iAvg[0] = hSum[0] * Avg;
  //         iAvg[1] = hSum[1] * Avg;
  //         iAvg[2] = hSum[2] * Avg;
  //         iAvg[3] = hSum[3] * Avg;
  //       }

  //       Hblur[i, j] = new Color();
  //       Hblur[i, j].a = iAvg[0];
  //       Hblur[i, j].r = iAvg[1];
  //       Hblur[i, j].g = iAvg[2];
  //       Hblur[i, j].b = iAvg[3];
  //     }
  //   }

  //   Color[,] total = Hblur.Clone() as Color[,];
  //   for (int i = 0; i < width; i++) {
  //     float[] tSum = new float[] {
  //     0f, 0f, 0f, 0f
  //     };
  //     float[] iAvg = new float[] {
  //     0f, 0f, 0f, 0f
  //     };

  //     for (int y = 0; y < kSize; y++) {
  //       Color tmpColor = Hblur[i, y];
  //       tSum[0] += tmpColor.a;
  //       tSum[1] += tmpColor.r;
  //       tSum[2] += tmpColor.g;
  //       tSum[3] += tmpColor.b;
  //     }
  //     iAvg[0] = tSum[0] * Avg;
  //     iAvg[1] = tSum[1] * Avg;
  //     iAvg[2] = tSum[2] * Avg;
  //     iAvg[3] = tSum[3] * Avg;

  //     for (int j = 0; j < height; j++) {
  //       if (j - kSize / 2 >= 0 && j + 1 + kSize / 2 < height) {
  //         Color tmp_pColor = Hblur[i, j - kSize / 2];
  //         tSum[0] -= tmp_pColor.a;
  //         tSum[1] -= tmp_pColor.r;
  //         tSum[2] -= tmp_pColor.g;
  //         tSum[3] -= tmp_pColor.b;
  //         Color tmp_nColor = Hblur[i, j + 1 + kSize / 2];
  //         tSum[0] += tmp_nColor.a;
  //         tSum[1] += tmp_nColor.g;
  //         tSum[2] += tmp_nColor.g;
  //         tSum[3] += tmp_nColor.b;
  //         //
  //         iAvg[0] = tSum[0] * Avg;
  //         iAvg[1] = tSum[1] * Avg;
  //         iAvg[2] = tSum[2] * Avg;
  //         iAvg[3] = tSum[3] * Avg;
  //       }

  //       total[i, j] = new Color();
  //       total[i, j].a = iAvg[0];
  //       total[i, j].r = iAvg[1];
  //       total[i, j].g = iAvg[2];
  //       total[i, j].b = iAvg[3];
  //     }
  //   }
  //   return total;
  // }

}
