//-----------------------------------------------------------------------
// <copyright file="WaterOcclusionShader.shader" company="Google LLC">
//
// Copyright 2020 Google LLC. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

Shader "ARRealism/Water Occlusion Shader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _WaveEdgeColor ("Wave Tip Color", Color) = (1,1,1,1)
        _WaveTipColor ("Wave Tip Color", Color) = (1,1,1,1)
        _WaveBottomColor ("Wave Bottom Color", Color) = (.1,.1,.2,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        [Space]
        // The size of screen-space alpha blending between visible and occluded regions.
        _OcclusionBlendingScale ("Occlusion blending scale", Range(0, 1)) = 0.01
        // The bias added to the estimated depth. Useful to avoid occlusion of objects anchored to planes.
        _OcclusionOffsetMeters ("Occlusion offset [meters]", Float) = 0

        _WaterBase ("Water base [meters]", Float) = 0
        _WaveHeight ("Wave height [meters]", Float) = 0.5

        [Space]
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Enabled("Enable the rendering process", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        LOD 200

        Pass
        {
            ZWrite On
            ColorMask 0
        }

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:blend vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.5

        #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

        struct Input
        {
            float2 uv_MainTex;
            float4 screenPos;
            float3 worldPos;
            float4 vertColor;
        };

        uniform sampler2D _MainTex;
        uniform half _Glossiness;
        uniform half _Metallic;
        uniform half _WaterBase;
        uniform half _WaveHeight;
        uniform fixed4 _Color;
        uniform fixed4 _WaveEdgeColor;
        uniform fixed4 _WaveTipColor;
        uniform fixed4 _WaveBottomColor;
        uniform half _Enabled;

#if UNITY_VERSION >= 201701
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
#endif

        float random (float2 uv)
        {
            return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
        }

        float hash(float n)
        {
            return frac(sin(n) * 43758.5453);
        }

        // The noise function returns a value in the range [-1f, 1f].
        float noise(float3 x)
        {
            float3 p = floor(x);
            float3 f = frac(x);

            f = f * f * (3.0 - 2.0 * f);
            float n = p.x + p.y * 57.0 + 113.0 * p.z;

            return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                             lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                        lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                             lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y),
                        f.z);
        }

        inline float ArCoreGetSampleFoam(float2 uv, float3 worldPos, float foamThickness)
        {
            float virtualDepth = -UnityWorldToViewPos(worldPos).z;
            float realDepth = ArCoreDepth_GetMeters(uv);
            float signedDiffMeters = realDepth - virtualDepth;
            return saturate(signedDiffMeters / foamThickness);
        }

        float remap(float value, float low1, float high1, float low2, float high2)
        {
            return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vertColor = v.color;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            if (_Enabled < 1.0) {
                o.Albedo = fixed4(0, 0, 0, 0);
                return;
            }

            // Screen pixel coordinate, to lookup depth texture value.
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            float2 uv = ArCoreDepth_GetUv(screenUV);
            float occlusionBlending =
                ArCoreDepth_GetVisibility(uv, UnityWorldToViewPos(IN.worldPos));
            float occlusionFoam = 1.0 - ArCoreGetSampleFoam(uv, IN.worldPos, 0.25);

            // Breaks up the edge foam.
            fixed3 edgeOffset = fixed3(0.0, (_Time.x), 0.0);
            float edgeNoise = noise((IN.worldPos + edgeOffset) * 15.0f);
            occlusionFoam = smoothstep(0.0, 0.25, edgeNoise * occlusionFoam);

            // Original texture color.
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Computes darkening and wave tip factor based on wave height in world space.
            float tipFactor = smoothstep(0.9, 1.0, IN.vertColor.r);
            float darkenFactor = smoothstep(0.0, 0.2, IN.vertColor.r);
            float deepAlphaFactor = 0.9 + (smoothstep(0.4, 0.45, IN.vertColor.r) * 0.1);

            // Final water color.
            fixed3 darkenedWater = lerp(_WaveBottomColor, c.rgb, darkenFactor);

            // Final color mixing with noise water tips.
            fixed3 tipColor = lerp(darkenedWater, _WaveTipColor, tipFactor);
            fixed3 edgeColor = lerp(tipColor, _WaveEdgeColor, occlusionFoam);

            // Fades water by distance.
            float virtualDepth = -UnityWorldToViewPos(IN.worldPos).z;
            float fadeAway = 1.0 - smoothstep(2.5, 3.5, virtualDepth);

            o.Albedo.rgb = edgeColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a * occlusionBlending * deepAlphaFactor * fadeAway;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
