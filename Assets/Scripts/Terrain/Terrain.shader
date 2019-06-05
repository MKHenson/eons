Shader "Custom/Terrain"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white"{}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
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

		sampler2D _MainTex;
		sampler2D _BumpMap;
		half _Glossiness;
		half _Occlusion;
		half _Metallic;
		float _BumpStength;
        fixed3 _SpecularColor;
		float stichTiling;
		float stichFalloffA;
		float stichFalloffB;


		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		struct Input
		{
			float2 uv_BumpMap;
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

		// half3 blend_rnm(half3 n1, half3 n2)
        // {
        //     n1.z += 1;
        //     n2.xy = -n2.xy;

        //     return n1 * dot(n1, n2) / n1.z - n2;
		// }

		// float3 WorldToTangentNormalVector(Input IN, float3 normal) {
        //     float3 t2w0 = WorldNormalVector(IN, float3(1,0,0));
        //     float3 t2w1 = WorldNormalVector(IN, float3(0,1,0));
        //     float3 t2w2 = WorldNormalVector(IN, float3(0,0,1));
        //     float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
        //     return normalize(mul(t2w, normal));
		// }

		// float3 triplanarNormal(Input IN, float3 worldPos, float scale, float3 blendAxes, uint textureIndex ) {
		// 	// work around bug where IN.worldNormal is always (0,0,0)!
		// 	IN.worldNormal = WorldNormalVector(IN, float3(0,0,1));

		// 	// calculate triplanar blend
	    //     half3 triblend = saturate(pow(IN.worldNormal, 4));
		// 	triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);

		// 	half3 axisSign = IN.worldNormal < 0 ? -1 : 1;
		// 	float3 scaledWorldPos = worldPos / scale;

		// 	float2 uvX = scaledWorldPos.zy;
		// 	float2 uvY = scaledWorldPos.xz;
		// 	float2 uvZ = scaledWorldPos.xy;

		// 	half3 tnormalX = UnpackNormal( UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(uvX, textureIndex)) );
		// 	half3 tnormalY = UnpackNormal( UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(uvY, textureIndex)) );
		// 	half3 tnormalZ = UnpackNormal( UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(uvZ, textureIndex)) );

		// 	half3 absVertNormal = abs(IN.worldNormal);

        //     // swizzle world normals to match tangent space and apply reoriented normal mapping blend
        //     tnormalX = blend_rnm(half3(IN.worldNormal.zy, absVertNormal.x), tnormalX);
        //     tnormalY = blend_rnm(half3(IN.worldNormal.xz, absVertNormal.y), tnormalY);
        //     tnormalZ = blend_rnm(half3(IN.worldNormal.xy, absVertNormal.z), tnormalZ);

        //     // apply world space sign to tangent space Z
        //     tnormalX.z *= axisSign.x;
        //     tnormalY.z *= axisSign.y;
        //     tnormalZ.z *= axisSign.z;

        //     // sizzle tangent normals to match world normal and blend together
        //     half3 worldNormal = normalize(
        //         tnormalX.zyx * triblend.x +
        //         tnormalY.xzy * triblend.y +
        //         tnormalZ.xyz * triblend.z
		// 	);

		// 	// convert world space normals into tangent normals
		// 	return WorldToTangentNormalVector(IN, worldNormal);
		// }

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


			// float4 normal = lerp(float4(0.5, 0.5, 1, 1), tex2D(_BumpMap, IN.uv_BumpMap), _BumpStength);
			// o.Normal = UnpackNormal(normal);

			// o.Normal = normalize( UnpackNormal (tex2D (_BumpMap, IN.uv_MainTex)) * _BumpStength );
			// float3 worldNormal = WorldNormalVector (IN, o.Normal);

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



			float u = IN.uv_MainTex.x * 2.0f - 1.0f;
			float v = IN.uv_MainTex.y * 2.0f - 1.0f;
			float fallOffVal = clamp( fallOff(max(abs(u), abs(v))), 0.0f, 1.0f );
			float fallOffVal2 = fallOff(max(abs(u), abs(v)));

			float noise = max( fallOffVal *
				(cnoise(IN.uv_MainTex * 4.0f) +
				cnoise(IN.uv_MainTex * 30.0f) +
				cnoise(IN.uv_MainTex * 80.0f) / 3.0f), 0.0f );

			float mixValue = clamp(fallOffVal + noise, 0.0f, 1.0f);

			// mixValue = clamp(mixValue * mixValue, 0.0f, 1.0f);;

			o.Albedo = lerp( o.Albedo.rgb, tex2D (_MainTex, IN.uv_MainTex * stichTiling ).rgb, mixValue );
			normalMap = lerp( normalMap, tex2D (_BumpMap, IN.uv_MainTex * stichTiling ).rgb, mixValue );

			// fixed3 normal = UnpackNormal(float4(normalMap, 1.0));
			// normal.z = normal.z * _BumpStength;
			// o.Normal = normalize(normal);
            o.Emission = half3(0,0,0);


			// float3 scaledWorldPos = IN.worldPos / baseTextureScales[3];
			// float4 normal = normalMap; // lerp(float4(0.5, 0.5, 1, 1), UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, 8)), _BumpStength);
			// o.Normal = UnpackNormal(float4(normalMap, 1));

			float4 normal = lerp(float4(0.5, 0.5, 1, 1), float4(normalMap, 1), _BumpStength);
			o.Normal = UnpackNormal(normal);

			// float3 scaledWorldPos = IN.worldPos / baseTextureScales[3];
			// float3 normalColor = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, 8));
			// float4 normal = lerp(float4(0.5, 0.5, 1, 1), float4(normalColor, 1.0), _BumpStength);
			// o.Normal = UnpackNormal(normal);

			// o.Albedo =  tex2D(_BumpMap, IN.uv_BumpMap);
		}
		ENDCG
	}
		FallBack "Diffuse"
}
