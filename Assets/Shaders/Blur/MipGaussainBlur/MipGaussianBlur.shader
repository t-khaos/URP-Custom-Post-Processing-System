Shader "Hidden/PostProcessing/MipGaussianBlur" {
    SubShader {
        Tags {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200
        ZWrite Off
        Cull Off
        
        Pass {
            Name "Mipmap Gaussian DownSample Pass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment MipGaussianDownSampleFragment

            #include "../../Common/PostProcessing.hlsl"
            #include "MipGaussianDownSample.hlsl"
            ENDHLSL
        } 

        Pass {
            Name "Mipmap Gaussian UpSample Pass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment MipGaussianUpSampleFragment

            #include "../../Common/PostProcessing.hlsl"
            #include "MipGaussianUpSample.hlsl"
            ENDHLSL
        } 
    }
}