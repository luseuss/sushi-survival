// UI 이미지(RawImage)에 "렌즈 왜곡 + 방사형 블러"를 입히는 셰이더 — 장면 전환에서 화면이 렌즈 속으로 빨려 들어가는 느낌을 낸다.
// - 왜곡(_Distortion): 가장자리일수록 가운데 쪽 그림을 끌어와 바깥이 방사형으로 늘어난다(핀쿠션, 터널로 빨려 드는 모양).
// - 블러(_Blur): 화면 가운데를 향한 방향으로 번지는 줌 블러. 가장자리일수록 많이 번진다.
// - 색수차(_Aberration): 가장자리에서 R/G/B가 살짝 갈라진다.
// 세기는 SceneTransition이 매 프레임 올린다. 프로젝트의 기존 셰이더(SpriteFlash)처럼 Resources에 두고 Resources.Load로 쓴다.
Shader "SushiSurvival/LensDistortion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        // 0이면 왜곡 없음, 클수록 가장자리가 강하게 늘어난다.
        _Distortion ("Distortion", Range(0, 4)) = 0
        // 줌 블러 세기(0이면 선명). 0.1이면 가장자리가 반지름의 10%만큼 번진다.
        _Blur ("Radial Blur", Range(0, 0.5)) = 0
        // 가장자리 색 갈라짐 정도(0이면 없음).
        _Aberration ("Chromatic Aberration", Range(0, 0.2)) = 0.04
        // 화면 가로/세로 비율(원이 찌그러지지 않게 보정). SceneTransition이 채운다.
        _Aspect ("Aspect (width/height)", Float) = 1.7778
    }

    SubShader
    {
        Tags
        {
            "Queue"="Overlay"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // 방사형 블러에서 한 픽셀당 읽는 횟수.
            #define BLUR_TAPS 10

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Distortion;
            float _Blur;
            float _Aberration;
            float _Aspect;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // 화면 가운데를 (0,0)으로 한 좌표에서, 가장자리일수록 가운데 쪽 그림을 끌어와 바깥을 늘린다.
            // 1 + strength * r2로 나누면 어떤 세기에서도 원본 범위 안에 머물고 뒤집히지 않는다.
            float2 lens(float2 uv, float strength)
            {
                float2 p = (uv - 0.5) * float2(_Aspect, 1.0);
                float r2 = dot(p, p);
                p /= 1.0 + strength * r2;
                p.x /= _Aspect;
                return p + 0.5;
            }

            // 가운데로 향하는 선을 따라 BLUR_TAPS번 읽어 평균을 낸다(줌 블러).
            fixed3 blurSample(float2 uvR, float2 uvG, float2 uvB)
            {
                float3 sum = 0;
                for (int i = 0; i < BLUR_TAPS; i++)
                {
                    float s = 1.0 - _Blur * (i / (float)(BLUR_TAPS - 1));
                    sum.r += tex2D(_MainTex, 0.5 + (uvR - 0.5) * s).r;
                    sum.g += tex2D(_MainTex, 0.5 + (uvG - 0.5) * s).g;
                    sum.b += tex2D(_MainTex, 0.5 + (uvB - 0.5) * s).b;
                }
                return sum / BLUR_TAPS;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 색수차: 채널마다 왜곡 세기를 조금씩 달리해 가장자리에서 R/G/B가 갈라진다.
                float d = _Distortion;
                float2 uvR = lens(IN.uv, d * (1.0 + _Aberration));
                float2 uvG = lens(IN.uv, d);
                float2 uvB = lens(IN.uv, d * (1.0 - _Aberration));

                fixed4 c;
                c.rgb = blurSample(uvR, uvG, uvB);
                c.a = 1.0;
                return c * IN.color;
            }
            ENDCG
        }
    }
}
