Shader "PBR/Opaque"
{
    Properties
    {
        _Color("Col", Color) = (1, 1, 1, 1)
        _BaseMap("Base", 2D) = "white" {}
        _NormalMap("Normal", 2D) = "white" {}
        _MetalMap("Metal", 2D) = "white" {}
        _CubeMap("CubeMap", Cube) = "white" {}
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
            #include "PBR_Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}