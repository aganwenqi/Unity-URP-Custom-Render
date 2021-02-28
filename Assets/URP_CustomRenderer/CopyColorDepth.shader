Shader "Hidden/CopyColorDepth"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
    }
    SubShader
    {
        
	  Tags {"RenderPipeline" = "UniversalPipeline"}

        Pass
		{
			ZTest Off Cull Off ZWrite Off
			HLSLPROGRAM

			#include  "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#pragma vertex vertex
			#pragma fragment frag
			#pragma shader_feature _ _enableDepth
			CBUFFER_START(UnityPerMaterial)
				TEXTURE2D(_MainTex);
				SAMPLER(sampler_MainTex);
#if _enableDepth
				TEXTURE2D(_CameraDepthAttachment);
				SAMPLER(sampler_CameraDepthAttachment);
				TEXTURE2D(_CameraDepthTexture);
				SAMPLER(sampler_CameraDepthTexture);
#endif

			CBUFFER_END
			
			struct Attributes
            {
                float4 positionOS : POSITION;
                float4 texcoord : TEXCOORD0;
            };

			struct Varyings
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
	
			Varyings vertex(Attributes v)
			{
				Varyings o = (Varyings)0;
				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
				o.pos = vertexInput.positionCS;
				o.uv = v.texcoord.xy;
				return o;
			}
	
			half4 frag(Varyings i) : SV_Target
			{
				half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
#if _enableDepth
				half depthTextureValue1 = SAMPLE_DEPTH_TEXTURE(_CameraDepthAttachment, sampler_CameraDepthAttachment, i.uv);
				half terrainDp1 = Linear01Depth(depthTextureValue1, _ZBufferParams);
				mainTex.a = terrainDp1;
#endif
				return mainTex;
			}
			
			ENDHLSL
		}
    }
}
