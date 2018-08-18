Shader "Hidden/HDRenderPipeline/DepthResolve"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"

        // Target multisampling texture
        Texture2DMS<float> _DepthTextureMS;

        // Different resolving approaches
        #define RESOLVE_MAX
        // #define RESOLVE_MIN
        // #define RESOLVE_AVERAGE

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
            int2 msTex = int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y);
            fragOut.depth = _DepthTextureMS.Load(msTex, 0).x;
            return fragOut;
        }

        FragOut Frag2X(Varyings input)
        {
            FragOut fragOut;
            int2 msTex = int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y);
            fragOut.color = float4(0.0, 0.0, 0.0, 1.0);
        #if defined(RESOLVE_MAX)
            fragOut.depth = max(_DepthTextureMS.Load(msTex, 0).x, _DepthTextureMS.Load(msTex, 1).x);
        #elif defined(RESOLVE_MIN)
            fragOut.depth = min(_DepthTextureMS.Load(msTex, 0).x, _DepthTextureMS.Load(msTex, 1).x);
        #elif defined(RESOLVE_AVERAGE)
            fragOut.depth = (_DepthTextureMS.Load(msTex, 0).x + _DepthTextureMS.Load(msTex, 1).x) * 0.5f;
        #else
            fragOut.depth = _DepthTextureMS.Load(msTex, 0).x;
        #endif
            return fragOut;
        }

        FragOut Frag4X(Varyings input)
        {
            FragOut fragOut;
            int2 msTex = int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y);
            fragOut.color = float4(0.0, 0.0, 0.0, 1.0);
        #if defined(RESOLVE_MAX)
            fragOut.depth = max(max(_DepthTextureMS.Load(msTex, 0).x, _DepthTextureMS.Load(msTex, 1).x),
                            max(_DepthTextureMS.Load(msTex, 2).x, _DepthTextureMS.Load(msTex, 3).x));
        #elif defined(RESOLVE_MIN)
            fragOut.depth = min(min(_DepthTextureMS.Load(msTex, 0).x, _DepthTextureMS.Load(msTex, 1).x),
                            min(_DepthTextureMS.Load(msTex, 2).x, _DepthTextureMS.Load(msTex, 3).x));
        #elif defined(RESOLVE_AVERAGE)
            fragOut.depth = (_DepthTextureMS.Load(msTex, 0).x + _DepthTextureMS.Load(msTex, 1).x
                            + _DepthTextureMS.Load(msTex, 2).x + _DepthTextureMS.Load(msTex, 3).x) * 0.25f;
        #else
            fragOut.depth = _DepthTextureMS.Load(msTex, 0).x;
        #endif
            return fragOut;
        }

        FragOut Frag8X(Varyings input)
        {
            FragOut fragOut;
            int2 msTex = int2(input.texcoord.x * _ScreenSize.x, input.texcoord.y * _ScreenSize.y);
            fragOut.color = float4(0.0, 0.0, 0.0, 1.0);
        #if defined(RESOLVE_MAX)
            fragOut.depth = max(max(max(_DepthTextureMS.Load(msTex, 0).x, _DepthTextureMS.Load(msTex, 1).x),
                            max(_DepthTextureMS.Load(msTex, 2).x, _DepthTextureMS.Load(msTex, 3).x)),
                            max(max(_DepthTextureMS.Load(msTex, 4).x, _DepthTextureMS.Load(msTex, 5).x),
                            max(_DepthTextureMS.Load(msTex, 6).x, _DepthTextureMS.Load(msTex, 7).x)));
        #elif defined(RESOLVE_MIN)
            fragOut.depth = min(min(min(_DepthTextureMS.Load(msTex, 0).x, _DepthTextureMS.Load(msTex, 1).x),
                            min(_DepthTextureMS.Load(msTex, 2).x, _DepthTextureMS.Load(msTex, 3).x)),
                            min(min(_DepthTextureMS.Load(msTex, 4).x, _DepthTextureMS.Load(msTex, 5).x),
                            min(_DepthTextureMS.Load(msTex, 6).x, _DepthTextureMS.Load(msTex, 7).x)));
        #elif defined(RESOLVE_AVERAGE)
            fragOut.depth = (_DepthTextureMS.Load(msTex, 0).x + _DepthTextureMS.Load(msTex, 1).x
                            + _DepthTextureMS.Load(msTex, 2).x + _DepthTextureMS.Load(msTex, 3).x
                            + _DepthTextureMS.Load(msTex, 4).x + _DepthTextureMS.Load(msTex, 5).x
                            + _DepthTextureMS.Load(msTex, 6).x + _DepthTextureMS.Load(msTex, 7).x) * 0.125f;
        #else
            fragOut.depth = _DepthTextureMS.Load(msTex, 0).x;
        #endif
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