//-----------------------------------------------------------------------
// <copyright file="MaterialWrapStandardSurface.shader" company="Google LLC">
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

Shader "MaterialWrap/Material Wrap Standard Surface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _ClipRadius ("Clip Radius", Range (0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Enables shadows on all light types via physically based Standard lighting model.
        #pragma surface surf Standard fullforwardshadows alpha

        // Uses shader model 3.0 target to get nicer looking lighting.
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _ClipRadius;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Samples albedo from the texture and multiplies by a tint color.
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;

            float2 off = pow((IN.uv_MainTex - .5), 2);
            clip(_ClipRadius/4 - (off.x + off.y));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
