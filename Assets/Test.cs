// using UnityEngine;
// using System;
// using System.IO;
// using UnityEngine.Experimental.TerrainAPI;

// public class Test : MonoBehaviour {
//   private Terrain terrain;
//   public NoiseSettings geography;
//   // [SerializeField] private FillAddressMode m_FillAddressMode = FillAddressMode.Mirror;
//   // private Material m_CrossBlendMaterial;
//   private float[,] temp;

//   // private enum FillAddressMode {
//   //   Clamp = 0,
//   //   Mirror = 1
//   // }

//   // private class TerrainNeighborInfo {
//   //   public TerrainData terrainData;
//   //   public Texture texture;
//   //   public float offset;
//   // }

//   // Start is called before the first frame update
//   void Start() {

//     terrain = GetComponent<Terrain>();
//     float[,] randomHeights = Noise.generateNoiseMap(terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight, geography, new Vector2());
//     terrain.terrainData.SetHeights(0, 0, randomHeights);
//     temp = randomHeights;

//     Vector3 size = terrain.terrainData.size;

//     CreateNeighbor(terrain, terrain.transform.position + Vector3.back * size.z);
//   }

//   // // Update is called once per frame
//   // void Update() {

//   // }

//   Terrain CreateNeighbor(Terrain parent, Vector3 position) {
//     string uniqueName = "Terrain_" + position.ToString();

//     if (null != GameObject.Find(uniqueName)) {
//       Debug.LogWarning("Already have a neighbor on that side");
//       return null;
//     }

//     TerrainData terrainData = new TerrainData();
//     terrainData.baseMapResolution = parent.terrainData.baseMapResolution;
//     terrainData.heightmapResolution = parent.terrainData.heightmapResolution;
//     terrainData.alphamapResolution = parent.terrainData.alphamapResolution;

//     Grassland grassland = new Grassland();

//     // if (parent.terrainData.terrainLayers != null && parent.terrainData.terrainLayers.Length > 0) {
//     //   var newarray = new TerrainLayer[1];
//     //   newarray[0] = parent.terrainData.terrainLayers[0];
//     //   terrainData.terrainLayers = newarray;
//     // }

//     terrainData.terrainLayers = grassland.generateLayers();

//     terrainData.SetDetailResolution(parent.terrainData.detailResolution, parent.terrainData.detailResolutionPerPatch);
//     terrainData.wavingGrassSpeed = parent.terrainData.wavingGrassSpeed;
//     terrainData.wavingGrassAmount = parent.terrainData.wavingGrassAmount;
//     terrainData.wavingGrassStrength = parent.terrainData.wavingGrassStrength;
//     terrainData.wavingGrassTint = parent.terrainData.wavingGrassTint;
//     terrainData.name = Guid.NewGuid().ToString();
//     terrainData.size = parent.terrainData.size;

//     GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
//     terrainGO.name = uniqueName;
//     terrainGO.transform.position = position;

//     Terrain terrainToReturn = terrainGO.GetComponent<Terrain>();
//     terrainToReturn.groupingID = parent.groupingID;
//     terrainToReturn.drawInstanced = parent.drawInstanced;
//     terrainToReturn.allowAutoConnect = parent.allowAutoConnect;
//     terrainToReturn.drawTreesAndFoliage = parent.drawTreesAndFoliage;
//     terrainToReturn.bakeLightProbesForTrees = parent.bakeLightProbesForTrees;
//     terrainToReturn.deringLightProbesForTrees = parent.deringLightProbesForTrees;
//     terrainToReturn.preserveTreePrototypeLayers = parent.preserveTreePrototypeLayers;
//     terrainToReturn.detailObjectDistance = parent.detailObjectDistance;
//     terrainToReturn.detailObjectDensity = parent.detailObjectDensity;
//     terrainToReturn.treeDistance = parent.treeDistance;
//     terrainToReturn.treeBillboardDistance = parent.treeBillboardDistance;
//     terrainToReturn.treeCrossFadeLength = parent.treeCrossFadeLength;
//     terrainToReturn.treeMaximumFullLODCount = parent.treeMaximumFullLODCount;


//     // FillHeightmapUsingNeighbors(terrainToReturn);

//     float[,] randomHeights = Noise.generateNoiseMap(terrainData.heightmapWidth, terrainData.heightmapHeight, geography, new Vector2(1, 0));
//     for (int y = 0; y < terrainData.heightmapHeight; y++) {
//       float h = temp[0, y];

//       randomHeights[terrainData.heightmapWidth - 1, y] = h;
//     }

//     terrainData.SetHeights(0, 0, randomHeights);

//     return terrainToReturn;
//   }

//   // private void SetTerrainSplatMap(Terrain terrain, Texture2D[] textures) {
//   //   var terrainData = terrain.terrainData;

//   //   // terrainData.terrainLayers[0];
//   // }

//   // private void FillHeightmapUsingNeighbors(Terrain terrain) {
//   //   TerrainUtility.AutoConnect();

//   //   Terrain[] nbrTerrains = new Terrain[4] { terrain.topNeighbor, terrain.bottomNeighbor, terrain.leftNeighbor, terrain.rightNeighbor };

//   //   // Position of the terrain must be lowest
//   //   Vector3 position = terrain.transform.position;
//   //   foreach (Terrain nbrTerrain in nbrTerrains) {
//   //     if (nbrTerrain)
//   //       position.y = Mathf.Min(position.y, nbrTerrain.transform.position.y);
//   //   }
//   //   terrain.transform.position = position;

//   //   TerrainNeighborInfo top = new TerrainNeighborInfo();
//   //   TerrainNeighborInfo bottom = new TerrainNeighborInfo();
//   //   TerrainNeighborInfo left = new TerrainNeighborInfo();
//   //   TerrainNeighborInfo right = new TerrainNeighborInfo();
//   //   TerrainNeighborInfo[] nbrInfos = new TerrainNeighborInfo[4] { top, bottom, left, right };

//   //   const float kNeightNormFactor = 2.0f;
//   //   for (int i = 0; i < 4; ++i) {
//   //     TerrainNeighborInfo nbrInfo = nbrInfos[i];
//   //     Terrain nbrTerrain = nbrTerrains[i];
//   //     if (nbrTerrain) {
//   //       nbrInfo.terrainData = nbrTerrain.terrainData;
//   //       if (nbrInfo.terrainData) {
//   //         nbrInfo.texture = nbrInfo.terrainData.heightmapTexture;
//   //         nbrInfo.offset = (nbrTerrain.transform.position.y - terrain.transform.position.y) / (nbrInfo.terrainData.size.y * kNeightNormFactor);
//   //       }
//   //     }
//   //   }

//   //   RenderTexture heightmap = terrain.terrainData.heightmapTexture;
//   //   Vector4 texCoordOffsetScale = new Vector4(-0.5f / heightmap.width, -0.5f / heightmap.height,
//   //       (float)heightmap.width / (heightmap.width - 1), (float)heightmap.height / (heightmap.height - 1));

//   //   Material crossBlendMat = GetOrCreateCrossBlendMaterial();
//   //   Vector4 slopeEnableFlags = new Vector4(bottom.texture ? 0.0f : 1.0f, top.texture ? 0.0f : 1.0f, left.texture ? 0.0f : 1.0f, right.texture ? 0.0f : 1.0f);
//   //   crossBlendMat.SetVector("_SlopeEnableFlags", slopeEnableFlags);
//   //   crossBlendMat.SetVector("_TexCoordOffsetScale", texCoordOffsetScale);
//   //   crossBlendMat.SetVector("_Offsets", new Vector4(bottom.offset, top.offset, left.offset, right.offset));
//   //   crossBlendMat.SetFloat("_AddressMode", (float)m_FillAddressMode);
//   //   crossBlendMat.SetTexture("_TopTex", top.texture);
//   //   crossBlendMat.SetTexture("_BottomTex", bottom.texture);
//   //   crossBlendMat.SetTexture("_LeftTex", left.texture);
//   //   crossBlendMat.SetTexture("_RightTex", right.texture);

//   //   Graphics.Blit(null, heightmap, crossBlendMat);

//   //   terrain.terrainData.DirtyHeightmapRegion(new RectInt(0, 0, heightmap.width, heightmap.height), TerrainHeightmapSyncControl.HeightAndLod);
//   // }

//   // private Material GetOrCreateCrossBlendMaterial() {
//   //   if (m_CrossBlendMaterial == null)
//   //     m_CrossBlendMaterial = new Material(Shader.Find("Hidden/TerrainEngine/CrossBlendNeighbors"));
//   //   return m_CrossBlendMaterial;
//   // }

// }
