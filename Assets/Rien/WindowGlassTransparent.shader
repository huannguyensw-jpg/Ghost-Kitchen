Shader "Rien/Window Glass Transparent"
{
    Properties
    {
        [HDR] _BaseColor("Glass Tint", Color) = (0.58, 0.82, 0.95, 1)
        _Opacity("Opacity", Range(0, 1)) = 0.18
        _Smoothness("Smoothness", Range(0, 1)) = 0.95
        _FresnelPower("Edge Power", Range(0.5, 8)) = 4
        _FresnelStrength("Edge Reflection", Range(0, 1)) = 0.38
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Opacity;
                half _Smoothness;
                half _FresnelPower;
                half _FresnelStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 viewDirectionWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.viewDirectionWS =
                    GetWorldSpaceViewDir(positionInputs.positionWS);
                output.shadowCoord = GetShadowCoord(positionInputs);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                normalWS *= isFrontFace ? 1.0h : -1.0h;

                half3 viewDirectionWS = normalize(input.viewDirectionWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 lightDirectionWS = normalize(mainLight.direction);
                half3 halfDirectionWS =
                    normalize(lightDirectionWS + viewDirectionWS);

                half directLight = saturate(dot(normalWS, lightDirectionWS));
                half specularPower = lerp(16.0h, 256.0h, _Smoothness);
                half specular = pow(
                    saturate(dot(normalWS, halfDirectionWS)),
                    specularPower
                );

                half fresnel = pow(
                    1.0h - saturate(dot(normalWS, viewDirectionWS)),
                    _FresnelPower
                );

                half3 glassColor = _BaseColor.rgb;
                half3 ambient = glassColor * 0.28h;
                half3 diffuse = glassColor * mainLight.color *
                    (0.12h + directLight * 0.3h) *
                    mainLight.shadowAttenuation;
                half3 reflection = mainLight.color * specular *
                    lerp(0.15h, 1.0h, _Smoothness);
                reflection += fresnel * _FresnelStrength *
                    lerp(glassColor, half3(1, 1, 1), 0.65h);

                half3 finalColor = ambient + diffuse + reflection;
                finalColor = MixFog(finalColor, input.fogFactor);

                half alpha = saturate(
                    _Opacity * _BaseColor.a +
                    fresnel * _FresnelStrength
                );

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
