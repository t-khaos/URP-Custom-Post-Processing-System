#ifndef _MIPGAUSSIAN_INCLUDED
#define _MIPGAUSSIAN_INCLUDED

#define PI 3.141592654

Texture2D _DownSampleTexture;
SamplerState sampler_DownSampleTexture;
float4 _DownSampleTexture_TexelSize;

Texture2D _UpSampleTexture;
SamplerState sampler_UpSampleTexture;
float4 _UpSampleTexture_TexelSize;

float g_sigma;
uint g_level;


float MipGaussianBlendWeight()
{
    float sigma = g_sigma;
    const float sigma2 = sigma * sigma;
    const float c = 2.0 * PI * sigma2;
    const float numerator = (1 << (g_level << 2)) * log(4.0);
    const float denorminator = c * ((1 << (g_level << 1)) + c);
    return clamp(numerator / denorminator, 0, 1);
}

half4 MipGaussianDownSampleFragment(Varyings input) : SV_Target
{
    if (g_level == 10)
    {
        return _DownSampleTexture.SampleLevel(sampler_DownSampleTexture, input.uv, 10);
    }

    const float weight = MipGaussianBlendWeight();

    const float3 Color = _UpSampleTexture.SampleLevel(sampler_UpSampleTexture, input.uv, g_level + 1);
    const float3 src = _DownSampleTexture.SampleLevel(sampler_DownSampleTexture, input.uv, g_level);
    return float4((1 - weight) * Color + weight * src, 1.0);
}

#endif
