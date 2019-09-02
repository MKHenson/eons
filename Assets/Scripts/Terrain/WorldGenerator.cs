using System;
using System.Collections.Generic;
using UnityEngine;


public class WorldGenerator : MonoBehaviour {
  const int textureSize = 512;
  const TextureFormat textureFormat = TextureFormat.RGB565;

  public MeshSettings meshSettings;
  public NoiseSettings geography;
  public NoiseSettings temperature;
  public NoiseSettings rainfall;
  public BiomeMaterialSettings[] biomes;
  private List<BiomeLookup> materialsLookup = new List<BiomeLookup>();
  private Texture2DArray textures;

  [Range(0, 1)]
  public float seaLevel;

  [Range(0, 4)]
  public float heightCoolingFactor;

  void Start() {
    List<Texture2D> texturesList = new List<Texture2D>();

    foreach (BiomeMaterialSettings materialSetting in biomes) {
      texturesList.Add(materialSetting.main);
      texturesList.Add(materialSetting.mainNormal);
      texturesList.Add(materialSetting.secondary);
      texturesList.Add(materialSetting.secondaryNormal);
    }

    textures = TextureGenerator.generateTextureArray(texturesList.ToArray(), textureSize, textureFormat);
  }

  HeightMapSettings getHeightSettings(BiomeData biome) {
    foreach (BiomeMaterialSettings setting in biomes)
      if (setting.type == biome.biomeType) {
        return setting.heightMapSettings;
      }

    return biomes[0].heightMapSettings;
  }

  public Material getMaterialForBiome(BiomeData biome) {
    foreach (BiomeLookup ml in materialsLookup) {
      if (ml.biome == biome.biomeType && ml.north == biome.north.biomeType &&
      ml.east == biome.east.biomeType &&
      ml.south == biome.south.biomeType &&
      ml.west == biome.west.biomeType) {
        return ml.material;
      }
    }

    BiomeLookup newLookup = new BiomeLookup();
    newLookup.north = biome.north.biomeType;
    newLookup.east = biome.east.biomeType;
    newLookup.south = biome.south.biomeType;
    newLookup.west = biome.west.biomeType;
    newLookup.biome = biome.biomeType;

    for (int i = 0; i < biomes.Length; i++) {
      if (biomes[i].type == biome.biomeType)
        newLookup.material = new Material(biomes[i].shader);
    }

    if (!newLookup.material)
      newLookup.material = new Material(biomes[0].shader);

    materialsLookup.Add(newLookup);
    return newLookup.material;
  }

  private BiomeMaterialSettings getMaterialSettings(BiomeType type) {
    for (int i = 0; i < biomes.Length; i++) {
      if (biomes[i].type == type)
        return biomes[i];
    }

    return biomes[0];
  }

  public void generateMaterialUniforms(Material material, BiomeData biome, HeightMap heightMap) {
    BiomeMaterialSettings[] settings = new BiomeMaterialSettings[5];
    float[] heights = new float[5];
    float[] blends = new float[5];
    float[] primaryUvScales = new float[5];
    float[] secondaryUvScales = new float[5];
    float[] textureIndices = new float[5];

    settings[0] = getMaterialSettings(biome.biomeType);
    settings[1] = getMaterialSettings(biome.north.biomeType);
    settings[2] = getMaterialSettings(biome.east.biomeType);
    settings[3] = getMaterialSettings(biome.south.biomeType);
    settings[4] = getMaterialSettings(biome.west.biomeType);

    for (int i = 0; i < 5; i++) {
      heights[i] = settings[i].blendHeight;
      blends[i] = settings[i].blendAmount;
      primaryUvScales[i] = settings[i].mainUVScale;
      secondaryUvScales[i] = settings[i].secondaryUVScale;

      // Times 4 as there are 4 texture lookups per biome
      textureIndices[i] = Array.IndexOf(biomes, settings[i]) * 4;
    }

    material.SetFloat("minHeight", heightMap.minValue);
    material.SetFloat("maxHeight", heightMap.maxValue);
    material.SetFloatArray("baseStartHeights", heights);
    material.SetFloatArray("baseBlends", blends);
    material.SetFloatArray("primaryUvScales", primaryUvScales);
    material.SetFloatArray("secondaryUvScales", secondaryUvScales);
    material.SetFloatArray("textureIndices", textureIndices);
    material.SetTexture("baseTextures", textures);
  }

  public HeightMap generateBiomeHeightmap(BiomeData biome, Vector2 coord) {
    HeightMapSettings[] heightmapSettings = new HeightMapSettings[9];
    heightmapSettings[0] = getHeightSettings(biome);
    heightmapSettings[1] = getHeightSettings(biome.north);
    heightmapSettings[2] = getHeightSettings(biome.northEast);
    heightmapSettings[3] = getHeightSettings(biome.east);
    heightmapSettings[4] = getHeightSettings(biome.southEast);
    heightmapSettings[5] = getHeightSettings(biome.south);
    heightmapSettings[6] = getHeightSettings(biome.southWest);
    heightmapSettings[7] = getHeightSettings(biome.west);
    heightmapSettings[8] = getHeightSettings(biome.northWest);

    float scale = meshSettings.meshWorldSize / meshSettings.meshScale;

    HeightMap heightmap = HeightMapGenerator.generateBlendedHeightmap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightmapSettings, coord, scale);
    return heightmap;
  }

  public Texture2D generateGeographyMap(Vector2Int worldPos, int size) {
    float[,] heightValues = Noise.generateNoiseMap(size, size, geography, new Vector2(worldPos.x, worldPos.y));
    Color[] geographyColors = new Color[size * size];

    for (int y = 0; y < size; y++)
      for (int x = 0; x < size; x++) {
        float height = heightValues[x, y];
        geographyColors[y * size + x] = Color.Lerp(Color.black, Color.white, height);

        if (height <= seaLevel)
          geographyColors[y * size + x] = new Color(0, 0, 1);
      }

    int halfWorld = (size / 2);
    geographyColors[halfWorld * size + halfWorld] = new Color(0, 1, 0);

    Texture2D geographyMap = TextureGenerator.textureFromColormap(geographyColors, size, size);
    return geographyMap;
  }

  public float getTemp(float baseTemp, float height) {
    float temperature = baseTemp;

    // Adjust the temperature based on the terrain height
    if (height <= seaLevel) {
      temperature = temperature * height;
    } else {
      float heightCooling = height - seaLevel;
      temperature -= (heightCooling) * heightCoolingFactor;
    }

    temperature = Mathf.Clamp01(temperature);
    return temperature;
  }

  public BiomeType getBiomeType(float temperature, float rainfall, float height) {
    BiomeType biomeType;

    if (height <= seaLevel / 2) {
      biomeType = BiomeType.DeepOcean;
    } else if (height <= seaLevel) {
      biomeType = BiomeType.Ocean;
    } else if (height <= seaLevel + ((1 - seaLevel) / 2)) {

      if (temperature < 0.3f) {
        if (rainfall < 0.5f)
          biomeType = BiomeType.Grassland;
        else
          biomeType = BiomeType.TemperateForest;
      } else if (temperature < 0.6f) {
        if (rainfall < 0.4f)
          biomeType = BiomeType.Grassland;
        else
          biomeType = BiomeType.Jungle;
      } else if (temperature < 0.8f) {
        biomeType = BiomeType.Grassland;
      } else {
        biomeType = BiomeType.Dessert;
      }
    } else {
      if (height < 0.85f)
        biomeType = BiomeType.Mountains;
      else
        biomeType = BiomeType.SnowyPeaks;
    }

    return biomeType;
  }

  public GeographyType getGeographyType(float height) {
    GeographyType geographyType;

    if (height <= seaLevel / 2) {
      geographyType = GeographyType.DeepOcean;
    } else if (height <= seaLevel) {
      geographyType = GeographyType.Ocean;
    } else if (height <= seaLevel + ((1 - seaLevel) / 2)) {
      geographyType = GeographyType.LowAltitude;
    } else {
      geographyType = GeographyType.HighAltitude;
    }

    return geographyType;
  }

  public BiomeData queryBiom(int x, int y, int size) {
    Vector2 location = new Vector2(x, y);
    float[,] heights = Noise.generateNoiseMap(size, size, geography, location);
    float[,] temps = Noise.generateNoiseMap(size, size, temperature, location);
    float[,] rainfalls = Noise.generateNoiseMap(size, size, rainfall, location);

    BiomeData mainBiome = queryBiomAt(0, 0, size, heights, temps, rainfalls);
    mainBiome.location = location;
    mainBiome.north = queryBiomAt(0, -1, size, heights, temps, rainfalls);
    mainBiome.northEast = queryBiomAt(1, -1, size, heights, temps, rainfalls);
    mainBiome.east = queryBiomAt(1, 0, size, heights, temps, rainfalls);
    mainBiome.southEast = queryBiomAt(1, 1, size, heights, temps, rainfalls);
    mainBiome.south = queryBiomAt(0, 1, size, heights, temps, rainfalls);
    mainBiome.southWest = queryBiomAt(-1, 1, size, heights, temps, rainfalls);
    mainBiome.west = queryBiomAt(-1, 0, size, heights, temps, rainfalls);
    mainBiome.northWest = queryBiomAt(-1, 1, size, heights, temps, rainfalls);

    return mainBiome;
  }

  private BiomeData queryBiomAt(int x, int y, int size, float[,] heights, float[,] temps, float[,] rainfalls) {
    BiomeData toReturn = new BiomeData();
    int centerIndex = size / 2;
    toReturn.height = heights[centerIndex + x, centerIndex + y];
    toReturn.temperature = getTemp(temps[centerIndex + x, centerIndex + y], toReturn.height);
    toReturn.rainfall = rainfalls[centerIndex + x, centerIndex + y];
    toReturn.biomeType = getBiomeType(toReturn.temperature, toReturn.rainfall, toReturn.height);
    toReturn.geographyType = getGeographyType(toReturn.height);

    return toReturn;
  }
}

public enum GeographyType {
  DeepOcean,
  Ocean,
  LowAltitude,
  HighAltitude
}

public enum BiomeType {
  DeepOcean,
  Ocean,
  Grassland,
  Dessert,
  TemperateForest,
  Jungle,
  Mountains,
  SnowyPeaks
}

[System.Serializable]
public class BiomeMaterialSettings {
  public BiomeType type;
  public Shader shader;
  public Texture2D main;
  public Texture2D mainNormal;
  public Texture2D secondary;
  public Texture2D secondaryNormal;
  public HeightMapSettings heightMapSettings;
  public float blendHeight;
  public float blendAmount;
  public float mainUVScale;
  public float secondaryUVScale;
}

public class BiomeData {
  public Vector2 location;
  public GeographyType geographyType;
  public BiomeType biomeType;
  public float height;
  public float temperature;
  public float rainfall;
  public BiomeData north;
  public BiomeData northEast;
  public BiomeData east;
  public BiomeData southEast;
  public BiomeData south;
  public BiomeData southWest;
  public BiomeData west;
  public BiomeData northWest;
}

class BiomeLookup {
  public Material material;
  public BiomeType biome;
  public BiomeType north;
  public BiomeType east;
  public BiomeType south;
  public BiomeType west;
}