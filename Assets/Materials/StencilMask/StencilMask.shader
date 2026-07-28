Shader "Custom/StencilMask"
{
    Properties
    {
        [IntRange] _StencilRef("Stencil Ref", Range(0, 255)) = 1
    }

    SubShader
    {
        Tags {
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass {
            Blend Zero One
            ZWrite Off
            Offset -1, -1
            ColorMask 0

            Stencil {
                Ref [_StencilRef]
                Comp Always
                PassFront Replace
                // ZFailFront Replace
                ZFailBack Keep
            }
        }
    }
}
