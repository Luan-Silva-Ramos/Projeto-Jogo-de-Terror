Shader "Mansao/Slate V20"
{
    Properties
    {
        _BaseMap("Ardosia",2D)="white"{}
        _BaseColor("Tonalidade",Color)=(0.46,0.46,0.46,1)
        [Normal] _BumpMap("Normal",2D)="bump"{}
        _BumpScale("Relevo",Range(0,1))=0.35
        _RoughMap("Rugosidade",2D)="white"{}
        _Smoothness("Brilho maximo",Range(0,1))=0.35
        _VEnd("Limite da textura sem borda clara",Range(0.9,1))=0.965
        _Cutoff("Cutoff",Float)=0.5
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_RoughMap); SAMPLER(sampler_RoughMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half _BumpScale, _Smoothness, _VEnd, _Cutoff;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS:POSITION;
                half3 normalOS:NORMAL;
                half4 tangentOS:TANGENT;
                float2 uv:TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                half3 normalWS:TEXCOORD1;
                half4 tangentWS:TEXCOORD2;
                float2 uv:TEXCOORD3;
                half fog:TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n=GetVertexNormalInputs(input.normalOS,input.tangentOS);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS;
                o.normalWS=n.normalWS;
                o.tangentWS=half4(n.tangentWS,input.tangentOS.w*GetOddNegativeScale());
                o.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // A borda clara fica no topo da imagem: amostrar somente 0..0.965 em V.
                // Gradientes antes de frac evitam linhas de mip entre repeticoes.
                float2 dx=ddx(input.uv)*float2(1,_VEnd);
                float2 dy=ddy(input.uv)*float2(1,_VEnd);
                float2 uv=float2(input.uv.x,frac(input.uv.y)*_VEnd);
                half3 color=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,uv,dx,dy).rgb;
                half rough=SAMPLE_TEXTURE2D_GRAD(_RoughMap,sampler_RoughMap,uv,dx,dy).r;
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap,sampler_BumpMap,uv,dx,dy),_BumpScale);
                half3 n=normalize(input.normalWS);
                half3 t=normalize(input.tangentWS.xyz);
                half3 b=cross(n,t)*input.tangentWS.w;
                InputData data=(InputData)0;
                data.positionWS=input.positionWS;
                data.normalWS=NormalizeNormalPerPixel(TransformTangentToWorld(normalTS,half3x3(t,b,n)));
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(input.positionWS);
                data.fogCoord=input.fog;
                data.vertexLighting=VertexLighting(input.positionWS,data.normalWS);
                data.bakedGI=SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=color*_BaseColor.rgb;
                surface.metallic=0; surface.smoothness=(1-rough)*_Smoothness;
                surface.normalTS=normalTS; surface.occlusion=1; surface.alpha=1;
                half4 result=UniversalFragmentPBR(data,surface);
                result.rgb=MixFog(result.rgb,input.fog);
                return result;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
