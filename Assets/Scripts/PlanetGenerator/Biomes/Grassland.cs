using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grassland : Biome {
  private static Texture2D grass;
  private static Texture2D grassNormalMap;

  public Grassland() : base(BiomeType.Grassland) {
  }

  public override HeightMap generateHeightmap(int size, Vector2 offset) {
    HeightMapSettings settings = ScriptableObject.CreateInstance("HeightMapSettings") as HeightMapSettings;
    settings.useFalloff = true;
    settings.noiseSettings = new NoiseSettings();
    settings.noiseSettings.scale = 1600;
    settings.noiseSettings.octaves = 5;
    settings.noiseSettings.lacunarity = 2.3f;
    settings.noiseSettings.persistance = 0.61f;

    settings.heightCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 0), new Keyframe(1, 1) });

    return HeightMapGenerator.generateHeightmap(size, size, settings, offset);
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
