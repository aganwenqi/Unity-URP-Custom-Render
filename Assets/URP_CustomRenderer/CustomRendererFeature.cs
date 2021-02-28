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

    //ʲôʱ����Ⱦ
    public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingOpaques;
    //��Ⱦ����
    public RenderQueue m_Queue = RenderQueue.opaque;
    //��Ⱦ���˲�
    public LayerMask layerMask = -1;

    #region copy��ɫ����������Ϣ
    //������ɫͼ
    public bool m_Color;
    //�������ͼ
    public bool m_Depth;
    //ͼ��ʽ
    public RenderTextureFormat m_TexFormat;
    //ͼ��
    public string m_TexName = "CurrentColorDepthTex";

    #endregion
    CustomRendererPass m_Pass;
    internal static StencilState OverwriteStencil(StencilState s, int stencilWriteMask)
    {
        if (!s.enabled)
        {
            //û����ģ�建��������״̬����
            return new StencilState(
                true,
                0, (byte)stencilWriteMask,
                CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep,
                CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep
            );
        }
        //��ģ�建��������
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
        //ģ�建���������
        StencilStateData stencilData = new StencilStateData() { passOperation = StencilOp.Replace }; // Ĭ�����ӳ���Ⱦ����

        //ģ�建��״̬����
        var m_DefaultStencilState = StencilState.defaultValue;
        //����ʾ������ģ�建�壬�����뻹�������������ο�
        m_DefaultStencilState.enabled = stencilData.overrideStencilState;
        m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
        m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
        m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
        m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);
        // Bits [5,6] are used for material types.
        StencilState forwardOnlyStencilState = OverwriteStencil(m_DefaultStencilState, 0b_0110_0000);
        forwardOnlyStencilState.enabled = false;
        int forwardOnlyStencilRef = stencilData.stencilReference | 0b_0000_0000;

        //�ܱ�ʶ�������ЩTag��ShaderPass
        ShaderTagId[] forwardOnlyShaderTagIds = new ShaderTagId[] {
                    new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("LightweightForward") 
                };

        //��ʾ��ֻ������͸���Ͱ�͸�����ֶ���ѡ��
        var queueRange = m_Queue == RenderQueue.opaque ? RenderQueueRange.opaque : RenderQueueRange.transparent;
        m_Pass = new CustomRendererPass("RenderObjectTest", forwardOnlyShaderTagIds, m_Queue == RenderQueue.opaque, passEvent, queueRange, layerMask, forwardOnlyStencilState, forwardOnlyStencilRef);

        //��������ɫ�������Ϣ���ô���Pass��
        m_Pass.Init(m_Color, m_Depth, m_TexName, m_TexFormat);
    }
    //ÿһ֡���ᱻ����
    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_Pass.Setpu(renderer.cameraColorTarget);
        //�����pass��ӵ���Ⱦ����
        renderer.EnqueuePass(m_Pass);
    }
}
