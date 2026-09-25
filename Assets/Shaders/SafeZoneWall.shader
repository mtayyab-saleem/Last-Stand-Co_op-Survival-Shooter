// Safe zone wall: a translucent energy curtain.
// Strongest at the bottom, fading with height, with bright bands drifting upwards so it
// reads as a moving barrier rather than a flat tinted pane. Unlit, no depth write, both
// faces drawn so it looks the same from inside and outside the zone. Fog is ignored on
// purpose: the wall must stay visible across the whole map.
Shader "LastStand/SafeZoneWall"
{
    Properties
    {
        _Color ("Wall Colour", Color) = (0.12, 0.45, 1.0, 0.45)
        _BandColor ("Band Colour", Color) = (0.65, 0.9, 1.0, 1.0)
        _MinAlpha ("Alpha At Top (x wall alpha)", Range(0, 1)) = 0.3
        _FadeHeight ("Fade Height (m)", Float) = 90
        _BandSpacing ("Band Spacing (m)", Float) = 7
        _BandWidth ("Band Width", Range(0.02, 0.5)) = 0.12
        _BandSpeed ("Band Speed", Float) = 0.35
        _BandStrength ("Band Strength", Range(0, 1)) = 0.55
        _GridSpacing ("Vertical Line Spacing (m)", Float) = 9
        _GridStrength ("Vertical Line Strength", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "SafeZoneWall"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _BandColor;
                half _MinAlpha;
                float _FadeHeight;
                float _BandSpacing;
                half _BandWidth;
                float _BandSpeed;
                half _BandStrength;
                float _GridSpacing;
                half _GridStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float height : TEXCOORD0;   // metres above the bottom of the wall
                float arc : TEXCOORD1;      // metres around the wall
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // The wall mesh is a unit-wide tube from y = -1 to 1, scaled by the
                // controller, so world metres come from the object's scale.
                float scaleY = length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y));
                float radius = 0.5 * length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x));

                output.height = (input.positionOS.y + 1.0) * scaleY;
                output.arc = atan2(input.positionOS.z, input.positionOS.x) * radius;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float fade = saturate(1.0 - input.height / max(_FadeHeight, 0.01));

                // Horizontal bands drifting up the wall.
                float band = frac(input.height / max(_BandSpacing, 0.01) - _Time.y * _BandSpeed);
                half bands = smoothstep(_BandWidth, 0.0, min(band, 1.0 - band)) * _BandStrength;

                // Faint vertical lines give the curve some shape up close.
                float column = frac(input.arc / max(_GridSpacing, 0.01));
                half lines = smoothstep(0.06, 0.0, min(column, 1.0 - column)) * _GridStrength;

                half glow = saturate(bands + lines);
                half3 colour = lerp(_Color.rgb, _BandColor.rgb, glow);
                half alpha = _Color.a * lerp(_MinAlpha, 1.0, fade) + glow * _BandColor.a * lerp(0.35, 1.0, fade);

                return half4(colour, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
