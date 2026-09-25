Shader "Vedanta/PotLeakage_Highlight"
{
    Properties
    {
        _HighlightColor ("Highlight Color", Color) = (0.0, 0.8, 1.0, 0.5)
        _EmissionColor ("Emission Color", Color) = (0.0, 0.6, 1.0, 1.0)
        _EmissionIntensity ("Emission Intensity", Range(0.0, 10.0)) = 2.0
        _RimPower ("Rim Power", Range(0.1, 8.0)) = 2.5
        _PulseSpeed ("Pulse Speed", Range(0.0, 10.0)) = 2.0
        _PulseMin ("Pulse Minimum", Range(0.0, 1.0)) = 0.4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "HighlightPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _HighlightColor;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _RimPower;
                float _PulseSpeed;
                float _PulseMin;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                float NdotV = saturate(dot(normalWS, viewDirWS));
                float rim = 1.0 - NdotV;
                rim = pow(rim, _RimPower);

                float pulse = 1.0;
                if (_PulseSpeed > 0.001)
                {
                    pulse = _PulseMin + (1.0 - _PulseMin) * (0.5 * (sin(_Time.y * _PulseSpeed) + 1.0));
                }

                half4 finalColor = _HighlightColor;
                finalColor.rgb += _EmissionColor.rgb * _EmissionIntensity * rim * pulse;
                finalColor.a = saturate(_HighlightColor.a + rim * 0.5) * pulse;

                return finalColor;
            }
            ENDHLSL
        }
    }
}
