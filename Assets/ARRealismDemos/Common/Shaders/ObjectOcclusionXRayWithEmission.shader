//-----------------------------------------------------------------------
// <copyright file="ObjectOcclusionXRayWithEmission.shader" company="Google LLC">
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
Shader "ARRealism/Object Occlusion X-Ray"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _MetallicGlossMap("Metallic Map", 2D) = "white" {}
        _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0)
        _Alpha("Alpha", Range(0,1)) = 0.4
        _RimPower("Rim Power", Range(0, 10)) = 3.0
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _OcclusionOffsetMeters ("Occlusion offset [meters]", Float) = 0.08
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
        #pragma surface surf Standard fullforwardshadows alpha:blend

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_MetallicGlossMap;
            float2 uv_EmissionMap;
            float3 viewDir;
            float3 worldPos;
            float3 worldNormal; INTERNAL_DATA
            float4 screenPos;
        };

        uniform sampler2D _MainTex;
        uniform sampler2D _BumpMap;
        uniform sampler2D _MetallicGlossMap;
        uniform sampler2D _EmissionMap;
        uniform half _Glossiness;
        uniform half _Metallic;
        uniform fixed4 _Color;
        uniform half _Alpha;
        uniform half _RimPower;
        uniform fixed4 _RimColor;
        uniform fixed4 _EmissionColor;

        // Tests if the current pixel is occluded by the depth data.
        bool IsOccluded(in float4 screenPos, in float3 worldPos)
        {
            // Gets screen pixel coordinates to lookup depth texture value.
            float2 screenUV = screenPos.xy / screenPos.w;
            float2 uv = ArCoreDepth_GetUv(screenUV);

            // Unpacks depth texture distance.
            float realDepth = ArCoreDepth_GetMeters(uv);

            // Finds distance to the 3D point along the principal axis.
            float virtualDepth = -UnityWorldToViewPos(worldPos).z - _OcclusionOffsetMeters;

            // Discards if object is obscured behind the depth texture (physical environment).
            return virtualDepth > realDepth;
        }

        // Simple x-ray shader example.
        bool XRayShading(Input IN, inout SurfaceOutputStandard o)
        {
            // Texture to grey-scale.
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = Luminance(c.rgb);

            // Sets rim lighting.
            half rim = 1.0 - saturate(dot (normalize(IN.viewDir), IN.worldNormal));
            o.Emission = _RimColor.rgb * pow (rim, _RimPower);

            // Sets transparency.
            o.Alpha = _Alpha;

            return true;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            if (IsOccluded(IN.screenPos, IN.worldPos.xyz))
            {
                XRayShading(IN, o);
            }
            else
            {
                // Albedo comes from a texture tinted by color
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = c.rgb;
                o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
                o.Emission = tex2D(_EmissionMap,  IN.uv_EmissionMap) * _EmissionColor;
                // Metallic and smoothness come from slider variables
                o.Metallic = tex2D(_MetallicGlossMap,  IN.uv_MetallicGlossMap) * _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = c.a * _Color.a;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
