Shader "Custom/URP_Painterly"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        [MainTexture] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Normal] _Normal("Normal", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(-2,2)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        [HDR]_SpecularColor("Specular Color", Color) = (1,1,1,1)

        _ShadingGradient("Shading Gradient", 2D) = "white" {}
        _PainterlyGuide("Painterly Guide", 2D) = "white" {}
        _PainterlySmoothness("Painterly Smoothness", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode"="UniversalForward"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _BaseColor;
            sampler2D _Normal;
            float _NormalStrength;
            sampler2D _PainterlyGuide;
            sampler2D _ShadingGradient;
            float _PainterlySmoothness;
            float4 _SpecularColor;
            float _Metallic;
            float _Smoothness;

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(o.positionWS);

                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.tangentWS = tangentWS;
                o.bitangentWS = cross(o.normalWS, tangentWS) * v.tangentOS.w;

                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Textures
                float4 albedo = tex2D(_MainTex, i.uv) * _BaseColor;
                float3 painterlyGuide = tex2D(_PainterlyGuide, i.uv).rgb;
                float3 normalTS = UnpackNormal(tex2D(_Normal, i.uv));
                normalTS.xy *= _NormalStrength;
                normalTS.z = sqrt(saturate(1.0 - dot(normalTS.xy, normalTS.xy)));

                // Transform normal to world
                float3x3 TBN = float3x3(normalize(i.tangentWS), normalize(i.bitangentWS), normalize(i.normalWS));
                float3 normalWS = normalize(mul(normalTS, TBN));

                // Lighting
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));

                // Painterly diffuse
                float nDotL = saturate(dot(normalWS, lightDir) + 0.2);
                float diffMask = smoothstep(painterlyGuide.r - _PainterlySmoothness, painterlyGuide.r + _PainterlySmoothness, nDotL);
                float3 gradientCol = tex2D(_ShadingGradient, float2(diffMask,0)).rgb;

                // Specular
                float3 refl = reflect(-lightDir, normalWS);
                float vDotRefl = dot(viewDir, refl);
                float specThresh = painterlyGuide.r + _Smoothness;
                float3 specular = _SpecularColor.rgb * smoothstep(specThresh - _PainterlySmoothness, specThresh + _PainterlySmoothness, vDotRefl) * _Smoothness;

                // Final color
                float3 col = albedo.rgb * gradientCol * mainLight.color + specular;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
