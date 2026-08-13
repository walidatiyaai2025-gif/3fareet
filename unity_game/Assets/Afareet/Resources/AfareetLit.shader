Shader "Afareet/RuntimeLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission", Color) = (0,0,0,0)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 150
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 vertex : SV_POSITION; float3 normal : TEXCOORD0; float3 worldPos : TEXCOORD1; };
            fixed4 _Color;
            fixed4 _EmissionColor;
            half _Metallic;
            half _Glossiness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float diffuse = saturate(dot(n, l)) * .72 + .28;
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfDir = normalize(l + viewDir);
                float specular = pow(saturate(dot(n, halfDir)), lerp(8, 96, _Glossiness)) * _Metallic;
                float3 color = _Color.rgb * diffuse * _LightColor0.rgb + specular + _EmissionColor.rgb;
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}
