Shader "Custom/Terrain"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white"{}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.5
		_Occlusion ("Occlusion", Float) = 0.5
		_BumpStength ("Bump Strength", Float) = 1
		stichTiling ("Stich Tiling", Float) = 16
		stichFalloffA ("Stich Falloff A", Float) = 2.8
		stichFalloffB ("Stich Falloff B", Float) = 4
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
		const static int maxLayerCount = 20;
        const static int numChunkTextures = 4;
		const static float epsilon = 1E-4;

		sampler2D _MainTex;
		float minHeight;
		float maxHeight;
		float baseBlends[maxLayerCount];
		float baseStartHeights[maxLayerCount];
		float baseColorStrength[maxLayerCount];
		float baseTextureScales[maxLayerCount];
		float _BumpStength;
		float stichTiling;
		float stichFalloffA;
		float stichFalloffB;
		float3 baseColors[maxLayerCount];
        fixed3 _SpecularColor;
        uint layerCount;
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

			for (uint i = 0; i < layerCount; i++ ) {
				float drawStrength = inverseLerp( -baseBlends[i]/2 - epsilon, baseBlends[i]/2,  heightPercent - baseStartHeights[i] );
				float3 baseColor = baseColors[i] * baseColorStrength[i];
				float3 textureColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColorStrength[i]);
				float3 normalColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i + layerCount );

				normalMap = normalMap * (1 - drawStrength) + normalColor * drawStrength;
				o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
			}

            o.Smoothness = _Glossiness;
			o.Metallic = _Metallic;

            // Generate a border patch mask. The mask falls off around the borders with isolated noise patches
            // that blend into other terrains
			float u = IN.uv_MainTex.x * 2.0f - 1.0f;
			float v = IN.uv_MainTex.y * 2.0f - 1.0f;
			float fallOffVal = clamp( fallOff( max( abs( u ), abs( v ) ) ), 0.0f, 1.0f );

			float noise = max( fallOffVal *
				(cnoise(IN.uv_MainTex * 4.0f) +
				cnoise(IN.uv_MainTex * 30.0f) +
				cnoise(IN.uv_MainTex * 80.0f) / 3.0f), 0.0f );

			float borderPathMask = clamp( fallOffVal + noise, 0.0f, 1.0f );

            // Stich the borders by the border patch mask
			o.Albedo = lerp( o.Albedo.rgb, tex2D (_MainTex, IN.uv_MainTex * stichTiling ).rgb, borderPathMask );
			normalMap = lerp( normalMap, tex2D (_BumpMap, IN.uv_MainTex * stichTiling ).rgb, borderPathMask );

			float4 normal = lerp(float4(0.5, 0.5, 1, 1), float4(normalMap, 1), _BumpStength);
			o.Normal = UnpackNormal(normal);
		}
		ENDCG
	}

    FallBack "Diffuse"
}
