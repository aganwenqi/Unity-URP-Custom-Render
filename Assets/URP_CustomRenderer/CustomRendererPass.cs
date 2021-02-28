using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRendererPass : ScriptableRenderPass
{
    //渲染队列、渲染层过滤
    FilteringSettings m_FilteringSettings;
    //渲染块
    RenderStateBlock m_RenderStateBlock;
    //识别Shader标签列表
    List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

    bool m_IsOpaque;

    #region copy颜色缓冲和深度信息
    //激活颜色图
    public bool m_Color;
    //激活深度图
    public bool m_Depth;
    //颜色和深度RT（rgb:颜色，a深度）
    RenderTargetHandle m_ColorDepthTexture;
    //图格式
    public RenderTextureFormat m_TexFormat;
    //处理该图材质
    public Material m_ColorDepthMat;
    FilterMode filterMode { get; set; }
    #endregion

    //当前颜色缓冲中的RT
    RenderTargetIdentifier m_Source;
    //FrameDebug中显示
    string m_ProfilerTag;
    ProfilingSampler m_ProfilingSampler;

    public CustomRendererPass(string profilerTag, ShaderTagId[] shaderTagIds, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
    {
        base.profilingSampler = new ProfilingSampler(nameof(CustomRendererPass));

        m_ProfilerTag = profilerTag;
        m_ProfilingSampler = new ProfilingSampler(profilerTag);
        //将要识别的Shader标签加入队列
        foreach (ShaderTagId sid in shaderTagIds)
            m_ShaderTagIdList.Add(sid);
        renderPassEvent = evt;
        m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

        //模板测试据盒
        m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        m_IsOpaque = opaque;
        
        if (stencilState.enabled)//模板测试是否激活
        {
            m_RenderStateBlock.stencilReference = stencilReference;
            m_RenderStateBlock.mask = RenderStateMask.Stencil;
            m_RenderStateBlock.stencilState = stencilState;
        }
    }

    //初始化颜色和深度信息配置
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
            //开启深度写入RT的A通道
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

    //创建一张存储当前颜色缓冲区或深度信息RT
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if(m_Depth || m_Color)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.colorFormat = m_TexFormat;
            descriptor.depthBufferBits = 0; 
            descriptor.msaaSamples = 1;
            //创建一张RT
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
            //创建渲染相关的设置
            var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortFlags);
            //渲染物体
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings, ref m_RenderStateBlock);

            //存储当前场景颜色或深度信息
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
        //销毁创建的RT
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
