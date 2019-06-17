Shader "Custom/Terrain"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white"{}
		_Glossiness ("Smoothness", Range(0,1)) = 0.0
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Occlusion ("Occlusion", Float) = 0.0
		_BumpStength ("Bump Strength", Float) = 1
		stichFalloffA ("Stich Falloff A", Float) = 1
		stichFalloffB ("Stich Falloff B", Float) = 10
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		const static float epsilon = 1E-4;

		float minHeight;
		float maxHeight;
		float baseBlends[5];
		float baseStartHeights[5];
		float primaryUvScales[5];
		float secondaryUvScales[5];
		float textureIndices[5];
		float _BumpStength;
		float stichFalloffA;
		float stichFalloffB;
        fixed3 _SpecularColor;
        half _Glossiness;
		half _Occlusion;
		half _Metallic;



		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		float inverseLerp(float a, float b, float value) {
			return saturate( (value - a) / (b-a) );
		}

		float3 triplanar( float3 worldPos, float scale, float3 blendAxes, uint textureIndex ) {
			float3 scaledWorldPos = worldPos / scale;

			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;

			return xProjection + yProjection + zProjection;
		}

		float fallOff(float value) {
			float a = stichFalloffA;
			float b = stichFalloffB;

			return pow(value, a) / (pow(value, a) + pow((b - b * value), a));
		}


		// Include noise
		#include "noisePerlin.cginc"

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float3 worldNormal = WorldNormalVector (IN, o.Normal);
			float heightPercent = inverseLerp( minHeight, maxHeight, IN.worldPos.y );
			float3 blendAxes = abs( worldNormal );
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			float3 normalMap = float3(0.0, 0.0, 0.0);


 			// Generate a border patch mask. The mask falls off around the borders with isolated noise patches
            // that blend into other terrains
			float u = IN.uv_MainTex.x * 2.0f - 1.0f;
			float v = IN.uv_MainTex.y * 2.0f - 1.0f;
			float fallOffVal = clamp( fallOff( max( abs( u ), abs( v ) ) ), 0.0f, 1.0f );


			float noise = max( fallOffVal *
				(cnoise(IN.uv_MainTex * 4.0f) +
				cnoise(IN.uv_MainTex * 30.0f) +
				cnoise(IN.uv_MainTex * 80.0f) / 3.0f), 0.0f );

			float borderPathMask = 1; // clamp( fallOffVal + noise, 0.0f, 1.0f );

			float borderFalloff = 0.2f;
			float northFade = (1.0 - inverseLerp( 0 - epsilon, borderFalloff, IN.uv_MainTex.y )) * 0.5f;
			float eastFade = (inverseLerp( 1.0 - borderFalloff, 1.0, IN.uv_MainTex.x )) * 0.5f;
			float southFade = inverseLerp( 1.0 - borderFalloff, 1.0, IN.uv_MainTex.y ) * 0.5f;
			float westFade = (1.0 - inverseLerp( 0 - epsilon, borderFalloff, IN.uv_MainTex.x )) * 0.5f;


			// Set lighting properties
			o.Smoothness = _Glossiness;
			o.Metallic = _Metallic;

			// Primary Biome
			// Main Texture
			int mainBiomeindex = 0;
			int mainTextureIndex = textureIndices[0];
			float3 textureColor = triplanar(IN.worldPos, primaryUvScales[mainBiomeindex], blendAxes, mainTextureIndex);
			float3 normalColor = triplanar(IN.worldPos, primaryUvScales[mainBiomeindex], blendAxes, mainTextureIndex + 1 );

			normalMap = normalColor;
			o.Albedo = textureColor;

			// Secondary Texture
			float drawStrength = inverseLerp( -baseBlends[mainBiomeindex]/2 - epsilon, baseBlends[mainBiomeindex]/2,  heightPercent - baseStartHeights[mainBiomeindex] );
			textureColor = triplanar(IN.worldPos, secondaryUvScales[mainBiomeindex], blendAxes, mainTextureIndex + 2 );
			normalColor = triplanar(IN.worldPos, secondaryUvScales[mainBiomeindex], blendAxes, mainTextureIndex + 3 );

			normalMap = normalMap * (1 - drawStrength) + normalColor * drawStrength;
			o.Albedo = o.Albedo * (1 - drawStrength) + textureColor * drawStrength;


			// North Biome
			// Main texture
			int northBiomeindex = 1;
			int northTextureIndex = textureIndices[northBiomeindex];
			float3 northDiffuseMap1 = triplanar(IN.worldPos, primaryUvScales[northBiomeindex], blendAxes, northTextureIndex);
			float3 northNormalMap1 = triplanar(IN.worldPos, primaryUvScales[northBiomeindex], blendAxes, northTextureIndex + 1 );

			// Secondary texture
			drawStrength = inverseLerp( -baseBlends[ northBiomeindex ]/2 - epsilon, baseBlends[ northBiomeindex ]/2,  heightPercent - baseStartHeights[ northBiomeindex ] );
			float3 northDiffuseMap2 = triplanar(IN.worldPos, secondaryUvScales[ northBiomeindex ], blendAxes, northTextureIndex + 2 );
			float3 northNormalMap2 = triplanar(IN.worldPos, secondaryUvScales[ northBiomeindex ], blendAxes, northTextureIndex + 3 );

			float3 northNormalCombined = northNormalMap1 * (1 - drawStrength) + northNormalMap2 * drawStrength;
			float3 northDiffuseCombined = northDiffuseMap1 * (1 - drawStrength) + northDiffuseMap2 * drawStrength;

			// Blends
			normalMap = lerp( normalMap, northNormalCombined, northFade * borderPathMask );
			o.Albedo = lerp( o.Albedo, northDiffuseCombined, northFade * borderPathMask );



			// East Biome
			// Main texture
			int eastBiomeindex = 2;
			int eastTextureIndex = textureIndices[eastBiomeindex];
			float3 eastDiffuseMap1 = triplanar(IN.worldPos, primaryUvScales[eastBiomeindex], blendAxes, eastTextureIndex);
			float3 eastNormalMap1 = triplanar(IN.worldPos, primaryUvScales[eastBiomeindex], blendAxes, eastTextureIndex + 1 );

			// Secondary texture
			drawStrength = inverseLerp( -baseBlends[ eastBiomeindex ]/2 - epsilon, baseBlends[ eastBiomeindex ]/2,  heightPercent - baseStartHeights[ eastBiomeindex ] );
			float3 eastDiffuseMap2 = triplanar(IN.worldPos, secondaryUvScales[ eastBiomeindex ], blendAxes, eastTextureIndex + 2 );
			float3 eastNormalMap2 = triplanar(IN.worldPos, secondaryUvScales[ eastBiomeindex ], blendAxes, eastTextureIndex + 3 );

			float3 eastNormalCombined = eastNormalMap1 * (1 - drawStrength) + eastNormalMap2 * drawStrength;
			float3 eastDiffuseCombined = eastDiffuseMap1 * (1 - drawStrength) + eastDiffuseMap2 * drawStrength;

			// Blends
			normalMap = lerp( normalMap, eastNormalCombined, eastFade * borderPathMask );
			o.Albedo = lerp( o.Albedo, eastDiffuseCombined, eastFade * borderPathMask );




			// South Biome
			// Main texture
			int southBiomeindex = 3;
			int southTextureIndex = textureIndices[southBiomeindex];
			float3 southDiffuseMap1 = triplanar(IN.worldPos, primaryUvScales[southBiomeindex], blendAxes, southTextureIndex);
			float3 southNormalMap1 = triplanar(IN.worldPos, primaryUvScales[southBiomeindex], blendAxes, southTextureIndex + 1 );

			// Secondary texture
			drawStrength = inverseLerp( -baseBlends[ southBiomeindex ]/2 - epsilon, baseBlends[ southBiomeindex ]/2,  heightPercent - baseStartHeights[ southBiomeindex ] );
			float3 southDiffuseMap2 = triplanar(IN.worldPos, secondaryUvScales[ southBiomeindex ], blendAxes, southTextureIndex + 2 );
			float3 southNormalMap2 = triplanar(IN.worldPos, secondaryUvScales[ southBiomeindex ], blendAxes, southTextureIndex + 3 );

			float3 southNormalCombined = southNormalMap1 * (1 - drawStrength) + southNormalMap2 * drawStrength;
			float3 southDiffuseCombined = southDiffuseMap1 * (1 - drawStrength) + southDiffuseMap2 * drawStrength;

			// Blends
			normalMap = lerp( normalMap, southNormalCombined, southFade  * borderPathMask );
			o.Albedo = lerp( o.Albedo, southDiffuseCombined, southFade   * borderPathMask );




			// West Biome
			// Main texture
			int westBiomeindex = 4;
			int westTextureIndex = textureIndices[westBiomeindex];
			float3 westDiffuseMap1 = triplanar(IN.worldPos, primaryUvScales[westBiomeindex], blendAxes, westTextureIndex);
			float3 westNormalMap1 = triplanar(IN.worldPos, primaryUvScales[westBiomeindex], blendAxes, westTextureIndex + 1 );

			// Secondary texture
			drawStrength = inverseLerp( -baseBlends[ westBiomeindex ]/2 - epsilon, baseBlends[ westBiomeindex ]/2,  heightPercent - baseStartHeights[ westBiomeindex ] );
			float3 westDiffuseMap2 = triplanar(IN.worldPos, secondaryUvScales[ westBiomeindex ], blendAxes, westTextureIndex + 2 );
			float3 westNormalMap2 = triplanar(IN.worldPos, secondaryUvScales[ westBiomeindex ], blendAxes, westTextureIndex + 3 );

			float3 westNormalCombined = westNormalMap1 * (1 - drawStrength) + westNormalMap2 * drawStrength;
			float3 westDiffuseCombined = westDiffuseMap1 * (1 - drawStrength) + westDiffuseMap2 * drawStrength;

			// Blends
			normalMap = lerp( normalMap, westNormalCombined, westFade * borderPathMask);
			o.Albedo = lerp( o.Albedo, westDiffuseCombined, westFade * borderPathMask);

			float4 normal = lerp(float4(0.5, 0.5, 1, 1), float4(normalMap, 1), _BumpStength);
			o.Normal = UnpackNormal(normal);

			// o.Albedo = float3(borderPathMask, borderPathMask, borderPathMask);
		}
		ENDCG
	}

    FallBack "Diffuse"
}
