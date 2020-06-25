//-----------------------------------------------------------------------
// <copyright file="BackgroundToDepthMapShader.shader" company="Google LLC">
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
Shader "ARRealism/Background To Depth Map Shader"
{
    Properties {
        _MainTex ("Main Texture", 2D) = "white" {}
        _RampTexture ("Ramp Texture", 2D) = "white" {}
        _CurrentDepthTexture ("Depth Texture", 2D) = "black" {}
        _TransitionHighlightColor("Transition highlight color]", Color) = (1,1,1,1)
        _GammaCorrection("Gamma Correction (1.0 = Enabled)", Range(0.0, 1.0)) = 1.0
        _MinDepth("Min Depth in [0, 1] for visualization", Range(0.0, 1.0)) = 0.0
        _DepthRange("Depth Range in [0, 1] for visualization.", Range(0.01, 1.0)) = 1.0
        _ShowColorOnly("Only Render Camera", Range(0.0, 1.0)) = 0.0
    }

    // Subshader for depth transition effect.
    SubShader
    {
        // Renders the background to depth map transition effect.
        Pass
        {
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"
            #include "Assets/ARRealismDemos/Common/Shaders/Utilities.cginc"
            #include "Assets/ARRealismDemos/Common/Shaders/DGAA.cginc"
            #include "Assets/ARRealismDemos/Common/Shaders/TurboColormap.cginc"
            #include "Assets/ARRealismDemos/DepthEffects/Shaders/BackgroundToDepthMapCore.cginc"
            
            uniform half _ShowColorOnly;
            
            struct v2f
            {
                float4 colorUv : TEXCOORD0;
                float2 depthUv : TEXCOORD1;
                float4 position : SV_POSITION;
            };

            v2f vert(appdata_base v) {
                v2f o;
                // Uses UnityObjectToClipPos from UnityCG.cginc to calculate
                // the clip-space of the vertex.
                o.position = UnityObjectToClipPos(v.vertex);
                // Uses ComputeGrabScreenPos function from UnityCG.cginc
                // to get the correct texture coordinate.
                o.colorUv = ComputeGrabScreenPos(o.position);
                o.depthUv = ArCoreDepth_GetUv(o.colorUv);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2Dproj(_BackgroundTexture, i.colorUv);
                if (_ShowColorOnly > 0.0) {
                    return color;
                }
                return RenderCameraToDepthMapTransition(color, i.depthUv);
            }
            ENDCG
        } // Shader: Background to Depth
    } // Subshader for depth transition effect.

    // Subshader for instant preview.
    Subshader
    {
        Pass
        {
            ZWrite Off

            CGPROGRAM
            #include "Assets/ARRealismDemos/Common/Shaders/PreviewEmptyShader.cginc"
            ENDCG
        } // Pass
    } // Subshader for instant preview.

    FallBack Off
}
