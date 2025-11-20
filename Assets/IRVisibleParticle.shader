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
        Blend SrcAlpha One
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
                    // Получаем текстуру частицы
                    fixed4 texColor = tex2D(_MainTex, i.texcoord);
                    
                    // КРИТИЧНО: Если NormalVisibility близка к 0, делаем частицу полностью прозрачной
                    if (_NormalVisibility < 0.01)
                    {
                        // В обычном режиме возвращаем полностью прозрачный цвет
                        return fixed4(0, 0, 0, 0);
                    }
                    else
                    {
                        // В ИК-режиме (_NormalVisibility устанавливается в 1)
                        // частицы ярко светятся
                        fixed4 irColor = texColor * i.color * _IRColor * _IRIntensity;
                        return irColor;
                    }
                }
                ENDCG
            }
        }
    }
}