using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRendererFeature : ScriptableRendererFeature
{
    
    public enum RenderQueue
    {
        opaque,
        transparent,
    };

    //什么时候渲染
    public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingOpaques;
    //渲染队列
    public RenderQueue m_Queue = RenderQueue.opaque;
    //渲染过滤层
    public LayerMask layerMask = -1;

    #region copy颜色缓冲和深度信息
    //激活颜色图
    public bool m_Color;
    //激活深度图
    public bool m_Depth;
    //图格式
    public RenderTextureFormat m_TexFormat;
    //图名
    public string m_TexName = "CurrentColorDepthTex";

    #endregion
    CustomRendererPass m_Pass;
    internal static StencilState OverwriteStencil(StencilState s, int stencilWriteMask)
    {
        if (!s.enabled)
        {
            //没激活模板缓冲各种情况状态设置
            return new StencilState(
                true,
                0, (byte)stencilWriteMask,
                CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep,
                CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep
            );
        }
        //打开模板缓冲后的设置
        CompareFunction funcFront = s.compareFunctionFront != CompareFunction.Disabled ? s.compareFunctionFront : CompareFunction.Always;
        CompareFunction funcBack = s.compareFunctionBack != CompareFunction.Disabled ? s.compareFunctionBack : CompareFunction.Always;
        StencilOp passFront = s.passOperationFront;
        StencilOp failFront = s.failOperationFront;
        StencilOp zfailFront = s.zFailOperationFront;
        StencilOp passBack = s.passOperationBack;
        StencilOp failBack = s.failOperationBack;
        StencilOp zfailBack = s.zFailOperationBack;

        return new StencilState(
            true,
            (byte)(s.readMask & 0x0F), (byte)(s.writeMask | stencilWriteMask),
            funcFront, passFront, failFront, zfailFront,
            funcBack, passBack, failBack, zfailBack
        );
    }

    public override void Create()
    {
        //模板缓冲相关设置
        StencilStateData stencilData = new StencilStateData() { passOperation = StencilOp.Replace }; // 默认与延迟渲染兼容

        //模板缓冲状态设置
        var m_DefaultStencilState = StencilState.defaultValue;
        //本次示例不打开模板缓冲，但代码还是完整贴上来参考
        m_DefaultStencilState.enabled = stencilData.overrideStencilState;
        m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
        m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
        m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
        m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);
        // Bits [5,6] are used for material types.
        StencilState forwardOnlyStencilState = OverwriteStencil(m_DefaultStencilState, 0b_0110_0000);
        forwardOnlyStencilState.enabled = false;
        int forwardOnlyStencilRef = stencilData.stencilReference | 0b_0000_0000;

        //能被识别的有这些Tag的ShaderPass
        ShaderTagId[] forwardOnlyShaderTagIds = new ShaderTagId[] {
                    new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("LightweightForward") 
                };

        //本示例只给出不透明和半透明两种队列选择
        var queueRange = m_Queue == RenderQueue.opaque ? RenderQueueRange.opaque : RenderQueueRange.transparent;
        m_Pass = new CustomRendererPass("RenderObjectTest", forwardOnlyShaderTagIds, m_Queue == RenderQueue.opaque, passEvent, queueRange, layerMask, forwardOnlyStencilState, forwardOnlyStencilRef);

        //将拷贝颜色和深度信息配置传到Pass里
        m_Pass.Init(m_Color, m_Depth, m_TexName, m_TexFormat);
    }
    //每一帧都会被调用
    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_Pass.Setpu(renderer.cameraColorTarget);
        //将这个pass添加到渲染队列
        renderer.EnqueuePass(m_Pass);
    }
}
