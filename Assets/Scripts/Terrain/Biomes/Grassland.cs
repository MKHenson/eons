using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grassland : Biome {
  private static Texture2D grass;
  private static Texture2D grassNormalMap;

  public Grassland() : base(BiomeType.Grassland) {

  }

  public override TerrainLayer[] generateLayers() {
    TerrainLayer[] toReturn = new TerrainLayer[] {
        new TerrainLayer()
    };

    if (!grass) {
      grass = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless");
      grassNormalMap = Resources.Load<Texture2D>("Terrain/Textures/grass-seamless-norm");
    }

    toReturn[0].diffuseTexture = grass;
    toReturn[0].normalMapTexture = grassNormalMap;
    toReturn[0].normalScale = 0.8f;
    toReturn[0].tileOffset = new Vector2(5, 5);

    return toReturn;
  }
}
