//-----------------------------------------------------------------------
// <copyright file="ScreenSpaceMaterialWrap.shader" company="Google LLC">
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

Shader "MaterialWrap/Screen Space Material Wrap"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _PseudoColorTex ("Pseudo Color", 2D) = "white" {}
        _CurrentDepthTexture("Current Depth Texture", 2D) = "" {}

        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _NormalizedDepthMin ("Normalized Depth Min", Range(0,5)) = 0.3
        _NormalizedDepthMax ("Normalized Depth Max", Range(0,10)) = 4

        _ClipRadius ("Clip Radius", Range (0,1)) = 1
        _AspectRatio ("Screen Aspect Ratio", float) = .5
        _EdgeFuzz ("Edge Fuzz", Range (0,3)) = .1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "IgnoreProjector"="False"}
        LOD 200

        CGPROGRAM
        // Enables shadows on all light types via physically based Standard lighting model.
        #pragma surface surf Standard fullforwardshadows alpha:blend vertex:vert

        // Uses shader model 3.0 target to get nicer looking lighting.
        #pragma target 3.0

        #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

        sampler2D _MainTex;
        sampler2D _PseudoColorTex;

        struct Input
        {
            float4 customColor;
            float normalizedDepth;
            int clipValue;
            float2 depthTexuv;
            float3 worldPos;
            float4 screenPos;
        };

        struct VInput
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            uint id : SV_VertexID;
        };

        static const float kClipOutValue = -10000000;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _FocalLengthX;
        float _FocalLengthY;
        float _PrincipalPointX;
        float _PrincipalPointY;
        int _ImageDimensionsX;
        int _ImageDimensionsY;
        float _TriangleConnectivityCutOff;
        float _NormalizedDepthMin;
        float _NormalizedDepthMax;
        float4x4 _VertexModelTransform;
        float _ClipRadius;
        float _AspectRatio;
        float _EdgeFuzz;

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

        void vert(inout VInput v, out Input OUT)
        {
            UNITY_INITIALIZE_OUTPUT(Input, OUT);

            float2 texID = int3((uint)v.id % (uint)_ImageDimensionsX, (uint)v.id / (uint)_ImageDimensionsX, 0);
            float2 depthTexuv0 = float2(texID.x / (float)_ImageDimensionsX, texID.y / (float)_ImageDimensionsY);
            float2 depthTexuv1 = float2((texID.x + 1) / (float)_ImageDimensionsX, texID.y / (float)_ImageDimensionsY);
            float2 depthTexuv2 = float2(texID.x / (float)_ImageDimensionsX, texID.y / (float)(_ImageDimensionsY + 1));
            float2 depthTexuv3 = float2((texID.x + 1) / (float)_ImageDimensionsX, texID.y / (float)(_ImageDimensionsY + 1));

            OUT.depthTexuv = depthTexuv0;

            float4 depths;

            // Skips this vertex if it doesn't have a depth value.
            depths[0] = ArCoreDepth_GetMeters(depthTexuv0);
            if (depths[0] == 0)
            {
                v.vertex = 0;
                v.normal = 0;
                OUT.clipValue = kClipOutValue;
                OUT.customColor = float4(0, 0, 0, 1);
                OUT.normalizedDepth = 0;
                return;
            }

            depths[1] = ArCoreDepth_GetMeters(depthTexuv1);
            depths[2] = ArCoreDepth_GetMeters(depthTexuv2);
            depths[3] = ArCoreDepth_GetMeters(depthTexuv3);

            // Tests the difference between each of the depth values and the
            // average.
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
                OUT.customColor = float4(0, 0, 0, 1);
                OUT.normalizedDepth = 0;
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

                // Normal mapped to color value range.
                OUT.customColor = float4((v.normal + 1) * 0.5, 1);

                float depthRange = _NormalizedDepthMax - _NormalizedDepthMin;
                OUT.normalizedDepth = 1 - (depths[0] - _NormalizedDepthMin) /
                depthRange;
            }
            OUT.worldPos = (v.vertex);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            clip(IN.clipValue);

            // Screen pixel coordinates to lookup depth texture value.
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            float2 uv = ArCoreDepth_GetUv(screenUV);
            float occlusionBlending =
                ArCoreDepth_GetVisibility(uv, UnityWorldToViewPos(IN.worldPos));

            fixed4 color = tex2D (_PseudoColorTex, float2(0.5, IN.normalizedDepth)) * _Color;
            o.Albedo = color;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = color.a;

            float2 center = (0.5,0.5);
            float2 off = pow((IN.depthTexuv - center), 2);
            off.y *= pow(_AspectRatio,2);
            float newAlpha = (_ClipRadius/4) - (off.x + off.y);
            newAlpha /= (off.x + off.y) * (_EdgeFuzz);
            newAlpha = saturate(newAlpha);
            if (_EdgeFuzz != 0) {
                o.Alpha *= newAlpha;
            }
            else {
                clip((_ClipRadius)/4 - (off.x + (off.y / pow(_AspectRatio,2))));
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
