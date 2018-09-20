Shader "Hidden/Shadow Mapping in 2D"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always

            CGPROGRAM
            #pragma vertex vertex
            #pragma fragment fragment

            Texture2D<float4> _MainTex;
            Texture2D<int> _ShadowMap;

            SamplerState _LinearClampSampler;

            struct Input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vertex(in Input input)
            {
                Varyings output;

                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;

                return output;
            }

            float4 fragment(in Varyings input) : SV_Target
            {
                float2 dimensions = 1.;
                _ShadowMap.GetDimensions(dimensions.x, dimensions.y);

                float2 uv = input.uv - .5;
                uv.x *= _ScreenParams.x * (_ScreenParams.w - 1.);

                float angle = atan2(uv.y, uv.x) * .318309;
                angle = angle * .5 + .5;

                uint3 coordinates = uint3((uint) (angle * dimensions.x), 0, 0);
                float depth = _ShadowMap.Load(coordinates).r * .000244140625;

                float4 color =
                    _MainTex.SampleLevel(_LinearClampSampler, input.uv, 0.);

                float4 shadow = color;

                float selector = step(length(uv), depth);

                shadow.rgb = shadow.rgb * selector + color.rgb * .3 *
                    step(selector, 0.);

                return lerp(color, shadow, step(color.a, .125));
            }
            ENDCG
        }

        Pass
        {
            Cull Off ZWrite Off ZTest Always

            CGPROGRAM
            #pragma vertex vertex
            #pragma fragment fragment

            Texture2D<float4> _MainTex;
            SamplerState _PointClampSampler;

            struct Input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vertex(in Input input)
            {
                Varyings output;

                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;

                return output;
            }

            float fragment(in Varyings input) : SV_Target
            {
                float alpha =
                    _MainTex.SampleLevel(_PointClampSampler, input.uv, 0.).a;

                return step(alpha, 0.);
            }
            ENDCG
        }
    }
}
