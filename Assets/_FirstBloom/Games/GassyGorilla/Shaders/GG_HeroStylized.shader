Shader "FirstBloom/GassyGorilla/HeroStylized"
{
    Properties
    {
        _MainTex ("Painted Albedo", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShadowTint ("Shadow Tint", Color) = (0.62,0.5,0.4,1)
        _KeyTint ("Key Tint", Color) = (1.08,1,0.88,1)
        _RimColor ("Rim Color", Color) = (0.62,0.96,0.74,1)
        _RimStrength ("Rim Strength", Range(0,1)) = 0.28
        _RimPower ("Rim Power", Range(0.5,6)) = 2.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ShadowTint;
            fixed4 _KeyTint;
            fixed4 _RimColor;
            half _RimStrength;
            half _RimPower;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                half3 viewDirection : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert(appdata input)
            {
                v2f output;
                float3 worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.viewDirection = UnityWorldSpaceViewDir(worldPosition);
                UNITY_TRANSFER_FOG(output, output.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                half3 normal = normalize(input.worldNormal);
                half3 viewDirection = normalize(input.viewDirection);
                half3 keyDirection = normalize(half3(-0.35h, 0.7h, -0.62h));
                half halfLambert = dot(normal, keyDirection) * 0.5h + 0.5h;
                half keyAmount = smoothstep(0.12h, 0.94h, halfLambert);
                fixed3 painted = tex2D(_MainTex, input.uv).rgb * _Color.rgb;
                fixed3 lighting = lerp(_ShadowTint.rgb, _KeyTint.rgb, keyAmount);
                half rim = pow(1.0h - saturate(dot(normal, viewDirection)), _RimPower) * _RimStrength;
                fixed4 color = fixed4(painted * lighting + _RimColor.rgb * rim, 1.0h);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Texture"
}
