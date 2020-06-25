//-----------------------------------------------------------------------
// <copyright file="SpotLight.shader" company="Google LLC">
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

Shader "ARRealism/Relighting/SpotLight"
{
    Properties {
        _MainTex ("Main Texture", 2D) = "black" {}
        _CurrentDepthTexture ("Depth Texture", 2D) = "black" {}
        _UvTopLeftRight ("UV of top corners", Vector) = (0, 1, 1, 1)
        _UvBottomLeftRight ("UV of bottom corners", Vector) = (0, 0, 1, 0)
        _NormalizedDepthMin ("Normalized Depth Min", Range(0.0, 5.0)) = 0.0
        _NormalizedDepthMax ("Normalized Depth Max", Range(0.0, 10.0)) = 7.0
    }

    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual

            GLSLPROGRAM

            #include "UnityCG.glslinc"

            #pragma only_renderers gles3 gles

            #ifdef SHADER_API_GLES3
                #extension GL_OES_EGL_image_external_essl3 : require
            #else
                #extension GL_OES_EGL_image_external : require
            #endif

            uniform sampler2D _BackgroundTexture;
            uniform vec4 _UvTopLeftRight;
            uniform vec4 _UvBottomLeftRight;

            #ifdef VERTEX
                #include "Assets/ARRealismDemos/Relighting/Shaders/ArCoreVertex.glslinc"

                void main()
                {
                    vTextureCoord = ArCoreGetDepthUv(gl_MultiTexCoord0.xy);
                    gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
                }
            #endif // VERTEX

            #ifdef FRAGMENT
                #include "Assets/ARRealismDemos/Relighting/Shaders/ArCoreFragment.glslinc"
                #include "Assets/ARRealismDemos/Relighting/Shaders/Utilities.glslinc"
                #include "Assets/ARRealismDemos/Relighting/Shaders/DGAA.glslinc"
                #include "Assets/ARRealismDemos/Relighting/Shaders/SpotLightCore.glslinc"

                void main()
                {
                    gl_FragColor = vec4(RenderMotionLights(vTextureCoord), 1.0);
                }
            #endif // FRAGMENT

            ENDGLSL
        }
    }
    FallBack Off
}
