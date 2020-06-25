//-----------------------------------------------------------------------
// <copyright file="SplatShader.shader" company="Google LLC">
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

Shader "ARRealism/SplatShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _RimPower ("Rim Power", Range(0.5,8.0)) = 3.0

        [Space]
        // The size of screen-space alpha blending between visible and occluded regions.
        _OcclusionBlendingScale ("Occlusion blending scale", Range(0, 1)) = 0.01

        // The bias added to the estimated depth. Useful to avoid occlusion of objects anchored to planes.
        _OcclusionOffsetMeters ("Occlusion offset [meters]", Float) = 0

        [Space]
        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        _Metallic ("Metallic", Range(0,1)) = 0.0
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
        // Physically-based Standard lighting model, and enables shadows on all light types.
        #pragma surface surf Standard fullforwardshadows alpha:blend

        // Uses shader model 3.0 target for better lighting.
        #pragma target 3.0

        #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float4 screenPos;
            float3 worldPos;
            float3 viewDir;
        };

        sampler2D _MainTex;
        sampler2D _BumpMap;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _RimPower;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Screen pixel coordinate, to lookup depth texture value.
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            float2 uv = ArCoreDepth_GetUv(screenUV);
            float occlusionBlending =
                ArCoreDepth_GetVisibility(uv, UnityWorldToViewPos(IN.worldPos));

            // Default surface shader behavior.
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo.rgb = c.rgb;
            o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a * occlusionBlending;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            o.Emission = o.Albedo.rgb * pow (rim, _RimPower);
        }
        ENDCG
    }

    FallBack "Diffuse"
}
