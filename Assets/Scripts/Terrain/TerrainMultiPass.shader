// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Terrain"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white"{}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		testScale("Scale", Float) = 1
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.5
		_Occlusion ("Occlusion", Float) = 0.5
		_BumpStength ("Bump Strength", Float) = 1
		_BumpOffset ("Bump Offset", Float) = 512
		stichTiling ("Stich Tiling", Float) = 16
		stichFalloffA ("Stich Falloff A", Float) = 2.8
		stichFalloffB ("Stich Falloff B", Float) = 4
	}
	SubShader
	{
		Pass
		{
			Cull Back ZWrite On ZTest LEqual
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			const static int maxLayerCount = 8;
			const static float epsilon = 1E-4;
			UNITY_DECLARE_TEX2DARRAY(baseTextures);
			float minHeight;
			float maxHeight;
			uint layerCount;
			float3 baseColors[maxLayerCount];
			float baseBlends[maxLayerCount];
			float baseStartHeights[maxLayerCount];
			float baseColorStrength[maxLayerCount];
			float baseTextureScales[maxLayerCount];
			sampler2D _MainTex;
			float stichTiling;
			float stichFalloffA;
			float stichFalloffB;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				return o;
			}

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

			fixed4 frag (v2f input) : SV_Target
			{
				fixed3 finalColor = 0.0f; // tex2D(_MainTex, input.uv).rgb;

				float3 worldNormal = input.worldNormal;

				float heightPercent = inverseLerp( minHeight, maxHeight, input.worldPos.y );
				float3 blendAxes = abs(worldNormal);
				blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

				for (uint i = 0; i < layerCount; i++ ) {
					float drawStrength = inverseLerp( -baseBlends[i]/2 - epsilon, baseBlends[i]/2,  heightPercent - baseStartHeights[i] );
					float3 baseColor = baseColors[i] * baseColorStrength[i];
					float3 textureColor = triplanar( input.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColorStrength[i]);
					finalColor = finalColor * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
				}

				float u = input.uv.x * 2.0f - 1.0f;
				float v = input.uv.y * 2.0f - 1.0f;
				float fallOffVal = clamp( fallOff(max(abs(u), abs(v))), 0.0f, 1.0f );
				float fallOffVal2 = fallOff(max(abs(u), abs(v)));

				float noise = max( fallOffVal *
					(cnoise(input.uv * 4.0f) +
					cnoise(input.uv * 30.0f) +
					cnoise(input.uv * 80.0f) / 3.0f), 0.0f );

				float mixValue = clamp(fallOffVal + noise, 0.0f, 1.0f);

				finalColor = lerp( finalColor, tex2D (_MainTex, input.uv * stichTiling ).rgb, mixValue );

				// fixed3 baseColor = tex2D(_MainTex, i.uv).rgb;
				fixed4 col = fixed4(finalColor,1);
				return col;
			}
			ENDCG
		}

		GrabPass { "_GrabTexture" }

		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0



		// float minHeight;
		// float maxHeight;

		// const static int maxLayerCount = 8;
		// const static float epsilon = 1E-4;

		// uint layerCount;
		// float3 baseColors[maxLayerCount];
		// float baseBlends[maxLayerCount];
		// float baseStartHeights[maxLayerCount];
		// float baseColorStrength[maxLayerCount];
		// float baseTextureScales[maxLayerCount];

		sampler2D _GrabTexture;
		sampler2D _MainTex;
		sampler2D _BumpMap;
		half _Glossiness;
		half _Occlusion;
		half _Metallic;
		float _BumpStength;
		float _BumpOffset;
        fixed3 _SpecularColor;
		float testScale;
		float stichTiling;
		float stichFalloffA;
		float stichFalloffB;


		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float3 worldNormal;
			float4 grabUv;
			INTERNAL_DATA
		};

		#include "UnityCG.cginc"

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			float4 vertex = UnityObjectToClipPos(v.vertex);
            o.grabUv = ComputeGrabScreenPos (vertex);
      	}


		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		// float inverseLerp(float a, float b, float value) {
		// 	return saturate( (value - a) / (b-a) );
		// }

		// float3 triplanar( float3 worldPos, float scale, float3 blendAxes, uint textureIndex ) {
		// 	float3 scaledWorldPos = worldPos / scale;

		// 	float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
		// 	float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
		// 	float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;

		// 	return xProjection + yProjection + zProjection;
		// }

		// float fallOff(float value) {
		// 	float a = stichFalloffA;
		// 	float b = stichFalloffB;

		// 	return pow(value, a) / (pow(value, a) + pow((b - b * value), a));
		// }

		// Include noise
		// #include "noisePerlin.cginc"

		float3 Unity_NormalFromTexture_float( float4 uv, float Offset, float Strength )
		{
			// Offset = pow(Offset, 3) * 0.1;
			// float4 offsetU = float4(uv.x + Offset, uv.y, uv.z, uv.w);
			// float4 offsetV = float4(uv.x, uv.y + Offset, uv.z, uv.w);
			// float normalSample = tex2Dproj(_GrabTexture, uv).rgb; // Texture.Sample(Sampler, UV);
			// float uSample = tex2Dproj(_GrabTexture, offsetU).rgb; // Texture.Sample(Sampler, offsetU);
			// float vSample = tex2Dproj(_GrabTexture, offsetV).rgb; // Texture.Sample(Sampler, offsetV);
			// float3 va = float3(1, 0, (uSample - normalSample) * Strength);
			// float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
			// return normalize(cross(va, vb));


			const float2 size = {2.0 * Strength,0.0};
			const float3 off = {-1.0,0.0,1.0};
			const float2 nTex = {Offset, Offset};

			float4 color = tex2Dproj(_GrabTexture, float4(uv.xy, 0, uv.w ));

			float2 offxy = {off.x/nTex.x , off.y/nTex.y};
			float2 offzy = {off.z/nTex.x , off.y/nTex.y};
			float2 offyx = {off.y/nTex.x , off.x/nTex.y};
			float2 offyz = {off.y/nTex.x , off.z/nTex.y};

			float s11 = color.x;
			float s01 = tex2Dproj(_GrabTexture, float4(uv.xy + offxy, uv.z, uv.w) ).x;
			float s21 = tex2Dproj(_GrabTexture, float4(uv.xy + offzy, uv.z, uv.w) ).x;
			float s10 = tex2Dproj(_GrabTexture, float4(uv.xy + offyx, uv.z, uv.w) ).x;
			float s12 = tex2Dproj(_GrabTexture, float4(uv.xy + offyz, uv.z, uv.w) ).x;
			float3 va = {size.x, size.y, s21-s01};
			float3 vb = {size.y, size.x, s12-s10};
			va = normalize(va);
			vb = normalize(vb);
			float3 bump = {( cross(va,vb)) / 2 + 0.5};
			return bump;
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// // o.Normal = normalize( UnpackNormal (tex2D (_BumpMap, IN.uv_MainTex)) * _BumpStength );
			// float3 worldNormal = WorldNormalVector (IN, o.Normal);

			// float heightPercent = inverseLerp( minHeight, maxHeight, IN.worldPos.y );
			// float3 blendAxes = abs(worldNormal);
			// blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			// float3 normalMap = float3(0.0, 0.0, 0.0);

			// for (uint i = 0; i < layerCount; i++ ) {
			// 	float drawStrength = inverseLerp( -baseBlends[i]/2 - epsilon, baseBlends[i]/2,  heightPercent - baseStartHeights[i] );
			// 	float3 baseColor = baseColors[i] * baseColorStrength[i];
			// 	float3 textureColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColorStrength[i]);
			// 	float3 normalColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i + layerCount );

			// 	normalMap = normalMap * (1 - drawStrength) + normalColor * drawStrength;
			// 	o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
			// }


			o.Albedo = tex2Dproj(_GrabTexture, IN.grabUv).rgb;
            o.Smoothness = _Glossiness;
			o.Metallic = _Metallic;

			float3 normalMap = Unity_NormalFromTexture_float( IN.grabUv, _BumpOffset, _BumpStength );


			o.Normal = normalMap;

			// float u = IN.uv_MainTex.x * 2.0f - 1.0f;
			// float v = IN.uv_MainTex.y * 2.0f - 1.0f;
			// float fallOffVal = clamp( fallOff(max(abs(u), abs(v))), 0.0f, 1.0f );
			// float fallOffVal2 = fallOff(max(abs(u), abs(v)));

			// float noise = max( fallOffVal *
			// 	(cnoise(IN.uv_MainTex * 4.0f) +
			// 	cnoise(IN.uv_MainTex * 30.0f) +
			// 	cnoise(IN.uv_MainTex * 80.0f) / 3.0f), 0.0f );

			// float mixValue = clamp(fallOffVal + noise, 0.0f, 1.0f);

			// mixValue = clamp(mixValue * mixValue, 0.0f, 1.0f);;

			// o.Albedo = lerp( o.Albedo.rgb, tex2D (_MainTex, IN.uv_MainTex * stichTiling ).rgb, mixValue );
			// normalMap = lerp( normalMap, tex2D (_BumpMap, IN.uv_MainTex * stichTiling ).rgb, mixValue );

			// fixed3 normal = UnpackNormal(float4(normalMap, 1.0));
			// normal.z = normal.z * _BumpStength;
			// o.Normal = normalize(normal);

            // o.Emission = half3(0,0,0);

			// o.Albedo = normalMap;

		}
		ENDCG
	}
		FallBack "Diffuse"
}
