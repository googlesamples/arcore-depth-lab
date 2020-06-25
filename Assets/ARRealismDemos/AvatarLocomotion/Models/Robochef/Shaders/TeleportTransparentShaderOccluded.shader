//-----------------------------------------------------------------------
// <copyright file="TeleportTransparentShaderOccluded.shader" company="Google LLC">
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
Shader "Google Occlusion/Robot Teleport Transparent Occluded"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "white" {}
        _Smoothness("Smoothness", Range( 0 , 1)) = 0.85
        _TeleportPhase("Teleport Phase", Range( 0 , 1)) = 0
        [HideInInspector] _texcoord( "", 2D ) = "white" {}
        [HideInInspector] __dirty( "", Int ) = 1
        _CurrentDepthTexture("Depth Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent" "IgnoreProjector" = "True" }

         Pass
        {
            ZWrite On
            ColorMask 0
        }

        Cull Back
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard alpha:blend addshadow fullforwardshadows
        #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"
        #include "UnityPBSLighting.cginc"
        #include "Lighting.cginc"

        struct Input
        {
            float2 uv_texcoord;
            float3 worldPos;
        };

        uniform sampler2D _Albedo;
        uniform float4 _Albedo_ST;
        uniform float _Smoothness;
        uniform float _TeleportPhase;

        void surf( Input i , inout SurfaceOutputStandard o )
        {
            float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
            float4 tex2DNode1 = tex2D( _Albedo, uv_Albedo );
            o.Albedo = tex2DNode1.rgb;
            o.Smoothness = _Smoothness;
            o.Alpha = ( tex2DNode1.a * saturate( (1.0 + (_TeleportPhase - 0.2) * (0.0 - 1.0) / (0.3 - 0.2)) ) ) *
                ArCoreDepth_GetVisibility( uv_Albedo, UnityWorldToViewPos( i.worldPos ) );
        }

        ENDCG
    }
    Fallback "Diffuse"
}
