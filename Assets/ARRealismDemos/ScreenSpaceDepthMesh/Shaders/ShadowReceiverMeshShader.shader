//-----------------------------------------------------------------------
// <copyright file="ShadowReceiverMeshShader.shader" company="Google LLC">
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

Shader "ARRealism/Shadow Receiver Mesh Shader"
{
    Properties
    {
        // Sets the transparency of the applied shadow.
        // This is in addition to any existing shadow intensity settings set for each source.
        _GlobalShadowIntensity ("Global Shadow Intensity", Range (0, 1)) = 0.6
        _MinimumMeshDistance ("Minimum Mesh Distance", Range (0, 1000)) = 0
        _MaximumMeshDistance ("Maximum Mesh Distance", Range (0, 1000)) = 1000
        _CurrentDepthTexture("Current Depth Texture", 2D) = "" {}
        _ZWrite ("Global Occlusion on=1 or off=0", Int) = 0
    }

    SubShader
    {
        Tags {"Queue"="Background+1" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
        LOD 200
        ZWrite [_ZWrite]
        Blend Zero SrcColor

        CGPROGRAM
        // Appends 'addshadow' after 'alphatest:_Cutoff' to allow the Shadow Receiver Mesh to cast its own shadow.
        // Also in the 'ShadowReceiverMesh' prefab, set 'Cast Shadows' to 'On' for the 'Mesh Renderer' component.
        #pragma surface surf ShadowOnly vertex:vert alphatest:_Cutoff

        #pragma target 3.0

        #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

        uniform float _GlobalShadowIntensity;
        uniform float _MinimumMeshDistance;
        uniform float _MaximumMeshDistance;

        uniform float _FocalLengthX;
        uniform float _FocalLengthY;
        uniform float _PrincipalPointX;
        uniform float _PrincipalPointY;
        uniform int _ImageDimensionsX;
        uniform int _ImageDimensionsY;
        uniform float4x4 _VertexModelTransform;

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

            float2 texId = int3((uint)v.id % (uint)_ImageDimensionsX, (uint)v.id / (uint)_ImageDimensionsX, 0);
            float2 depthTexUv = float2(texId.x / (float)_ImageDimensionsX, texId.y / (float)_ImageDimensionsY);
            OUT.depth = ArCoreDepth_GetMeters(depthTexUv);

            float4 vertex = GetVertex(texId.x, texId.y, OUT.depth);
            v.vertex = vertex;
            v.normal = mul((float3x3)UNITY_MATRIX_V,float3(0, 0, -1));
        }

        inline fixed4 LightingShadowOnly(SurfaceOutput s, fixed3 lightDir, fixed attenuation)
        {
            fixed4 c;
            c.rgb = lerp(s.Albedo, s.Albedo * attenuation, _GlobalShadowIntensity);
            c.a = s.Alpha;
            return c;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            // Constrains the shadow & occlusion mesh to a minimum and maximum depth.
            clip(_MaximumMeshDistance - IN.depth);
            clip(IN.depth - _MinimumMeshDistance);

            o.Albedo = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Transparent/Cutout/VertexLit"
}
