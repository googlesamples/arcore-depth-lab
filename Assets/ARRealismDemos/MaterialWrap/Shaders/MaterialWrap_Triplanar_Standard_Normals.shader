//-----------------------------------------------------------------------
// <copyright file="MaterialWrap_Triplanar_Standard_Normals.shader" company="Google LLC">
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

Shader "MaterialWrap/Material Wrap Triplanar Standard Normals" {
    Properties {
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _MainTex("Wall Texture", 2D) = "white" {}
        _MainTex2("Ground Texture (RGB)", 2D) = "white" {}
        _TexScale("Texture Scale", float) = 1
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _ClipRange ("Clip Range", Range (0,10)) = 1
        _ClipRadius ("Clip Radius", Range (0,1)) = 1
    }

    SubShader{
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows alpha
        #pragma target 3.0

        uniform fixed4 _Color;
        uniform sampler2D _MainTex;
        uniform sampler2D _MainTex2;
        uniform sampler2D _Normal;
        uniform sampler2D _Normal2;
        uniform float4 _MainTex_ST;
        uniform half _TexScale;
        uniform half _Glossiness;
        uniform half _Metallic;
        uniform float _ClipRange;
        uniform float _ClipRadius;

        struct Input
        {
            float3 localCoord;
            float3 localNormal;
        };

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.localCoord = v.vertex.xyz;
            data.localNormal = v.normal.xyz;
        }

        void surf(Input IN, inout SurfaceOutputStandard o) {
            float3 bf = normalize(abs(IN.localNormal));
            bf /= dot(bf, (float3)1);

            float2 tx = IN.localCoord.yz * _TexScale;
            float2 ty = IN.localCoord.zx * _TexScale;
            float2 tz = IN.localCoord.xy * _TexScale;

            half4 cx = tex2D(_MainTex, tx) * bf.x;
            half4 cz = tex2D(_MainTex, tz) * bf.z;
            half4 cy = tex2D(_MainTex2, ty) * bf.y;
            half4 color = (cx + cy + cz) * _Color;

            o.Albedo = color.rgb * _Color;
            o.Alpha = color.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = color.a;

            // Discards out-of-range vertices.
            clip((_ClipRadius * _ClipRange) - distance(IN.localCoord, unity_ObjectToWorld[3].xyz));
        }
        ENDCG
    }

    FallBack "Legacy Shaders/Diffuse"
}
