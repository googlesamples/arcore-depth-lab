//-----------------------------------------------------------------------
// <copyright file="PointsRelighting.shader" company="Google LLC">
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

Shader "ARRealism/Relighting/Points Relighting"
{
    Properties {
        _NormalizedDepthMin ("Normalized depth minimum", Range(0.0, 5.0)) = 0.0
        _NormalizedDepthMax ("Normalized depth maximum", Range(0.0, 10.0)) = 8.0
        _GlobalDarkness ("Mixture percentage of the global darkness", Range(0.0, 1.0)) = 1.0
        _ShowColorOnly("Only Render Camera", Range(0.0, 1.0)) = 0.0
    }

    // SubShader for point relighting.
    SubShader
    {
        Pass
        {
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"
            #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"
            #include "Assets/ARRealismDemos/Common/Shaders/TurboColormap.cginc"

            uniform float _NormalizedDepthMin;
            uniform float _NormalizedDepthMax;

            // Returns the input RGB value of the current camera.
            uniform sampler2D _BackgroundTexture;
            uniform half _ShowColorOnly;

            float3 SampleColor(in float2 uv) {
                return tex2D(_BackgroundTexture, uv).rgb;
            }

            #include "PointsRelightingCore.cginc"

            fixed4 frag(v2f_img i) : COLOR
            {
                if (_ShowColorOnly > 0.0) {
                    return tex2D(_BackgroundTexture, i.uv);
                }
                return fixed4(Render(i.uv), 1);
            }

            ENDCG
        } // Pass
    } // SubShader for point relighting.

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
