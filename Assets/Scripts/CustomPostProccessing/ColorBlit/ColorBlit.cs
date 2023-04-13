using CPP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CPP.EFFECTS{
    [VolumeComponentMenu("Custom Post-processing/Color Blit")]
    public class ColorBlit : CustomPostProcessing{
        public ClampedFloatParameter intensity = new(0.0f, 0.0f, 2.0f);

        private Material mMaterial;
        private const string mShaderName = "Hidden/PostProcess/ColorBlit";

        public override bool IsActive() => mMaterial != null && intensity.value > 0;

        public override CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.AfterOpaqueAndSky;
        public override int OrderInInjectionPoint => 0;

        public override void Setup() {
            if (mMaterial == null)
                mMaterial = CoreUtils.CreateEngineMaterial(mShaderName);
        }

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination) {
            if (mMaterial == null) return;
            mMaterial.SetFloat("_Intensity", intensity.value);
            cmd.Blit(source, destination, mMaterial, 0);
        }

        public override void Dispose(bool disposing) {
            base.Dispose(disposing);
            CoreUtils.Destroy(mMaterial);
        }
    }
}