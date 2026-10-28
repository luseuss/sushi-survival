// UI 이미지(RawImage)에 "렌즈 왜곡"을 입히는 셰이더 — 장면 전환에서 화면이 렌즈 속으로 빨려 들어가는 느낌을 낸다.
// 화면 가운데는 부풀고(볼록 렌즈) 가장자리는 늘어나며 모여들고, 가장자리 색이 R/G/B로 살짝 갈라지고(색수차),
// 렌즈 밖으로 나간 모서리는 검게 비워 터널처럼 보인다. 세기는 SceneTransition이 _Distortion으로 매 프레임 올린다.
// 프로젝트의 기존 셰이더(SpriteFlash)처럼 Resources에 두고 Resources.Load로 쓴다.
Shader "SushiSurvival/LensDistortion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        // 0이면 왜곡 없음, 클수록 가장자리가 강하게 휜다.
        _Distortion ("Distortion", Range(0, 3)) = 0
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

            // 화면 가운데를 (0,0)으로 한 좌표 p에서, 볼록 렌즈로 휜 원본 좌표를 구한다.
            // 반지름 r2가 클수록(가장자리) 더 먼 곳의 그림을 끌어와서 가장자리가 눌리고 가운데가 부푼다.
            float2 lens(float2 uv, float strength)
            {
                float2 p = (uv - 0.5) * float2(_Aspect, 1.0);
                float r2 = dot(p, p);
                p *= 1.0 + strength * r2;
                p.x /= _Aspect;
                return p + 0.5;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 색수차: 채널마다 왜곡 세기를 조금씩 달리해 가장자리에서 R/G/B가 갈라진다.
                float d = _Distortion;
                float2 uvR = lens(IN.uv, d * (1.0 + _Aberration));
                float2 uvG = lens(IN.uv, d);
                float2 uvB = lens(IN.uv, d * (1.0 - _Aberration));

                fixed4 c;
                c.r = tex2D(_MainTex, uvR).r;
                c.g = tex2D(_MainTex, uvG).g;
                c.b = tex2D(_MainTex, uvB).b;
                c.a = 1.0;

                // 원본 밖(렌즈 밖)으로 나간 곳은 검게.
                float2 inside = step(0.0, uvG) * step(uvG, 1.0);
                c.rgb *= inside.x * inside.y;

                return c * IN.color;
            }
            ENDCG
        }
    }
}
