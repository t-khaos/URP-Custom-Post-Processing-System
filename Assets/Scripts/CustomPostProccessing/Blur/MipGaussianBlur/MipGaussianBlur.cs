using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CPP;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using Color = UnityEngine.Color;
using Graphics = UnityEngine.Graphics;

namespace CPP.EFFECTS
{
    [VolumeComponentMenu(("Custom Post Processing/Blur/Mipmap Gaussian Blur"))]
    public class MipGaussianBlur : CustomPostProcessing
    {
        enum ShaderPass
        {
            Copy = -1,
            DownSample = 0,
            UpSample = 1,
        }


        public ClampedFloatParameter Sigma = new ClampedFloatParameter(0.0f, 0.0f, 10.0f);

        private const int mipCount = 11;
        private const string mShaderName = "Hidden/PostProcessing/MipGaussianBlur";

        private int mSigmaKeyword = Shader.PropertyToID("g_sigma");
        private int mMipLevelKeyword = Shader.PropertyToID("g_level");
        public override bool IsActive() => mMaterial != null && Sigma != 0;

        public override CustomPostProcessInjectionPoint InjectionPoint =>
            CustomPostProcessInjectionPoint.AfterPostProcess;

        public override int OrderInInjectionPoint => 5;

        private const string mDownSampleRTName = "_DownSampleTexture";
        private readonly int mDownSampleTextureId = Shader.PropertyToID(mDownSampleRTName);
        private RTHandle mDownSampleRT;

        private const string mUpSampleRTName = "_UpSampleTexture";
        private readonly int mUpSampleTextureId = Shader.PropertyToID(mUpSampleRTName);
        private readonly RTHandle[] mUpSampleRTs = new RTHandle[mipCount];

        public override void Setup()
        {
            if (mMaterial == null)
                mMaterial = CoreUtils.CreateEngineMaterial(mShaderName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = GetCameraRenderTextureDescriptor(renderingData);
            
            var size = 1;
            for (var i = mipCount - 1; i >= 0; i--)
            {
                descriptor.width = size;
                descriptor.height = size;
                size *= 2;
                RenderingUtils.ReAllocateIfNeeded(ref mUpSampleRTs[i], descriptor, name: mUpSampleRTName + i,
                    wrapMode: TextureWrapMode.Clamp, filterMode: FilterMode.Bilinear);
            }

            descriptor.useMipMap = true;
            descriptor.mipCount = mipCount;
            descriptor.autoGenerateMips = false;
            RenderingUtils.ReAllocateIfNeeded(ref mDownSampleRT, descriptor, name: mDownSampleRTName,
                wrapMode: TextureWrapMode.Clamp, filterMode: FilterMode.Bilinear);
        }


        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, in RTHandle source,
            in RTHandle destination)
        {
            if (mMaterial == null) return;

            using (new ProfilingScope(cmd, new ProfilingSampler("DownSample")))
            {
                Blitter.BlitCameraTexture(cmd, source, mDownSampleRT);
                cmd.GenerateMips(mDownSampleRT);
            }

            using (new ProfilingScope(cmd, new ProfilingSampler("UpSample")))
            {
                for (var i = mipCount - 1; i >= 0; i--)
                {
                    cmd.SetGlobalFloat(mSigmaKeyword, 10);
                    cmd.SetGlobalInt(mMipLevelKeyword, i);

                    cmd.SetGlobalTexture(mDownSampleTextureId, mDownSampleRT);
                    cmd.SetGlobalTexture(mUpSampleTextureId, mUpSampleRTs[i == mipCount - 1 ? i : i + 1]);

                    CoreUtils.SetRenderTarget(cmd, mUpSampleRTs[i], RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store);
                    cmd.DrawProcedural(Matrix4x4.identity, mMaterial, (int)ShaderPass.UpSample, MeshTopology.Triangles,
                        3);
                }
            }

            using (new ProfilingScope(cmd, new ProfilingSampler("Blit")))
            {
                Blitter.BlitCameraTexture(cmd, mUpSampleRTs[0], destination);
            }
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoreUtils.Destroy(mMaterial);
            
            mDownSampleRT?.Release();
            foreach (var rt in mUpSampleRTs)
            {
                rt?.Release();
            }
        }
    }
}