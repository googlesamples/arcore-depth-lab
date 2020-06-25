//-----------------------------------------------------------------------
// <copyright file="Sunbeam.shader" company="Google LLC">
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

Shader "ARRealism/Relighting/Sunbeam"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DepthPower ("Luminosity", Range(0.0, 1.0)) = 1.0
        _CurrentDepthTexture ("Depth Texture", 2D) = "black" {}
        _UvTopLeftRight ("UV of top corners", Vector) = (0, 1, 1, 1)
        _UvBottomLeftRight ("UV of bottom corners", Vector) = (0, 0, 1, 0)
        _NormalizedDepthMin ("Normalized Depth Min", Range(0.0, 5.0)) = 0.0
        _NormalizedDepthMax ("Normalized Depth Max", Range(0.0, 10.0)) = 7.0
        _GlobalAlphaValue ("Global Opacity (1 = Visible)", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        // No culling or depth.
        Cull Off ZWrite Off ZTest Always

        Pass {
            GLSLPROGRAM

            #pragma fragmentoption ARB_precision_hint_nicest

            #include "UnityCG.glslinc"

            #ifdef VERTEX // here begins the vertex shader
                varying vec2 vTextureCoord;

                void main() // all vertex shaders define a main() function
                {
                    vTextureCoord = gl_MultiTexCoord0.xy;
                    gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
                }

            #endif // here ends the definition of the vertex shader

            #ifdef FRAGMENT // here begins the fragment shader

                varying vec2 vTextureCoord;

                uniform sampler2D _BackgroundTexture;
                uniform sampler2D _CurrentDepthTexture;
                uniform sampler2D _CameraDepthTexture;

                uniform float _NormalizedDepthMin;
                uniform float _NormalizedDepthMax;

                uniform vec4 _UvTopLeftRight;
                uniform vec4 _UvBottomLeftRight;

                #include "Assets/ARRealismDemos/CombinedDemo/Shaders/MSLightsImageEffectIncludes.glslinc"
                #include "Assets/ARRealismDemos/Relighting/Shaders/Utilities.glslinc"
                #include "Assets/ARRealismDemos/Relighting/Shaders/SunbeamCore.glslinc"

                void main()
                {
                    gl_FragColor = vec4(RenderMotionLights(vTextureCoord, false), 1.0);
                }

            #endif // Here ends the definition of the fragment shader.

            ENDGLSL // Here ends the part in GLSL.
        }
    }

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

    Fallback off
}
