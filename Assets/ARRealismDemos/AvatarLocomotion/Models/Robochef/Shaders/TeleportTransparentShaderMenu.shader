//-----------------------------------------------------------------------
// <copyright file="TeleportTransparentShaderMenu.shader" company="Google LLC">
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

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X
Shader "Google Occlusion/Robot Teleport Menu"
{
    Properties
    {
        _AlbedoSmoothness("Albedo Smoothness", 2D) = "black" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _Cutoff("Mask Clip Value", Float) = 0.5
        _Occlusion("Occlusion", 2D) = "white" {}
        [HDR]_Emission("Emission", 2D) = "white" {}
        [HDR]_Teleport("Teleport", 2D) = "white" {}
        _TeleportPhase("Teleport Phase", Range(0 , 1)) = 1
        [HDR]_TeleportColor("Teleport Color", Color) = (1,1,1,0)
        _TeleportGlowSize("Teleport Glow Size", Range(0 , 0.5)) = 0.1
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }

        SubShader
        {
            Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
            Cull Back
            CGPROGRAM
            #pragma target 3.0
            #pragma surface surf Standard keepalpha addshadow fullforwardshadows
            struct Input
            {
                float2 uv_texcoord;
            };

            uniform sampler2D _NormalMap;
            uniform float4 _NormalMap_ST;
            uniform sampler2D _AlbedoSmoothness;
            uniform float4 _AlbedoSmoothness_ST;
            uniform sampler2D _Teleport;
            uniform float4 _Teleport_ST;
            uniform float _TeleportPhase;
            uniform float4 _TeleportColor;
            uniform sampler2D _Emission;
            uniform float4 _Emission_ST;
            uniform float _TeleportGlowSize;
            uniform sampler2D _Occlusion;
            uniform float4 _Occlusion_ST;
            uniform float _Cutoff = 0.5;

            void surf(Input i , inout SurfaceOutputStandard o)
            {
                float2 uv_NormalMap = i.uv_texcoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
                o.Normal = UnpackNormal(tex2D(_NormalMap, uv_NormalMap));
                float2 uv_AlbedoSmoothness = i.uv_texcoord * _AlbedoSmoothness_ST.xy + _AlbedoSmoothness_ST.zw;
                float4 tex2DNode1 = tex2D(_AlbedoSmoothness, uv_AlbedoSmoothness);
                float2 uv_Teleport = i.uv_texcoord * _Teleport_ST.xy + _Teleport_ST.zw;
                float4 tex2DNode5 = tex2D(_Teleport, uv_Teleport);
                float temp_output_48_0 = saturate(_TeleportPhase);
                float temp_output_42_0 = saturate((tex2DNode5.r - temp_output_48_0) * 1000);
                float4 lerpResult27 = lerp(float4(0,0,0,0) , tex2DNode1 , temp_output_42_0);
                o.Albedo = lerpResult27.rgb;
                float2 uv_Emission = i.uv_texcoord * _Emission_ST.xy + _Emission_ST.zw;
                float4 lerpResult26 = lerp(_TeleportColor, tex2D(_Emission, uv_Emission), saturate(tex2DNode5.r * (1.0 - _TeleportGlowSize) / temp_output_48_0));
                o.Emission = lerpResult26.rgb;
                o.Smoothness = tex2DNode1.a;
                float2 uv_Occlusion = i.uv_texcoord * _Occlusion_ST.xy + _Occlusion_ST.zw;
                o.Occlusion = tex2D(_Occlusion, uv_Occlusion).r;
                o.Alpha = 1;
                clip(temp_output_42_0 - _Cutoff);
            }

            ENDCG
        }
            Fallback "Diffuse"
                CustomEditor "ASEMaterialInspector"
}
