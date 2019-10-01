using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator {
  private WorldSettings settings;

  public WorldGenerator(WorldSettings settings) {
    this.settings = settings;
  }

  public Texture2D generateGeographyMap(Vector2Int worldPos, int size) {
    float[,] heightValues = Noise.generateNoiseMap(size, size, settings.geography, new Vector2(worldPos.x, worldPos.y));
    Color[] geographyColors = new Color[size * size];

    for (int y = 0; y < size; y++)
      for (int x = 0; x < size; x++) {
        float height = heightValues[x, y];
        geographyColors[y * size + x] = Color.Lerp(Color.black, Color.white, height);

        if (height <= settings.seaLevel)
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
    if (height <= settings.seaLevel) {
      temperature = temperature * height;
    } else {
      float heightCooling = height - settings.seaLevel;
      temperature -= (heightCooling) * settings.heightCoolingFactor;
    }

    temperature = Mathf.Clamp01(temperature);
    return temperature;
  }

  public BiomeType getBiomeType(float temperature, float rainfall, float height) {
    BiomeType biomeType;

    if (height <= settings.seaLevel / 2) {
      biomeType = BiomeType.DeepOcean;
    } else if (height <= settings.seaLevel) {
      biomeType = BiomeType.Ocean;
    } else if (height <= settings.seaLevel + ((1 - settings.seaLevel) / 2)) {

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

    if (height <= settings.seaLevel / 2) {
      geographyType = GeographyType.DeepOcean;
    } else if (height <= settings.seaLevel) {
      geographyType = GeographyType.Ocean;
    } else if (height <= settings.seaLevel + ((1 - settings.seaLevel) / 2)) {
      geographyType = GeographyType.LowAltitude;
    } else {
      geographyType = GeographyType.HighAltitude;
    }

    return geographyType;
  }

  public BiomeData queryBiom(int x, int y, int size) {
    Vector2 location = new Vector2(x, y);
    float[,] heights = Noise.generateNoiseMap(size, size, settings.geography, location);
    float[,] temps = Noise.generateNoiseMap(size, size, settings.temperature, location);
    float[,] rainfalls = Noise.generateNoiseMap(size, size, settings.rainfall, location);

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

