Shader "Rouge/Character"
{
    Properties
    {
        [MainTexture]  _BaseMap     ("Main Texture", 2D) = "white" {}
        [MainColor]    _BaseColor   ("Color", Color) = (1, 1, 1, 1)
        _Metallic      ("Metallic", Range(0, 1)) = 0
        _Smoothness    ("Smoothness", Range(0, 1)) = 0.3
        [HDR] _HitColor   ("Hit Flash Color", Color) = (1, 1, 1, 1)
        _HitAmount     ("Hit Flash Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        // ── ShadowCaster (depth pass) ──
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ── ForwardLit (PBR) ──
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 texcoord   : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _HitColor;
            half  _HitAmount;
            half  _Metallic;
            half  _Smoothness;
            float4 _BaseMap_ST;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs posInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrmInput  = GetVertexNormalInputs(input.normalOS);

                o.positionCS = posInput.positionCS;
                o.positionWS = posInput.positionWS;
                o.normalWS   = nrmInput.normalWS;
                o.uv         = TRANSFORM_TEX(input.texcoord, _BaseMap);
                o.fogCoord   = ComputeFogFactor(posInput.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                half  alpha  = _BaseColor.a;

                half3 normalWS = normalize(i.normalWS);
                half3 viewDirWS = SafeNormalize(GetCameraPositionWS() - i.positionWS);

                // ── Direct lighting ──
                Light light = GetMainLight();
                half NdotL = saturate(dot(normalWS, light.direction));

                // Surface data for PBR
                BRDFData brdfData;
                InitializeBRDFData(albedo, _Metallic, half3(0, 0, 0), _Smoothness, alpha, brdfData);

                // Direct BRDF
                half3 direct = DirectBDRF(brdfData, normalWS, light.direction, viewDirWS) * NdotL * light.color;

                // ── Indirect (IBL + SH) ──
                half3 indirect = half3(0, 0, 0);
                half3 ambient  = SampleSH(normalWS);

                // Diffuse IBL: SH ambient * albedo
                indirect += ambient * brdfData.diffuse;

                // Specular IBL: rough environment reflection (approximate with SH)
                half3 reflectDir = reflect(-viewDirWS, normalWS);
                half3 envSpecular = SampleSH( reflectDir ) * brdfData.specular * brdfData.roughness * 0.5;
                indirect += envSpecular;

                half3 finalColor = direct + indirect;

                // ── Hit flash ──
                finalColor = lerp(finalColor, _HitColor.rgb, _HitAmount);

                half4 color = half4(finalColor, alpha);
                color.rgb = MixFog(color.rgb, i.fogCoord);
                return color;
            }
            ENDHLSL
        }
    }
}
