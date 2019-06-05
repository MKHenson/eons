Shader "Custom/MultiPassTest" {
Properties
{
    _MainTex ("Texture", 2D) = "white" {}
}
SubShader
{
    Pass
    {
        Cull Back ZWrite On ZTest Always
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD;
        };

        struct v2f
        {
            float2 depth : DEPTH;
            float4 vertex : SV_POSITION;

            // we'll output world space normal as one of regular ("texcoord") interpolators
            half3 worldNormal : TEXCOORD0;
            float2 uv : TEXCOORD1;
        };

        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.depth = -UnityObjectToViewPos(v.vertex).z * 200 * _ProjectionParams.w;

            // UnityCG.cginc file contains function to transform
            // normal from object to world space, use that
            o.worldNormal = UnityObjectToWorldNormal(v.normal);
            o.uv = v.uv;

            return o;
        }

        sampler2D _MainTex;

        fixed4 frag (v2f i) : SV_Target
        {
            fixed3 baseColor = tex2D(_MainTex, i.uv).rgb;

            float c=1-i.depth;
            fixed4 col = fixed4(c,c,c,1);

            // normal is a 3D vector with xyz components; in -1..1
            // range. To display it as color, bring the range into 0..1
            // and put into red, green, blue components
            col.rgb = (i.worldNormal * 0.5 + 0.5) * baseColor;

            return col;
        }
        ENDCG
    }

    GrabPass { "_GrabTexture" }

    Pass
    {
        Cull Front ZWrite On ZTest Always
        // BlendOp RevSub
        // Blend One One
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float4 grabUv : TEXCOORD0;
        };

        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.grabUv = ComputeGrabScreenPos (o.vertex);
            return o;
        }

        sampler2D _GrabTexture;

        fixed4 frag (v2f i) : SV_Target
        {
            fixed4 prevPassColor = tex2Dproj(_GrabTexture, i.grabUv);
            return prevPassColor;
        }
        ENDCG
    }
}
}