Shader "Hidden/NightVision"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _IRTint ("IR Tint", Color) = (0, 1, 0.3, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1.2
        _Noise ("Noise Amount", Range(0, 1)) = 0.1
        _Vignette ("Vignette", Range(0, 1)) = 0.3
        
        // Параметры сканирующих линий
        _ScanlineCount ("Scanline Count", Range(100, 1000)) = 400
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.15
        _ScanSpeed ("Scan Speed", Range(0, 5)) = 2.0
        _ScanlineThickness ("Scanline Thickness", Range(0.001, 0.01)) = 0.003
        _ScanlineBrightness ("Scanline Brightness", Range(0, 2)) = 1.5
    }
    
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

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
            float4 _IRTint;
            float _Intensity;
            float _Noise;
            float _Vignette;
            float _ScanlineCount;
            float _ScanlineIntensity;
            float _ScanSpeed;
            float _ScanlineThickness;
            float _ScanlineBrightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Функция для генерации шума
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Функция для создания горизонтальных линий сканирования
            float scanlines(float2 uv, float count)
            {
                return sin(uv.y * count * 3.14159);
            }

            // Функция для движущейся сканирующей линии
            float movingScanline(float2 uv, float time, float speed)
            {
                // Позиция сканирующей линии (движется сверху вниз)
                float scanPos = frac(time * speed * 0.1);
                
                // Расстояние от текущей позиции до сканирующей линии
                float dist = abs(uv.y - scanPos);
                
                // Создаем яркую линию с градиентом
                float scanLine = smoothstep(_ScanlineThickness * 2, 0, dist);
                
                // Добавляем свечение вокруг линии
                float glow = exp(-dist * 100) * 0.5;
                
                return scanLine * _ScanlineBrightness + glow;
            }

            // Функция для статических горизонтальных линий (имитация CRT)
            float staticScanlines(float2 uv)
            {
                float scanLine = sin(uv.y * _ScanlineCount * 3.14159) * 0.5 + 0.5;
                return lerp(1.0, scanLine, _ScanlineIntensity);
            }

            // Функция для эффекта мерцания
            float flicker(float time)
            {
                return 1.0 + sin(time * 30.0) * 0.02 + sin(time * 17.5) * 0.01;
            }

            // Функция для искажения (имитация помех)
            float2 distortion(float2 uv, float time)
            {
                float distortAmount = 0.002;
                uv.x += sin(uv.y * 10.0 + time * 5.0) * distortAmount;
                return uv;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                
                // Применяем небольшое искажение
                float2 distortedUV = i.uv;
                
                // Получаем исходный цвет
                fixed4 col = tex2D(_MainTex, distortedUV);
                
                // Конвертируем в оттенки серого (яркость)
                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                
                // Применяем ИК-тинт
                fixed4 nightVision = fixed4(_IRTint.rgb * luminance * _Intensity, 1);
                
                // Добавляем статические горизонтальные линии (имитация CRT монитора)
                float staticLines = staticScanlines(i.uv);
                nightVision.rgb *= staticLines;
                
                // Добавляем движущуюся сканирующую линию
                float movingLineEffect = movingScanline(i.uv, time, _ScanSpeed);
                nightVision.rgb += _IRTint.rgb * movingLineEffect * 0.3;
                
                // Добавляем шум для эффекта аналоговой камеры
                float noise = rand(i.uv * time) * _Noise;
                nightVision.rgb += noise;
                
                // Добавляем эффект мерцания
                nightVision.rgb *= flicker(time);
                
                // Добавляем виньетирование (затемнение по краям)
                float2 center = i.uv - 0.5;
                float vignette = 1.0 - dot(center, center) * _Vignette;
                nightVision.rgb *= vignette;
                
                // Добавляем bloom для ярких областей
                if (luminance > 0.7)
                {
                    nightVision.rgb += _IRTint.rgb * (luminance - 0.7) * 0.5;
                }
                
                // Добавляем случайные горизонтальные помехи (glitch эффект)
                float glitchLine = step(0.998, rand(float2(floor(i.uv.y * 100), time * 10)));
                nightVision.rgb += glitchLine * _IRTint.rgb * 0.3;
                
                return nightVision;
            }
            ENDCG
        }
    }
}