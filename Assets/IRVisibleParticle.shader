
Shader "Custom/IRVisibleParticle"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _IRColor ("IR Glow Color", Color) = (0, 1, 0.3, 1)
        _IRIntensity ("IR Intensity", Range(0, 5)) = 2.0
        _NormalVisibility ("Normal Visibility", Range(0, 1)) = 0.0
    }

    Category
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        Cull Off
        Lighting Off
        ZWrite Off

        SubShader
        {
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_particles

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                fixed4 _TintColor;
                fixed4 _IRColor;
                float _IRIntensity;
                float _NormalVisibility;

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                float4 _MainTex_ST;

                v2f vert (appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.color = v.color;
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    return o;
                }

                fixed4 frag (v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                    
                    // В обычном режиме частицы почти невидимы или полностью невидимы
                    fixed4 normalColor = col * _TintColor * _NormalVisibility;
                    
                    // В ИК-режиме частицы ярко светятся
                    fixed4 irColor = col * _IRColor * _IRIntensity;
                    
                    // Смешиваем в зависимости от того, включен ли ночной режим
                    // Это будет управляться через глобальную переменную шейдера
                    float irActive = _NormalVisibility < 0.5 ? 1.0 : 0.0;
                    
                    fixed4 finalColor = lerp(normalColor, irColor, irActive);
                    
                    return finalColor;
                }
                ENDCG
            }
        }
    }
}