Shader "Phong/Opaque"
{
    Properties
    {
        _BaseMap("Base", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        LOD 200

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

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
                float3 position : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = v.uv;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.position = TransformObjectToWorld(v.vertex.xyz);
                o.normal = TransformObjectToWorldNormal(v.normal);
                return o;
            }


            float4 frag(v2f i) : SV_Target
            {
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float3 N = normalize(i.normal);
                float3 V = normalize(_WorldSpaceCameraPos - i.position);
                float3 L = normalize(_MainLightPosition.xyz); // Assume directional ligh
                float3 R = reflect(-L, N);

                float ambientStrength = 0.5, diffuseStrength = 0.4, specularStrength = 0.1;

                float nl = max(1e-6, dot(N, L));
                float spec = pow(max(1e-6, dot(V, R)), 16);

                float3 ambient = ambientStrength * albedo;
                float3 diffuse = diffuseStrength * albedo * nl;
                float3 specular = specularStrength * spec;

                float3 col = ambient + diffuse + specular;
                return float4(col, albedo.a);
            }
            ENDHLSL
        }
    }
}