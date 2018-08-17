Shader "Hidden/HDRenderPipeline/ColorResolve"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"

        Texture2DMS<float4> _ColorTextureMS;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
        };

        struct FragOut
        {
            float4 color : SV_Target;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        FragOut Frag1X(Varyings input) : SV_Target
        {
            FragOut fragOut;
            int2 msTex = int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y);
            fragOut.color = _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 0);
            return fragOut;
        }

        FragOut Frag2X(Varyings input) : SV_Target
        {
            FragOut fragOut;
            fragOut.color = (_ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 0)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 1)) * 0.5f;
            return fragOut;
        }

        FragOut Frag4X(Varyings input) : SV_Target
        {
            FragOut fragOut;
            fragOut.color = (_ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 0)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 1)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 2)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 3)) * 0.25f;
            return fragOut;
        }

        FragOut Frag8X(Varyings input) : SV_Target
        {
            FragOut fragOut;
            fragOut.color = (_ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 0)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 1)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 2)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 3)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 4)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 5)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 6)
            + _ColorTextureMS.Load(int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y), 7)) * 0.125f;
            return fragOut;
        }
    ENDHLSL
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: MSAA 1x
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag1X
            ENDHLSL
        }

        // 1: MSAA 2x
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag2X
            ENDHLSL
        }

        // 2: MSAA 4X
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag4X
            ENDHLSL
        }

        // 3: MSAA 8X
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag8X
            ENDHLSL
        }
    }
    Fallback Off
}