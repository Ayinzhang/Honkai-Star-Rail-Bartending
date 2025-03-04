#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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