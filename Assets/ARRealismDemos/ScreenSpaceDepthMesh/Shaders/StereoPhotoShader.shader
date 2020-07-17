//-----------------------------------------------------------------------
// <copyright file="StereoPhotoShader.shader" company="Google LLC">
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

Shader "ARRealism/StereoPhotoShader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _CurrentDepthTexture("Static Depth Texture", 2D) = "" {}
        _NormalizedDepthMin ("Normalized Depth Min", Range(0,5)) = 0.3
        _NormalizedDepthMax ("Normalized Depth Max", Range(0,10)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf NoLighting vertex:vert
        #pragma target 3.0

        #include "Assets/ARRealismDemos/Common/Shaders/TurboColormap.cginc"
        #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

        struct Input
        {
            int clipValue;
            float4 modelViewPosition;
        };

        struct VertexInput
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            uint id : SV_VertexID;
        };

        static const float kClipOutValue = -10000000;
        uniform sampler2D _MainTex;
        uniform float _FocalLengthX;
        uniform float _FocalLengthY;
        uniform float _PrincipalPointX;
        uniform float _PrincipalPointY;
        uniform int _ImageDimensionsX;
        uniform int _ImageDimensionsY;
        uniform float _TriangleConnectivityCutOff;
        uniform float _NormalizedDepthMin;
        uniform float _NormalizedDepthMax;
        uniform float4x4 _VertexModelTransform;
        uniform float4x4 _CameraViewMatrix;
        uniform float4x4 _TextureProjectionMatrix;

        float4 GetVertex(float tex_x, float tex_y, float z)
        {
            float4 vertex = 0;

            if (z > 0)
            {
                float x = (tex_x - _PrincipalPointX) * z / _FocalLengthX;
                float y = (tex_y - _PrincipalPointY) * z / _FocalLengthY;
                vertex = float4(x, -y, z, 1);
            }

            vertex = mul(_VertexModelTransform, vertex);

            return vertex;
        }

        void vert(inout VertexInput v, out Input OUT)
        {
            UNITY_INITIALIZE_OUTPUT(Input, OUT);

            float2 texID = int3((uint)v.id % (uint)_ImageDimensionsX, (uint)v.id / (uint)_ImageDimensionsX, 0);
            float2 depthTexuv0 = float2(texID.x / (float)_ImageDimensionsX, texID.y / (float)_ImageDimensionsY);
            float2 depthTexuv1 = float2((texID.x + 1) / (float)_ImageDimensionsX, texID.y / (float)_ImageDimensionsY);
            float2 depthTexuv2 = float2(texID.x / (float)_ImageDimensionsX, texID.y / (float)(_ImageDimensionsY + 1));
            float2 depthTexuv3 = float2((texID.x + 1) / (float)_ImageDimensionsX, texID.y / (float)(_ImageDimensionsY + 1));
            float4 depths;

            // Skips this vertex if it doesn't have a depth value.
            depths[0] = ArCoreDepth_GetMeters(depthTexuv0);
            if (depths[0] == 0)
            {
                v.vertex = 0;
                v.normal = 0;
                OUT.clipValue = kClipOutValue;
                return;
            }

            depths[1] = ArCoreDepth_GetMeters(depthTexuv1);
            depths[2] = ArCoreDepth_GetMeters(depthTexuv2);
            depths[3] = ArCoreDepth_GetMeters(depthTexuv3);

            // Tests the difference between each of the depth values and the average.
            // If any deviates by the cutoff or more, don't render this triangle.
            float4 averageDepth = (depths[0] +
            depths[1] +
            depths[2] +
            depths[3]) * 0.25;
            float4 depthDev = abs(depths - averageDepth);
            float cutoff = _TriangleConnectivityCutOff;
            float4 branch_ = step(cutoff, depthDev);

            if (any(branch_))
            {
                v.vertex = 0;
                v.normal = 0;
                OUT.clipValue = kClipOutValue;
            }
            else
            {
                // Calculates vertex positions of right and bottom neighbors.
                v.vertex = GetVertex(texID.x, texID.y, depths[0]);
                float4 vertexRight = GetVertex(texID.x + 1, texID.y, depths[1]);
                float4 vertexBottom = GetVertex(texID.x, texID.y + 1, depths[2]);

                // Calculates the vertex normal.
                float3 sideBA = vertexRight - v.vertex;
                float3 sideCA = vertexBottom - v.vertex;
                float3 normal = normalize(cross(sideBA, sideCA));

                v.normal = normal;
                OUT.clipValue = 0;
                OUT.modelViewPosition = mul(_CameraViewMatrix, v.vertex);
            }
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            clip(IN.clipValue);

            // Computes the homogeneous coordinates and the uvs of the frozen texture.
            float4 homogeneous_coordinates = mul(_TextureProjectionMatrix, IN.modelViewPosition);
            float2 uv = homogeneous_coordinates.xy / homogeneous_coordinates.w * 0.5 + 0.5;

            if (uv.x < 0 || uv.y < 0 || uv.x > 1 || uv.y > 1)
            {
                o.Emission = float3(0, 0, 0);
            }
            else
            {
                o.Emission = tex2D(_MainTex, uv).rgb;
            }

            o.Alpha = 1;
        }

        fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
        {
            return fixed4(s.Albedo, s.Alpha);
        }

        ENDCG
    }
    FallBack "Diffuse"
}
