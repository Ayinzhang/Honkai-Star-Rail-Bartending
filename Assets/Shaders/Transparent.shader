Shader "Phong/Transparent"
{
    Properties
    {
        _NormalMap("Normal", 2D) = "bump" {}  
        _Color("Color", Color) = (0.8, 0.9, 1, 0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ÉùÃ÷ÊôÐÔ
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            float4 _Color;

            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
                float3 normal: NORMAL;
                float4 tangent: TANGENT;
            };

            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 position: TEXCOORD1;
                float3 tbn0: TEXCOORD2;
                float3 tbn1: TEXCOORD3;
                float3 tbn2: TEXCOORD4;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                o.position = TransformObjectToWorld(v.vertex.xyz);
                o.tbn0 = normalize(TransformObjectToWorldNormal(v.normal));
                o.tbn1 = normalize(TransformObjectToWorldDir(v.tangent.xyz));
                o.tbn2 = normalize(cross(o.tbn0, o.tbn1) * v.tangent.w);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 albedo = _Color;
                float3 V = normalize(_WorldSpaceCameraPos - i.position);
                float3 L = normalize(_MainLightPosition.xyz); // Assume directional ligh
                float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));
                float3 N = normalize(float3(dot(i.tbn0, normal), dot(i.tbn1, normal), dot(i.tbn2, normal)));
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