Shader "Custom/Wine"
{
    Properties
    { 
        _Color("Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GridSize("GridSize", Int) = 4
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

            CBUFFER_START(UnityPerMaterial)
            int _GridSize; float4 _Color;
            StructuredBuffer<float4> _DataBuffer; // x: height yzw: normal
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 position : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = v.uv;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.position = TransformObjectToWorld(v.vertex.xyz);
                return o;
            }


            float4 frag(v2f i) : SV_Target
            {
                // Pixel Height & Normal
                int xl = (int)(i.uv.x / _GridSize), xr = min(xl + 1, _GridSize - 1),
                    yl = (int)(i.uv.y / _GridSize), yr = min(yl + 1, _GridSize - 1);
                float xf = i.uv.x * (_GridSize + 1) - (xl + 0.5), 
                      yf = i.uv.y * (_GridSize + 1) - (yl + 0.5),
                    height = smoothstep(smoothstep(_DataBuffer[yl * _GridSize + xl].x, _DataBuffer[yl * _GridSize + xr].x, xf),
                    smoothstep(_DataBuffer[yr * _GridSize + xl].x, _DataBuffer[yr * _GridSize + xr].x, xf), yf);
                float3 N = smoothstep(smoothstep(_DataBuffer[yl * _GridSize + xl].yzw, _DataBuffer[yl * _GridSize + xr].yzw, xf),
                    smoothstep(_DataBuffer[yr * _GridSize + xl].yzw, _DataBuffer[yr * _GridSize + xr].yzw, xf), yf);
                
                i.position += float3(0, height, 0);
                float4 albedo = _Color;
                float3 V = normalize(_WorldSpaceCameraPos - i.position);
                float3 L = normalize(_MainLightPosition.xyz); // Assume directional light
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