Shader "Custom/Wine"
{
    Properties
    { 
        _Color("Color", Color) = (1, 1, 1, 1)
        _BubbleMap("Bubble", 2D) = "black" {}
        _BubbleSV("BubbleSV", Vector) = (1, 1, 0, 1)
        _NoiseMap("Noise", 2D) = "black" {}
        _NoiseSV("NoiseSV", Vector) = (1, 1, 1, 1)
        _Height("Height", Range(-0.3, 0.2)) = 0
        _BlendFrac("BlendFrac", Range(0, 0.5)) = 0.1
        _RefractionFrac("RefractionFrac", Range(0, 0.1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define LAYER_NUM 3
            TEXTURE2D(_BubbleMap); SAMPLER(sampler_BubbleMap);
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);
            TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
            CBUFFER_START(UnityPerMaterial)
            int _LayerCnt, _GridSize; float _Height, _BlendFrac, _RefractionFrac; float4 _Color, _BubbleSV, _NoiseSV; 
            float4 _LayerCols[LAYER_NUM]; StructuredBuffer<float4> _DataBuffer; // x: height yzw: normal
            CBUFFER_END

            float CosStep(float a, float b, float t)
            {
                float f = (1 - cos(t * PI)) * 0.5;
                return a * (1 - f) + b * f;
            }
                        
            float3 CosStep(float3 a, float3 b, float t)
            {
                float ft = t * PI;
                float f = (1 - cos(t * PI)) * 0.5;
                return a * (1 - f) + b * f;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 position : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float4 screenpos : TEXCOORD3;
                float3 tbn0 : TEXCOORD4;
                float3 tbn1 : TEXCOORD5;
                float3 tbn2 : TEXCOORD6;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = v.uv;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.position = TransformObjectToWorld(v.vertex.xyz);
                o.screenpos = ComputeScreenPos(o.vertex);

                float3 normalWS = o.normal = TransformObjectToWorldNormal(v.normal);
                float3 tangentWS = TransformObjectToWorldDir(v.tangent.xyz);
                float3 bitangentWS = normalize(cross(normalWS, tangentWS) * v.tangent.w);
            
                o.tbn0 = float3(tangentWS.x, bitangentWS.x, normalWS.x);
                o.tbn1 = float3(tangentWS.y, bitangentWS.y, normalWS.y);
                o.tbn2 = float3(tangentWS.z, bitangentWS.z, normalWS.z);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            { 
                float4 albedo = _Color;
                float h = (i.position.y + 0.3) * 2 * LAYER_NUM - 0.5,
                f = saturate((frac(h) - 0.5 + _BlendFrac) / (2 * _BlendFrac));
                int l = max(0, floor(h)), r = min(_LayerCnt - 1, ceil(h));
                albedo = lerp(_LayerCols[l], _LayerCols[r], f * (_NoiseSV.x * SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, i.uv + _Time.x * _NoiseSV.zw).a + 1 - 0.5 * _NoiseSV.x));
                // Pixel Height & Normal
                int xl = floor(i.uv.x * _GridSize), xr = xl + 1,
                    yl = floor(i.uv.y * _GridSize), yr = yl + 1;
                float xf = frac(i.uv.x * _GridSize), yf = frac(i.uv.y * _GridSize),
                    height = CosStep(CosStep(_DataBuffer[yl * _GridSize + xl].x, _DataBuffer[yl * _GridSize + xr].x, xf),
                    CosStep(_DataBuffer[yr * _GridSize + xl].x, _DataBuffer[yr * _GridSize + xr].x, xf), yf);
                
                i.position.y += 0.05 * height; 
                float clipH = _Height - i.position.y;
                clip(clipH); float3 N;
                if (clipH > 1e-3) N = normalize(i.normal);
                else N = lerp(lerp(_DataBuffer[yl * _GridSize + xl].yzw, _DataBuffer[yl * _GridSize + xr].yzw, xf),
                    lerp(_DataBuffer[yr * _GridSize + xl].yzw, _DataBuffer[yr * _GridSize + xr].yzw, xf), yf);
                float3 V = normalize(_WorldSpaceCameraPos - i.position);
                float3 L = normalize(_MainLightPosition.xyz); // Assume directional light
                float3 R = reflect(-L, N);
                float3 VT = normalize(float3(dot(V, i.tbn0), dot(V, i.tbn1), dot(V, i.tbn2)));

                float ambientStrength = 0.5, diffuseStrength = 0.4, specularStrength = 0.1;
                float nl = max(1e-6, dot(N, L));
                float spec = pow(max(1e-6, dot(V, R)), 16);

                float rimMask = 0.3 + 0.3 * (pow(1 - saturate(dot(N, V)), 2) + smoothstep(0.1, 0, clipH));
                float3 ambient = rimMask * albedo;
                float3 diffuse = diffuseStrength * albedo * nl;
                float3 specular = specularStrength * spec;

                float4 bubble = SAMPLE_TEXTURE2D(_BubbleMap, sampler_BubbleMap, 
                    (i.uv - 0.1 * VT.xy / VT.z) * _BubbleSV.xy + _Time.x * _BubbleSV.zw);
                float3 bubbleCol = bubble.g;

                float3 col = ambient + diffuse + specular + bubbleCol;

                float2 screenUV = i.screenpos.xy / i.screenpos.w;
                float rawDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                float2 refractOffset = i.normal.xy * _RefractionFrac * (sceneDepth - i.position.z);
                float2 refractUV = clamp(screenUV + refractOffset, float2(0, 0), float2(1, 1));

                float4 refractColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractUV);
                float alpha = max(albedo.a, bubble.a);
                col = lerp(refractColor.rgb, col, alpha);
                
                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}
