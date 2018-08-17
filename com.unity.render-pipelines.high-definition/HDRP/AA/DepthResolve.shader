Shader "Hidden/HDRenderPipeline/DepthResolve"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"

        Texture2DMS<float> _DepthTextureMS;

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
            float depth : SV_Depth;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        FragOut Frag1X(Varyings input)
        {
            FragOut fragOut;
            fragOut.color = float4(0.0, 0.0, 0.0, 1.0);
            fragOut.depth = _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696), 0).x;
            return fragOut;
        }

        FragOut Frag2X(Varyings input)
        {
            FragOut fragOut;
            fragOut.color = float4(0.0, 0.0, 0.0, 1.0);
            fragOut.depth = (_DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696), 0).x
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696), 1)).x * 0.5f;
            return fragOut;
        }

        FragOut Frag4X(Varyings input)
        {
            FragOut fragOut;
            fragOut.color = float4(0.0, 0.0, 0.0, 1.0);
            fragOut.depth = (_DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696).x, 0)
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696), 1).x
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696), 2).x
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696), 3).x) * 0.25f;
            return fragOut;
        }

        FragOut Frag8X(Varyings input)
        {
            FragOut fragOut;
            fragOut.color = float4(0.0, 0.0, 0.0, 1.0);
            fragOut.depth = (_DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696).x, 0)
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696).x, 1)
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696).x, 2)
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696).x, 3)
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696).x, 4)
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696).x, 5)
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696).x, 6)
            + _DepthTextureMS.Load(int2(input.texcoord.x * 1238, input.texcoord.y * 696).x, 7)) * 0.125f;
            return fragOut;
        }
    ENDHLSL
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: MSAA 1x
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag1X
            ENDHLSL
        }

        // 1: MSAA 2x
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag2X
            ENDHLSL
        }

        // 2: MSAA 4X
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag4X
            ENDHLSL
        }

        // 3: MSAA 8X
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag8X
            ENDHLSL
        }
    }
    Fallback Off
}