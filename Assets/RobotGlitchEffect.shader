Shader "Custom/RobotGlitchEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.1
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0
        _ColorSplit ("Color Split", Range(0, 0.1)) = 0.01
        _GlitchColor ("Glitch Color", Color) = (0, 0.8, 1, 1)
        _WarningColor ("Warning Color", Color) = (1, 0.3, 0, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _GlitchIntensity;
            float _ScanlineIntensity;
            float _NoiseIntensity;
            float _ColorSplit;
            float4 _GlitchColor;
            float4 _WarningColor;

            // Псевдослучайное число
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Глитч-смещение
                float glitchOffset = 0;
                if (_GlitchIntensity > 0)
                {
                    float time = _Time.y * 10;
                    float lineNoise = rand(float2(floor(uv.y * 50), floor(time)));
                    
                    if (lineNoise > 1 - _GlitchIntensity * 0.3)
                    {
                        glitchOffset = (rand(float2(time, uv.y)) - 0.5) * _GlitchIntensity * 0.1;
                    }
                }
                
                uv.x += glitchOffset;
                
                // Базовый цвет
                fixed4 col = tex2D(_MainTex, uv);
                
                // Разделение цветовых каналов (хроматическая аберрация)
                if (_GlitchIntensity > 0)
                {
                    float split = _ColorSplit * _GlitchIntensity;
                    col.r = tex2D(_MainTex, uv + float2(split, 0)).r;
                    col.b = tex2D(_MainTex, uv - float2(split, 0)).b;
                }
                
                // Сканлайны
                if (_ScanlineIntensity > 0)
                {
                    float scanline = sin(uv.y * 800) * 0.5 + 0.5;
                    col.rgb *= 1 - (scanline * _ScanlineIntensity * 0.3);
                }
                
                // Шум
                if (_NoiseIntensity > 0)
                {
                    float noise = rand(uv + _Time.y);
                    col.rgb = lerp(col.rgb, float3(noise, noise, noise), _NoiseIntensity * 0.5);
                }
                
                // Цветовой оттенок глитча
                if (_GlitchIntensity > 0.3)
                {
                    float glitchMix = (_GlitchIntensity - 0.3) / 0.7;
                    float4 glitchTint = lerp(_GlitchColor, _WarningColor, sin(_Time.y * 5) * 0.5 + 0.5);
                    col.rgb = lerp(col.rgb, col.rgb * glitchTint.rgb, glitchMix * 0.5);
                }
                
                // Финальная альфа
                col.a *= _GlitchIntensity;
                
                return col;
            }
            ENDCG
        }
    }
}
