using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRendererPass : ScriptableRenderPass
{
    //��Ⱦ���С���Ⱦ�����
    FilteringSettings m_FilteringSettings;
    //��Ⱦ��
    RenderStateBlock m_RenderStateBlock;
    //ʶ��Shader��ǩ�б�
    List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

    bool m_IsOpaque;

    #region copy��ɫ����������Ϣ
    //������ɫͼ
    public bool m_Color;
    //�������ͼ
    public bool m_Depth;
    //��ɫ�����RT��rgb:��ɫ��a��ȣ�
    RenderTargetHandle m_ColorDepthTexture;
    //ͼ��ʽ
    public RenderTextureFormat m_TexFormat;
    //�����ͼ����
    public Material m_ColorDepthMat;
    FilterMode filterMode { get; set; }
    #endregion

    //��ǰ��ɫ�����е�RT
    RenderTargetIdentifier m_Source;
    //FrameDebug����ʾ
    string m_ProfilerTag;
    ProfilingSampler m_ProfilingSampler;

    public CustomRendererPass(string profilerTag, ShaderTagId[] shaderTagIds, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
    {
        base.profilingSampler = new ProfilingSampler(nameof(CustomRendererPass));

        m_ProfilerTag = profilerTag;
        m_ProfilingSampler = new ProfilingSampler(profilerTag);
        //��Ҫʶ���Shader��ǩ�������
        foreach (ShaderTagId sid in shaderTagIds)
            m_ShaderTagIdList.Add(sid);
        renderPassEvent = evt;
        m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

        //ģ����Ծݺ�
        m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        m_IsOpaque = opaque;
        
        if (stencilState.enabled)//ģ������Ƿ񼤻�
        {
            m_RenderStateBlock.stencilReference = stencilReference;
            m_RenderStateBlock.mask = RenderStateMask.Stencil;
            m_RenderStateBlock.stencilState = stencilState;
        }
    }

    //��ʼ����ɫ�������Ϣ����
    public void Init(bool color, bool depth, string texName, RenderTextureFormat texFormat)
    {
        m_Color = color;
        m_Depth = depth;
        m_TexFormat = texFormat;
        m_ColorDepthTexture.Init(texName);

        m_ColorDepthMat = new Material(Shader.Find("Hidden/CopyColorDepth"));
        if (!m_ColorDepthMat)
            return;

        if (m_Depth)
        {
            //�������д��RT��Aͨ��
            m_ColorDepthMat.EnableKeyword("_enableDepth");
        }
        else
        {
            m_ColorDepthMat.DisableKeyword("_enableDepth");
        }
    }
    
    public void Setpu(RenderTargetIdentifier source)
    {
        m_Source = source;
    }

    //����һ�Ŵ洢��ǰ��ɫ�������������ϢRT
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if(m_Depth || m_Color)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.colorFormat = m_TexFormat;
            descriptor.depthBufferBits = 0; 
            descriptor.msaaSamples = 1;
            //����һ��RT
            cmd.GetTemporaryRT(m_ColorDepthTexture.id, descriptor, FilterMode.Bilinear);
        }
        
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            ExecuteCommand(context, cmd);
            var sortFlags = (m_IsOpaque) ? renderingData.cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;
            //������Ⱦ��ص�����
            var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortFlags);
            //��Ⱦ����
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings, ref m_RenderStateBlock);

            //�洢��ǰ������ɫ�������Ϣ
            if (m_Color || m_Depth)
            {
                cmd.Blit(m_Source, m_ColorDepthTexture.Identifier(), m_ColorDepthMat);
                //cmd.Blit(m_Source, m_Source);
            }
        }
        ExecuteCommand(context, cmd);
    }
    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);
        //���ٴ�����RT
        if (m_Color || m_Depth)
        {
            if (m_ColorDepthTexture != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(m_ColorDepthTexture.id);
            }
            
        }
    }
    void ExecuteCommand(ScriptableRenderContext context, CommandBuffer cmd)
    {
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }
}
