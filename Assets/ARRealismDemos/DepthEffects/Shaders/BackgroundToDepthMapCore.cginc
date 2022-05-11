//-----------------------------------------------------------------------
// <copyright file="BackgroundToDepthMapCore.cginc" company="Google LLC">
//
// Copyright 2020 Google LLC
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

    float depthMeters = ArCoreDepth_GetMeters(depthUv);
    const float kMidRangeMeters = 8;
    const float kMaxRangeMeters = 30;
    const float kOffsetMeters = 0.1;
    depthMeters = clamp(depthMeters, kOffsetMeters, kMaxRangeMeters - kOffsetMeters);

    float depth01 =
    (depthMeters < kMidRangeMeters) ?
    0.5 * (depthMeters / kMidRangeMeters) :
    (0.5 + 0.5 * (depthMeters - kMidRangeMeters) / (kMaxRangeMeters - kMidRangeMeters));

    // depth01 =  depthMeters / 30;

    // Visualizes the depth map with texture sampling.
    fixed4 depthColor = tex2D(_RampTexture, float2(depth01, 0.5));
    // depthColor = fixed4(depth01, depth01, depth01, 1);

    // Fades out the transition region at far depth.
    float fadingStart = 1 - _FarFadePortion;
    float farFadingOpacity = 1 - InverseLerp(fadingStart, 1, _Transition);

    // Uses the RGB camera's luminance to add details to the depth colors.
    depthColor = depthColor * (GetLuminance(cameraColor) * 0.25 + 0.75);

    // Increases depth visualization from the camera viewpoint.
    float transitionCameraViewOpacity = step(_Transition, depth01);

    // Fades to depth at the far back when the transition is almost completed.
    if (_Transition >= fadingStart && depth01 >= fadingStart)
    {
        transitionCameraViewOpacity = farFadingOpacity;
    }

    fixed4 outputColor = lerp(depthColor, cameraColor, transitionCameraViewOpacity);

    // Highlights the area of transition.
    float normalizedTransitionWidth = _HalfTransitionHighlightWidth / _MaxVisualizationDistance;
    float normalizedDistanceToFront = abs(_Transition - depth01) / normalizedTransitionWidth;

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
