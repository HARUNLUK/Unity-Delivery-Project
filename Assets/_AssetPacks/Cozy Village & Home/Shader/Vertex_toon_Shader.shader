Shader "Custom/URPVertexToon"
{
    Properties
    {
        _MainColor ("Global Tint", Color) = (1,1,1,1)
        
        [KeywordEnum(Standard, Toon)] _RenderMode("Render Mode", Float) = 0
        
        [Header(Toon Settings)]
        _ToonThreshold ("Toon Threshold", Range(0.0, 1.0)) = 0.3
        _ToonSmoothness ("Toon Smoothness", Range(0.0, 0.1)) = 0.02
        _ShadowIntensity ("Shadow Darkness", Range(0.0, 1.0)) = 0.5

        [Header(Emission Settings)]
        [Toggle(_EMISSION_ON)] _UseEmission ("Enable Emission", Float) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionPower ("Emission Power", Range(0.0, 10.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _RENDERMODE_STANDARD _RENDERMODE_TOON
            #pragma shader_feature_local _EMISSION_ON
            
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 vertexColor  : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float4 shadowCoord  : TEXCOORD2;
                float4 vertexColor  : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float _ToonThreshold;
                float _ToonSmoothness;
                float _ShadowIntensity;
                
                float4 _EmissionColor;
                float _EmissionPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.vertexColor = input.vertexColor;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                
                float3 correctedVertexColor = SRGBToLinear(input.vertexColor.rgb);
                float4 baseColor = float4(correctedVertexColor, input.vertexColor.a) * _MainColor;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normal;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                Light mainLight = GetMainLight(input.shadowCoord);
                float dNL = dot(normal, mainLight.direction);
                float mainShadow = mainLight.shadowAttenuation;
                
                float3 ambient = unity_AmbientSky.rgb;
                float3 lightColor = float3(0, 0, 0);

                #if defined(_RENDERMODE_STANDARD)
                    lightColor = (saturate(dNL) * mainLight.color * mainShadow) + ambient;
                #elif defined(_RENDERMODE_TOON)
                    float toonLight = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, dNL);
                    toonLight = lerp(_ShadowIntensity, 1.0, toonLight * mainShadow);
                    lightColor = (toonLight * mainLight.color) + ambient;
                #endif

                uint pixelLightCount = GetAdditionalLightsCount();
                
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, input.positionWS);
                    float adddNL = dot(normal, addLight.direction);

                    #if defined(_RENDERMODE_STANDARD)
                        lightColor += saturate(adddNL) * addLight.color * addLight.distanceAttenuation * addLight.shadowAttenuation;
                    #elif defined(_RENDERMODE_TOON)
                        float addToonLight = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, adddNL);
                        addToonLight = lerp(_ShadowIntensity, 1.0, addToonLight) * addLight.distanceAttenuation * addLight.shadowAttenuation;
                        lightColor += addToonLight * addLight.color;
                    #endif
                LIGHT_LOOP_END

                float3 finalColor = baseColor.rgb * lightColor;

                #if defined(_EMISSION_ON)
                    float3 emission = baseColor.rgb * _EmissionColor.rgb * _EmissionPower;
                    finalColor += emission;
                #endif

                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
