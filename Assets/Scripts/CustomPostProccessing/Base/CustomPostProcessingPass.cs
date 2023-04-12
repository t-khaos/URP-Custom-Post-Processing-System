using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CPP{
    public class CustomPostProcessingPass : ScriptableRenderPass{
        // 所有自定义后处理基类
        private List<CustomPostProcessing> mCustomPostProcessings;

        // 当前active组件下标
        private List<int> mActiveCustomPostProcessingIndex;

        // 每个组件对应的ProfilingSampler
        private string mProfilerTag;
        private List<ProfilingSampler> mProfilingSamplers;

        // 声明RT
        private RenderTargetIdentifier mSourceRT;
        private RenderTargetIdentifier mDesRT;
        private RenderTargetIdentifier mTempRT0;
        private RenderTargetIdentifier mTempRT1;
        private int mSourceId;
        private int mDesId;
        private int mTempRT0Id = Shader.PropertyToID("_TemporaryRenderTexture0");
        private int mTempRT1Id = Shader.PropertyToID("_TemporaryRenderTexture1");

        public CustomPostProcessingPass(string profilerTag, List<CustomPostProcessing> customPostProcessings) {
            mProfilerTag = profilerTag;
            mCustomPostProcessings = customPostProcessings;
            mActiveCustomPostProcessingIndex = new List<int>(customPostProcessings.Count);
            // 将自定义后处理器对象列表转换成一个性能采样器对象列表
            mProfilingSamplers = customPostProcessings.Select(c => new ProfilingSampler(c.ToString())).ToList();

            // mTempRT0 = RTHandles.Alloc("_TemporaryRenderTexture0", name: "_TemporaryRenderTexture0");
            // mTempRT1 = RTHandles.Alloc("_TemporaryRenderTexture1", name: "_TemporaryRenderTexture1");
        }

        // 设置CustomPostProcessing，并返回是否存在有效组件
        public bool SetupCustomPostProcessing() {
            mActiveCustomPostProcessingIndex.Clear();
            for (int i = 0; i < mCustomPostProcessings.Count; i++) {
                mCustomPostProcessings[i].Setup();
                if (mCustomPostProcessings[i].IsActive()) {
                    mActiveCustomPostProcessingIndex.Add(i);
                }
            }

            return mActiveCustomPostProcessingIndex.Count != 0;
        }

        // 设置渲染源和渲染目标
        // public void Setup(RTHandle source, RTHandle destination) {
        //     mSourceRT = source;
        //     mDesRT = destination;
        // }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blitTargetDescriptor.depthBufferBits = 0;

            var renderer = renderingData.cameraData.renderer;

            // mSourceRT = renderer.cameraColorTargetHandle;
            // mDesRT = renderer.cameraColorTargetHandle;

            mSourceId = -1;
            mSourceRT = renderer.cameraColorTargetHandle.nameID;
            mDesId = -1;
            mDesRT = renderer.cameraColorTargetHandle.nameID;

            Debug.Log(renderer.cameraColorTargetHandle.name);
        }

        // 实现渲染逻辑
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            // 初始化commandbuffer
            var cmd = CommandBufferPool.Get(mProfilerTag);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // 获取相机Descriptor
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;

            // 初始化临时RT
            // bool rt1Used = false;

            // 如果des没有初始化，则需要获取RT，主要是des为AfterPostProcessTexture的情况
            // if (mDesRT != renderingData.cameraData.renderer.cameraColorTargetHandle /* && !mDesRT.HasInternalRenderTargetId()*/) {
            //     cmd.GetTemporaryRT(Shader.PropertyToID(mDesRT.name), descriptor, FilterMode.Trilinear);
            // }
            
            cmd.GetTemporaryRT(mTempRT0Id, descriptor);
            mTempRT0 = new RenderTargetIdentifier(mTempRT0Id);

            // 执行每个组件的Render方法
            if (mActiveCustomPostProcessingIndex.Count == 1) {
                int index = mActiveCustomPostProcessingIndex[0];
                using (new ProfilingScope(cmd, mProfilingSamplers[index])) {
                    mCustomPostProcessings[index].Render(cmd, ref renderingData, mSourceRT, mTempRT0);
                }
            }
            // else {
            //     // 如果有多个组件，则在两个RT上来回bilt
            //     cmd.GetTemporaryRT(Shader.PropertyToID(mTempRT1.name), descriptor);
            //     rt1Used = true;
            //     Blit(cmd, mSourceRT, mTempRT0);
            //     for (int i = 0; i < mActiveCustomPostProcessingIndex.Count; i++) {
            //         int index = mActiveCustomPostProcessingIndex[i];
            //         var customProcessing = mCustomPostProcessings[index];
            //         using (new ProfilingScope(cmd, mProfilingSamplers[index])) {
            //             customProcessing.Render(cmd, ref renderingData, mTempRT0, mTempRT1);
            //         }
            //
            //         CoreUtils.Swap(ref mTempRT0, ref mTempRT1);
            //     }
            // }
            

            ProfilingSampler tmp = new ProfilingSampler("to camera");
            using (new ProfilingScope(cmd, tmp)) {
                // Blit(cmd, mSourceRT, mTempRT0);
                Blit(cmd, mTempRT0, mDesRT);
            }

            // 释放
            cmd.ReleaseTemporaryRT(mTempRT0Id);
            // cmd.ReleaseTemporaryRT(mTempRT1Id);
            // if (rt1Used) cmd.ReleaseTemporaryRT(Shader.PropertyToID(mTempRT1.name));

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}