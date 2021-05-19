//-----------------------------------------------------------------------
// <copyright file="BackgroundToDepth.shader" company="Google LLC">
//
// Copyright 2021 Google LLC
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

Shader "DepthLab/BackgroundToDepth"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _CurrentDepthTexture("Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite On
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }

            GLSLPROGRAM

            #pragma multi_compile_local __ ARCORE_ENVIRONMENT_DEPTH_ENABLED

            #pragma only_renderers gles3

            #include "UnityCG.glslinc"
            #include "Assets/ARRealismDemos/Common/Shaders/TurboColormap.glslinc"
            #include "BackgroundToDepthMapCore.glslinc"

#ifdef SHADER_API_GLES3
#extension GL_OES_EGL_image_external_essl3 : require
#endif // SHADER_API_GLES3

            // Device display transform is provided by the AR Foundation camera background renderer.
            uniform mat4 _DisplayTransform;

#ifdef VERTEX
            varying vec2 textureCoord;

            void main()
            {
#ifdef SHADER_API_GLES3
                // Transform the position from object space to clip space.S
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;

                // Remap the texture coordinates based on the device rotation.
                textureCoord = (_DisplayTransform * vec4(gl_MultiTexCoord0.x, 1.0f - gl_MultiTexCoord0.y, 1.0f, 0.0f)).xy;
#endif // SHADER_API_GLES3
            }
#endif // VERTEX

#ifdef FRAGMENT
            varying vec2 textureCoord;
            uniform samplerExternalOES _MainTex;
            uniform float _UnityCameraForwardScale;

#ifdef ARCORE_ENVIRONMENT_DEPTH_ENABLED
            uniform sampler2D _CurrentDepthTexture;
#endif // ARCORE_ENVIRONMENT_DEPTH_ENABLED

#if defined(SHADER_API_GLES3) && !defined(UNITY_COLORSPACE_GAMMA)
            float GammaToLinearSpaceExact (float value)
            {
                if (value <= 0.04045F)
                    return value / 12.92F;
                else if (value < 1.0F)
                    return pow((value + 0.055F)/1.055F, 2.4F);
                else
                    return pow(value, 2.2F);
            }

            vec3 GammaToLinearSpace (vec3 sRGB)
            {
                // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
                return sRGB * (sRGB * (sRGB * 0.305306011F + 0.682171111F) + 0.012522878F);

                // Precise version, useful for debugging, but the pow() function is too slow.
                // return vec3(GammaToLinearSpaceExact(sRGB.r), GammaToLinearSpaceExact(sRGB.g), GammaToLinearSpaceExact(sRGB.b));
            }

#endif // SHADER_API_GLES3 && !UNITY_COLORSPACE_GAMMA

            void main()
            {
                vec3 result = vec3(0.0, 0.0, 0.0);

#ifdef SHADER_API_GLES3
                vec3 background = texture(_MainTex, textureCoord).xyz;

#ifdef ARCORE_ENVIRONMENT_DEPTH_ENABLED
                float distance = texture(_CurrentDepthTexture, textureCoord).x;
                result = RenderCameraToDepthMapTransition(background, distance);
#endif // ARCORE_ENVIRONMENT_DEPTH_ENABLED

#ifndef UNITY_COLORSPACE_GAMMA
                result = GammaToLinearSpace(result);
#endif // UNITY_COLORSPACE_GAMMA

                gl_FragColor = vec4(result, 1.0);
                // To enable occlusion with the depth image add `gl_FragDepth = depth;`.
#endif // SHADER_API_GLES3
            }

#endif // FRAGMENT
            ENDGLSL
        }
    }

    FallBack Off
}
