using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour {
  const int textureSize = 512;
  const TextureFormat textureFormat = TextureFormat.RGB565;

  public NoiseSettings geography;
  public NoiseSettings temperature;
  public NoiseSettings rainfall;

  public BiomeMaterialSettings[] biomes;
  private List<MaterialLookup> materialsLookup = new List<MaterialLookup>();

  [Range(0, 1)]
  public float seaLevel;

  [Range(0, 4)]
  public float heightCoolingFactor;

  public void load(HeightMapSettings settings) {
    for (int i = 0; i < biomes.Length; i++) {
      biomes[i].textureSettings.applyToMaterial(biomes[i].material);
      biomes[i].textureSettings.updateMeshHeights(biomes[i].material, settings.minHeight, settings.maxHeight);
    }
  }

  public Material getMaterialForBiome(BiomeData biome) {
    foreach (MaterialLookup ml in materialsLookup) {
      if (ml.biome == biome.biomeType && ml.north == biome.north.biomeType &&
      ml.east == biome.east.biomeType &&
      ml.south == biome.south.biomeType &&
      ml.west == biome.west.biomeType) {
        return ml.material;
      }
    }

    MaterialLookup newLookup = new MaterialLookup();
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

  public void generateMaterialUniforms(Material material, BiomeData biome) {
    List<BiomeMaterialSettings> settings = new List<BiomeMaterialSettings>();
    List<Texture2D> textures = new List<Texture2D>();
    List<float> heights = new List<float>();
    List<float> blends = new List<float>();
    List<float> uvScales = new List<float>();

    settings.Add(getMaterialSettings(biome.biomeType));
    settings.Add(getMaterialSettings(biome.north.biomeType));
    settings.Add(getMaterialSettings(biome.east.biomeType));
    settings.Add(getMaterialSettings(biome.south.biomeType));
    settings.Add(getMaterialSettings(biome.west.biomeType));

    foreach (BiomeMaterialSettings setting in settings) {
      textures.Add(setting.main);
      textures.Add(setting.mainNormal);
      textures.Add(setting.secondary);
      textures.Add(setting.secondaryNormal);
      heights.Add(setting.blendHeight);
      blends.Add(setting.blendAmount);
      uvScales.Add(setting.mainUVScale);
      uvScales.Add(setting.secondaryUVScale);
    }

    material.SetFloatArray("baseStartHeights", heights.ToArray());
    material.SetFloatArray("baseBlends", blends.ToArray());
    material.SetFloatArray("baseTextureScales", uvScales.ToArray());
    material.SetTexture("baseTextures", TextureGenerator.generateTextureArray(textures.ToArray(), textureSize, textureFormat));
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
    mainBiome.east = queryBiomAt(1, 0, size, heights, temps, rainfalls);
    mainBiome.south = queryBiomAt(0, 1, size, heights, temps, rainfalls);
    mainBiome.west = queryBiomAt(-1, 0, size, heights, temps, rainfalls);

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
  public Material material;
  public TextureData textureSettings;
  public BiomeType type;

  public Shader shader;
  public Texture2D main;
  public Texture2D mainNormal;
  public Texture2D secondary;
  public Texture2D secondaryNormal;
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
  public BiomeData east;
  public BiomeData south;
  public BiomeData west;
}

class MaterialLookup {
  public Material material;
  public BiomeType biome;
  public BiomeType north;
  public BiomeType east;
  public BiomeType south;
  public BiomeType west;
}