Shader "Rouge/Character"
{
    Properties
    {
        [HDR] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        [HDR] _HitColor ("Hit Flash Color", Color) = (1, 1, 1, 1)
        _HitAmount ("Hit Flash Amount", Range(0, 1)) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float  fogCoord   : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _HitColor;
            half  _HitAmount;
            half  _Metallic;
            half  _Smoothness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs posInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrmInput  = GetVertexNormalInputs(input.normalOS);

                o.positionCS = posInput.positionCS;
                o.positionWS = posInput.positionWS;
                o.normalWS   = nrmInput.normalWS;
                o.fogCoord   = ComputeFogFactor(posInput.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half3  normal   = normalize(i.normalWS);
                half3  viewDir  = normalize(GetCameraPositionWS() - i.positionWS);
                Light  light    = GetMainLight();
                half   NdotL    = max(0, dot(normal, light.direction));

                // Indirect (SH ambient)
                half3 ambient = SampleSH(normal);

                // Diffuse
                half3 diffuse  = (ambient + light.color * NdotL) * _BaseColor.rgb;

                // Simple Blinn-Phong specular
                half3 halfVec  = normalize(light.direction + viewDir);
                half  NdotH    = max(0, dot(normal, halfVec));
                half  spec     = pow(NdotH, lerp(1, 100, _Smoothness)) * _Metallic;
                half3 specular = light.color * spec * _BaseColor.rgb;

                half3 finalColor = diffuse + specular;

                // Hit flash: lerp toward hit color
                finalColor = lerp(finalColor, _HitColor.rgb, _HitAmount);

                half4 color = half4(finalColor, _BaseColor.a);
                color.rgb = MixFog(color.rgb, i.fogCoord);
                return color;
            }
            ENDHLSL
        }
    }
}
