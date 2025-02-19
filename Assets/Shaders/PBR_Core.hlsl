#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _Color;
CBUFFER_END

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
TEXTURE2D(_MetalMap); SAMPLER(sampler_MetalMap);
TEXTURECUBE(_CubeMap); SAMPLER(sampler_CubeMap);

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
    float3 tbn0 : TEXCOORD2;
    float3 tbn1 : TEXCOORD3;
    float3 tbn2 : TEXCOORD4;
};

inline half3 DecodeHDR(half4 data, half4 decodeInstructions)
{
    half alpha = decodeInstructions.w * (data.a - 1) + 1;  
#if defined(UNITY_COLORSPACE_GAMMA)
    return (decodeInstructions.x * alpha) * data.rgb;
#else
#   if defined(UNITY_USE_NATIVE_HDR)
    return decodeInstructions.x * data.rgb;
#   else
    return (decodeInstructions.x * pow(alpha, decodeInstructions.y)) * data.rgb;
#   endif
#endif
}
    
v2f vert(appdata v)
{
    v2f o;
    o.uv = v.uv;
    o.vertex = TransformObjectToHClip(v.vertex);
    o.position = TransformObjectToWorld(v.vertex.xyz);
            
    float3 normalWS = TransformObjectToWorldNormal(v.normal);
    float3 tangentWS = TransformObjectToWorldDir(v.tangent.xyz);
    float3 bitangentWS = normalize(cross(normalWS, tangentWS) * v.tangent.w);
            
    o.tbn0 = float3(tangentWS.x, bitangentWS.x, normalWS.x);
    o.tbn1 = float3(tangentWS.y, bitangentWS.y, normalWS.y);
    o.tbn2 = float3(tangentWS.z, bitangentWS.z, normalWS.z);

    return o;
}


float4 frag(v2f i): SV_Target
{
    float4 albedo = _Color * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
    float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));
    float metal = SAMPLE_TEXTURE2D(_MetalMap, sampler_MetalMap, i.uv).a;
    float rough = 1 - SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv).a;
    float3 lightCol = GetMainLight().color;
        
    float3 N = normalize(float3(dot(i.tbn0, normal), dot(i.tbn1, normal), dot(i.tbn2, normal)));
    float3 V = normalize(_WorldSpaceCameraPos - i.position);
    float3 L = normalize(_MainLightPosition.xyz); // Assume directional light
    float3 H = normalize(L + V);
    float3 VR = reflect(-V, N);
    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo.rgb, metal);
    float hv = max(1e-6, dot(H, V));
    float nl = max(1e-6, dot(N, L));
    float nv = max(1e-6, dot(N, V));
    float nh = max(1e-6, dot(N, H));
            
    float D = rough * rough / (PI * pow((nh * nh * (rough * rough - 1) + 1), 2));
    float F = F0 + (1 - F0) * pow((1 - hv), 5);
    float k = (rough * rough + 1) / 8;
    float G = nl * nv / ((nv * (1 - k) + k) * (nv * (1 - k) + k));
    float3 specular = lightCol * D * F * G / (4 * nl * nv);
    float3 diffuse = (1 - F) * (1 - metal) * albedo.rgb / PI;
    float3 directLight = (specular + diffuse) * nl;
        
    float mip = rough * (1.7 - 0.7 * rough) * UNITY_SPECCUBE_LOD_STEPS;
    float4 env = SAMPLE_TEXTURECUBE_LOD(_CubeMap, sampler_CubeMap, VR, mip);
    float3 flast = F0 + (max(float3(1, 1, 1) * (1 - rough), F0) - F0) * pow(1 - nv, 5);
    float4 p0 = float4(0.5745, 1.548, -0.02397, 1.301);
    float4 p1 = float4(0.5753, -0.2511, -0.02066, 0.4755);
    float4 t = metal * p0 + p1;
    float bias = saturate(t.x * min(t.y, exp2(-7.672 * nv)) + t.z);
    float scale = saturate(t.w) - bias;
    bias *= saturate(50.0 * flast.y);
    float3 iblSpecular = lightCol * (flast * scale + bias) * DecodeHDR(env, unity_SpecCube0_HDR);
    float3 iblDiffuse = albedo * (1 - flast) * (1 - metal) / PI;
    float3 indirectLight = iblSpecular + iblDiffuse;
    
    float3 col = directLight + indirectLight;
        
    return float4(col.rgb, albedo.a);
}