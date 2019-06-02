Shader "Custom/Terrain"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.5
		_Occlusion ("Occlusion", Float) = 0.5
		_BumpStength ("Bump Strength", Float) = 1
        // _SpecularColor("Specular", Color) = (0.2,0.2,0.2)
		// _Factor("Factor", Vector) = (1,1,1,0)
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

		sampler2D _MainTex;

		float minHeight;
		float maxHeight;

		const static int maxLayerCount = 8;
		const static float epsilon = 1E-4;

		uint layerCount;
		float3 baseColors[maxLayerCount];
		float baseBlends[maxLayerCount];
		float baseStartHeights[maxLayerCount];
		float baseColorStrength[maxLayerCount];
		float baseTextureScales[maxLayerCount];

		sampler2D _BumpMap;
		half _Glossiness;
		half _Occlusion;
		half _Metallic;
		float _BumpStength;
        fixed3 _SpecularColor;
		float testScale;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		struct Input
		{
			float2 uv_BumpMap;
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

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Normal = normalize( UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap)) * _BumpStength );
			float3 worldNormal = WorldNormalVector (IN, o.Normal);

			float heightPercent = inverseLerp( minHeight, maxHeight, IN.worldPos.y );
			float3 blendAxes = abs(worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			float3 normalMap = float3(0.0, 0.0, 0.0);

			for (uint i = 0; i < layerCount; i++ ) {
				float drawStrength = inverseLerp( -baseBlends[i]/2 - epsilon, baseBlends[i]/2,  heightPercent - baseStartHeights[i] );
				float3 baseColor = baseColors[i] * baseColorStrength[i];
				float3 textureColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColorStrength[i]);
				float3 normalColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i + layerCount ) * (1 - baseColorStrength[i]);

				normalMap = normalMap * (1 - drawStrength) + normalColor * drawStrength;
				o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
			}

            // o.Specular = _SpecularColor;

            o.Smoothness = _Glossiness;
			o.Metallic = _Metallic;

			fixed3 normal = UnpackNormal(float4(normalMap, 1.0));
			normal.z = normal.z * _BumpStength;
			o.Normal = normalize(normal);

			// o.Occlusion = _Occlusion;
            o.Emission = half3(0,0,0);
		}
		ENDCG
	}
		FallBack "Diffuse"
}
