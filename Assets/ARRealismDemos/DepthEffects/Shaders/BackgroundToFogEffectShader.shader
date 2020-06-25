//-----------------------------------------------------------------------
// <copyright file="BackgroundToFogEffectShader.shader" company="Google LLC">
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
Shader "ARRealism/Background To Fog Effect Shader"
{
    Properties {
        _MainTex ("Main Texture", 2D) = "white" {}
        _GammaCorrection("Gamma Correction (1.0 = Enabled)", Range(0.0, 1.0)) = 1.0
        _FogDistance("Fog Threshold", Range(0.0, 5.0)) = 1.5
        _FogThickness("Fog Thickness", Range(0.0, 1.0)) = 1.0
        _FogDensityFactor("Fog Density Factor", Range(0.0, 1.0)) = 0.0
        _FogColor("Fog color", Color) = (1,1,1,1)
        _ShowColorOnly("Only Render Camera", Range(0.0, 1.0)) = 0.0
    }

    // For GLES3 or GLES2 on device
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

            // Whether or not to enable depth-guided anti-aliasing on this shader.
            #define APPLY_DGAA 1
            // Whether or not to use polynomial color optimization or depth-color texture.
            #define APPLY_POLYNOMIAL_COLOR 1

            #include "UnityCG.cginc"
            #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"
            #include "Assets/ARRealismDemos/Common/Shaders/Utilities.cginc"
            #include "Assets/ARRealismDemos/Common/Shaders/DGAA.cginc"

            struct v2f
            {
                float4 grabPos : TEXCOORD0;
                float2 duv : TEXCOORD1;
                float4 pos : SV_POSITION;
            };

            float random(float2 uv)
            {
                return frac(sin(dot(uv,float2(12.9898,78.233)))*43758.5453123);
            }

            // Returns a random value in [0, 1).
            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            // Returns a value in the range if [-1.0, 1.0].
            float noise(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);

                f = f * f * (3.0 - 2.0 * f);
                float n = p.x + p.y * 57.0 + 113.0 * p.z;

                return lerp(
                lerp(lerp( hash(n), hash(n + 1.0),f.x),
                lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                lerp( hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
            }

            v2f vert(appdata_base v) {
                v2f o;
                // Uses UnityObjectToClipPos from UnityCG.cginc to calculate
                // the clip-space of the vertex.
                o.pos = UnityObjectToClipPos(v.vertex);
                // Uses ComputeGrabScreenPos function from UnityCG.cginc
                // to get the correct texture coordinate.
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.duv = ArCoreDepth_GetUv(o.grabPos);
                return o;
            }

            uniform sampler2D _BackgroundTexture;
            uniform sampler2D _RampTexture;
            uniform half _CameraViewOpacity;
            uniform half _FarFadePortion;
            uniform half _MaxVisualizationDistance;
            uniform half _MinVisualizationDistance;
            uniform half _HalfTransitionHighlightWidth;
            uniform half _FogDistance;
            uniform half _FogThickness;
            uniform half _FogDensityFactor;
            uniform fixed4 _FogColor;
            uniform half _ShowColorOnly;

            // Normalized transition between 0 and 1.
            uniform half _Transition;

            half4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2Dproj(_BackgroundTexture, i.grabPos);
                if (_ShowColorOnly > 0.0) {
                    return color;
                }
                
                fixed4 grey = dot(color.rgb, float3(0.3, 0.59, 0.11));

                float depthInMt = ArCoreDepth_GetMeters(i.duv);

                float2 depthRange = float2(_MinVisualizationDistance, _MaxVisualizationDistance);
                #if APPLY_DGAA
                    float depth = ArCoreGetNormalizedDepthWithDGAA(i.duv, depthRange);
                #else
                    float depth = ArCoreGetNormalizedDepth(i.duv, depthRange);
                #endif

                fixed4 fogColor = _FogColor;

                if (depthInMt < _FogDistance)
                {
                    fogColor = color;
                }
                else
                {
                    float fogFactor = saturate((depthInMt - _FogDistance) / _FogThickness);
                    fixed4 colorGrey = lerp(color, grey, fogFactor);
                    fogColor = lerp(colorGrey, fogColor, fogFactor * 0.97);
                }

                return fixed4(fogColor.rgb, 1.0);
            }
            ENDCG
        } // Shader: Background to Depth
    } // Subshader

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
