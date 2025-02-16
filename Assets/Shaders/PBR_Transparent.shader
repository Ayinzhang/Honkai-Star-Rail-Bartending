Shader "PBR/Transparent"
{
    Properties
    {
        _BaseMap("Base", 2D) = "white" {}
        _NormalMap("Normal", 2D) = "white" {}
        _MetalMap("Metal", 2D) = "white" {}
        _CubeMap("CubeMap", Cube) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 200

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #include "PBR_Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}