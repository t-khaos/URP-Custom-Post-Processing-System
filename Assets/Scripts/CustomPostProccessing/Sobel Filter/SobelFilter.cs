using CPP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CPP.Effects{
    [VolumeComponentMenu("Custom Post-processing/Sobel Filter")]
    public class SobelFilter : CustomPostProcessing{
        public ClampedFloatParameter lineThickness = new(0f, .0005f, .0025f);
        public BoolParameter outLineOnly = new(false);
        public BoolParameter posterize = new(false);
        public IntParameter count = new(6);

        private Material material;

        private const string ShaderName = "Hidden/PostProcess/SobleFilter";

        public override CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

        public override void Setup() {
            if (material == null)
                material = CoreUtils.CreateEngineMaterial(ShaderName);
        }

        public override bool IsActive() => material != null && lineThickness.value > 0f;

        public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination) {
            if (material == null)
                return;

            material.SetFloat("_Delta", lineThickness.value);
            material.SetInt("_PosterizationCount", count.value);
            if (outLineOnly.value)
                material.EnableKeyword("RAW_OUTLINE");
            else
                material.DisableKeyword("RAW_OUTLINE");
            if (posterize.value)
                material.EnableKeyword("POSTERIZE");
            else
                material.DisableKeyword("POSTERIZE");

            cmd.Blit(source, destination, material, 0);
        }

        public override void Dispose(bool disposing) {
            base.Dispose(disposing);
            CoreUtils.Destroy(material);
        }
    }
}