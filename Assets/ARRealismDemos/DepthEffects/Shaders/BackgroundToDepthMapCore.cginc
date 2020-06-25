//-----------------------------------------------------------------------
// <copyright file="BackgroundToDepthMapCore.cginc" company="Google LLC">
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

// Whether or not to use polynomial color optimization or depth-color texture.
#define APPLY_POLYNOMIAL_COLOR 1

uniform sampler2D _BackgroundTexture;
uniform sampler2D _RampTexture;
uniform half _CameraViewOpacity;
uniform half _FarFadePortion;
uniform half _MaxVisualizationDistance;
uniform half _MinVisualizationDistance;
uniform half _HalfTransitionHighlightWidth;
uniform fixed4 _TransitionHighlightColor;
uniform float _MinDepth;
uniform float _DepthRange;
// Whether or not to enable depth-guided anti-aliasing on this shader.
uniform float _ApplyAntiAliasing;

// Normalized transition between 0 and 1.
uniform half _Transition;

fixed4 RenderCameraToDepthMapTransition(in fixed4 cameraColor, in float2 depthUv) {
    float2 depthRange = float2(_MinVisualizationDistance, _MaxVisualizationDistance);

    // Whether or not to apply depth-guided anti-aliasing.
    float depth = 0;
    if (_ApplyAntiAliasing > 0.5) {
        depth = ArCoreGetNormalizedDepthWithDGAA(depthUv, depthRange);
    }
    else
    {
        depth = ArCoreGetNormalizedDepth(depthUv, depthRange);
    }

    #if APPLY_POLYNOMIAL_COLOR
        // Visualizes the depth map with faster polynomial interpolation.
        float normalizedDepth = (depth - _MinDepth) / _DepthRange;
        fixed4 depthColor = step(0, _Transition) * fixed4(TurboColormap(normalizedDepth * 0.95), 1);
    #else
        // Visualizes the depth map with slower texture sampling.
        fixed4 depthColor = tex2D(_RampTexture, float2(depthColorLookup, 0.5));
    #endif // APPLY_POLYNOMIAL_COLOR

    // Fades out the transition region at far depth.
    float fadingStart = 1 - _FarFadePortion;
    float farFadingOpacity = 1 - InverseLerp(fadingStart, 1, _Transition);

    // Uses the RGB camera's luminance to add details to the depth colors.
    depthColor = depthColor * (GetLuminance(cameraColor) * 0.25 + 0.75);

    // Increases depth visualization from the camera viewpoint.
    float transitionCameraViewOpacity = step(_Transition, depth);

    // Fades to depth at the far back when the transition is almost completed.
    if (_Transition >= fadingStart && depth >= fadingStart)
    {
        transitionCameraViewOpacity = farFadingOpacity;
    }

    fixed4 outputColor = lerp(depthColor, cameraColor, transitionCameraViewOpacity);

    // Highlights the area of transition.
    float normalizedTransitionWidth = _HalfTransitionHighlightWidth / _MaxVisualizationDistance;
    float normalizedDistanceToFront = abs(_Transition - depth) / normalizedTransitionWidth;

    // Feathers the highlight area.
    float highlightPulse = Gain(1 - normalizedDistanceToFront, 3, 3);
    float highlightAlpha = step(normalizedDistanceToFront, 1.0) * highlightPulse;

    // Total contribution of the highlight color.
    float totalHighlightContribution = highlightAlpha * farFadingOpacity;

    if (_Transition == 0)
    {
        totalHighlightContribution = 0;
    }

    // Emphasizes the highlight color.
    _TransitionHighlightColor.rgb = Highlight(_TransitionHighlightColor, highlightAlpha * 0.8);
    outputColor = lerp(outputColor, _TransitionHighlightColor, totalHighlightContribution);
    return outputColor;
}
