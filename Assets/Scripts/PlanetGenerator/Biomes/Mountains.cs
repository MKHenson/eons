using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mountains : Biome {
  private static Texture2D grass;
  private static Texture2D grassNormalMap;

  public Mountains() : base(BiomeType.Mountains) {
  }

  public override HeightMap generateHeightmap(int size, Vector2 offset) {
    HeightmapSettings settings = new HeightmapSettings();
    settings.useFalloff = true;
    settings.heightMultiplier = 1;
    settings.noiseSettings = new NoiseSettings();
    settings.noiseSettings.scale = 100;
    settings.noiseSettings.octaves = 6;
    settings.noiseSettings.lacunarity = 2.2f;
    settings.noiseSettings.persistance = 0.51f;

    settings.heightCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.5f, 0.3f), new Keyframe(1, 1) });

    return HeightMapGenerator.generateHeightmap(size, size, settings, offset);
  }

  public override TerrainLayer[] generateLayers() {
    TerrainLayer[] toReturn = new TerrainLayer[] {
        new TerrainLayer()
    };

    if (!grass) {
      grass = Resources.Load<Texture2D>("Terrain/Textures/mountain-gravel");
      grassNormalMap = Resources.Load<Texture2D>("Terrain/Textures/mountain-gravel-normal");
    }

    toReturn[0].diffuseTexture = grass;
    toReturn[0].normalMapTexture = grassNormalMap;
    toReturn[0].normalScale = 0.8f;
    toReturn[0].specular = new Color(1, 1, 1, 1);
    toReturn[0].tileOffset = new Vector2(4, 4);
    toReturn[0].smoothness = 0.3f;

    return toReturn;
  }
}
