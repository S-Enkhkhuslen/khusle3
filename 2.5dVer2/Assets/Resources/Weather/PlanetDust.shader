Shader "Dusty Oxygen/Wind Dust"
{
    Properties
    {
        [MainTexture] _BaseMap("Dust Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        _NearFadeStart("Camera Fade Start", Float) = 4
        _NearFadeEnd("Camera Fade End", Float) = 9
        _SoftDistance("Soft Intersection", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _NearFadeStart, _NearFadeEnd, _SoftDistance;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half fog : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.color = input.color * _BaseColor;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * input.color;
                float distanceToCamera = distance(GetCameraPositionWS(), input.positionWS);
                color.a *= saturate((distanceToCamera - _NearFadeStart) / max(0.01, _NearFadeEnd - _NearFadeStart));
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(input.positionCS)), _ZBufferParams);
                float particleDepth = -TransformWorldToView(input.positionWS).z;
                color.a *= saturate((sceneDepth - particleDepth) / max(0.01, _SoftDistance));
                color.rgb = MixFog(color.rgb, input.fog);
                return color;
            }
            ENDHLSL
        }
    }
}
