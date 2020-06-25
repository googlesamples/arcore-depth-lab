//-----------------------------------------------------------------------
// <copyright file="DepthVisualizationEffect.shader" company="Google LLC">
//
// Copyright 2020 Google LLC. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain/ a copy of the License at
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

Shader "ARRealism/DepthVisualizationEffect"
{
    Properties
    {
        _MainTex ("Depth Map", 2D) = "white" {}
        _RampTex ("Color Ramp", 2D) = "white" {}
        _CameraViewOpacity("Camera View Opacity", Float) = 0.0
        _MaxVisualizationDistance("Maximum Depth Visualization Distance [m]", Float) = 6.0
    }
    SubShader
    {
        // No culling on depth visualization.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 cameraUv : TEXCOORD0;
                float2 depthUv : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.cameraUv = v.uv;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depthUv = ArCoreDepth_GetUv(v.uv);
                return o;
            }

            uniform sampler2D _MainTex;
            uniform sampler2D _RampTexture;
            uniform half _CameraViewOpacity;
            uniform half _MaxVisualizationDistance;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 camera_color = tex2D(_MainTex, i.cameraUv);
                float depth = ArCoreDepth_GetMeters(i.depthUv);
                depth = clamp(depth / _MaxVisualizationDistance, 0.0, 1.0);
                fixed4 depth_color = tex2D(_RampTexture, float2(depth, 0.5));
                fixed4 output = lerp(depth_color, camera_color, _CameraViewOpacity);
                return fixed4(output.rgb, 1.0);
            }
            ENDCG
        }
    }
}
