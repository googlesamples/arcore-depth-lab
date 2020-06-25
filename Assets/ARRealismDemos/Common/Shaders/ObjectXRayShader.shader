//-----------------------------------------------------------------------
// <copyright file="ObjectXRayShader.shader" company="Google LLC">
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

Shader "ARRealism/Object X-Ray Shader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Alpha("Alpha", Range(0,1)) = 0.4
        _RimPower("Rim Power", Range(0, 10)) = 3.0
        _RimColor("Rim Color", Color) = (1,1,1,1)
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
            float3 viewDir;
            float3 worldPos;
            float3 worldNormal; INTERNAL_DATA
            float4 screenPos;
        };

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _Alpha;
        half _RimPower;
        fixed4 _RimColor;

        // Test if the current pixel is occluded by the depth data.
        bool IsOccluded(float4 screenPos, float3 worldPos)
        {
            // Screen pixel coordinate, to lookup depth texture value.
            float2 screenUV = screenPos.xy / screenPos.w;
            float2 uv = ArCoreDepth_GetUv(screenUV);

            // Unpack depth texture distance.
            float realDepth = ArCoreDepth_GetMeters(uv);

            // Find distance to the 3D point along the principal axis.
            float virtualDepth = -UnityWorldToViewPos(worldPos).z;

            // Discard if object is obscured behind the depth texture.
            return virtualDepth > realDepth;
        }

        // Simple x-ray shader example.
        bool XRayShading(Input IN, inout SurfaceOutputStandard o)
        {
            // Texture to grey-scale.
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = Luminance(c.rgb);

            // Rim lighting.
            half rim = 1.0 - saturate(dot (normalize(IN.viewDir), IN.worldNormal));
            o.Emission = _RimColor.rgb * pow (rim, _RimPower);

            // Fade.
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
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = c.a * _Color.a;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
