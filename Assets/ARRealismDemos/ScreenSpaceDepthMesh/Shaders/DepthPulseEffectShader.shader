//-----------------------------------------------------------------------
// <copyright file="DepthPulseEffectShader.shader" company="Google LLC">
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

Shader "ARRealism/Depth Pulse Effect"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf NoLighting vertex:vert alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

        float _FocalLengthX;
        float _FocalLengthY;
        float _PrincipalPointX;
        float _PrincipalPointY;
        int _ImageDimensionsX;
        int _ImageDimensionsY;
        fixed4 _Color;
        float _PulseDepth;
        float _PulseWidth = 0.4;
        float4x4 _ScreenRotation;
        float _MaximumPulseDepth;
        float _StartFadingDepth;

        struct Input
        {
            float depth;
        };

        struct VInput
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            uint id : SV_VertexID;
        };

        float Gain(in float x, in float low_power, in float high_power)
        {
            float low = step(0.5, x);
            float high = 1.0 - low;
            float exponent = lerp(low_power, high_power, low);
            float blend = 0.5 * pow(2.0 * lerp(low, high, x), exponent);
            return lerp(low, high, blend);
        }

        // Returns a [0-1] value - linear position of v between a and b.
        float InverseLerp(float a, float b, float v)
        {
            return saturate((v - a) / (b - a));
        }

        float4 GetVertex(float tex_x, float tex_y, float z)
        {
            float4 vertex = 0;

            if (z > 0)
            {
                float x = (tex_x - _PrincipalPointX) * z / _FocalLengthX;
                float y = (tex_y - _PrincipalPointY) * z / _FocalLengthY;
                vertex = float4(x, -y, z, 1);
            }

            vertex = mul(vertex, _ScreenRotation);

            return vertex;
        }

        void vert(inout VInput v, out Input OUT)
        {
            UNITY_INITIALIZE_OUTPUT(Input, OUT);

            float2 texId = int3((uint)v.id % (uint)_ImageDimensionsX, (uint)v.id / (uint)_ImageDimensionsX, 0);
            float2 depthTexUv = float2(texId.x / (float)_ImageDimensionsX, texId.y / (float)_ImageDimensionsY);
            OUT.depth = ArCoreDepth_GetMeters(depthTexUv);

            float4 vertex = GetVertex(texId.x, texId.y, OUT.depth);
            v.vertex = vertex;
            v.normal = float3(0, 0, -1);
        }

        fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
        {
            fixed4 c;
            c.rgb = s.Albedo;
            c.a = s.Alpha;
            return c;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float halfPulseWidth = _PulseWidth * 0.5;
            float maximumMeshDistance = _PulseDepth + halfPulseWidth;
            float minimumMeshDistance = _PulseDepth - halfPulseWidth;

            clip(maximumMeshDistance - IN.depth);
            clip(IN.depth - minimumMeshDistance);

            float alpha = Gain(1 - (abs(_PulseDepth - IN.depth) / halfPulseWidth), 3, 3);
            float fading = 1 - InverseLerp(_StartFadingDepth, _MaximumPulseDepth, IN.depth);

            o.Albedo = _Color;
            o.Alpha = alpha * fading;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
