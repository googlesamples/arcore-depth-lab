//-----------------------------------------------------------------------
// <copyright file="DepthOfFieldCore.cginc" company="Google LLC">
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
#define kPi (3.14159265358979324)
#define kPi2 (kPi * 2.0)
#define kOneOverSqrtTwoPi (1.0 / sqrt(2.0 * kPi))
#define kEnableDiscontinuity 0
#define kDebugDiscontinuity 0
#define kFocusOnWorldAnchor 0
#define kFocusOnScreenPoint 1
#define kFocusOnProjectedPoint 2
#define kRenderDepthMap 3
#define kRenderColorImage 4

uniform int _RenderMode;
uniform int _GreyscalePheripheral;
uniform float _MinDepth;
uniform float _DepthRange;
// The larger the aperture is, the more blurry the peripheral region is.
uniform float _Aperture;
uniform float2 _AspectRatio;
uniform float3 _TouchPosition;

float SampleDepth(in float2 uv) {
    float2 depth_uv = ArCoreDepth_GetUv(uv);
    return (ArCoreDepth_GetMeters(depth_uv) - _MinDepth) / _DepthRange;
}

// Normalizes a coordinate system from [0, 1] to [-1, 1].
float2 NormalizeCoord(in float2 coord, in float2 aspect_ratio) {
    return (2.0 * coord - float2(1.0, 1.0)) * aspect_ratio;
}

// Raises the results slowly over x by a factor of slope.
// Cuts off the function at x < shift.
float SlowRaise(float x, float shift, float slope) {
    return (step(0.0, x - shift) * (1.0 - cos(slope * (x - shift))));
}

// Gaussian function of x with kernel size sigma.
float Gaussian(in float x, in float sigma) {
    return kOneOverSqrtTwoPi / sigma * exp(-0.5 * x * x / (sigma * sigma));
}

// Computes a smooth decaying curve from 1.0 to 0.0 as
// the distance increases from 0.0 to 1.0.
float Decay(in float dist, in float gradient, in float cutoff) {
    float x = dist + gradient;
    float y = x * step(cutoff, x) / gradient;
    return exp(1.0 - y) * y;
}

// Turns sRGB color into greyscale.
float3 GetGreyscale(in float3 col) {
    float intensity = dot(col, float3(0.3, 0.59, 0.11));
    return float3(intensity, intensity, intensity);
}

// Renders the depth-of-field effect.
fixed3 Render(in float2 uv) {
    const int kKernelSize = 17;
    const int kKernelHalfSize = (kKernelSize - 1) / 2;
    const float kGammaValue = 2.2;
    const float3 kGamma = float3(kGammaValue, kGammaValue, kGammaValue);
    const float3 kInversedGamma = float3(1.0, 1.0, 1.0) / kGamma;

    // The larger the shift is, the clearer the focus region is.
    const float kShift = 0.1;
    const float kSlope = 3.0;

    // Initial sigma with the sharpest value.
    // When sigma = 35 for kKernelSize = 21,
    // the Gaussian filter degrades to a box filter.
    const float kInitialSigma = 0.1;

    // The smaller the kOcclusionDecaySigma is,
    // the sharper the depth of field preserves the edges.
    const float kOcclusionDecaySigma = 0.05;
    const float kOcclusionDecayCutoff = kOcclusionDecaySigma;
    const float kEpsilon = 0.00001;

    float2 normalized_uv = NormalizeCoord(uv, _AspectRatio);
    float2 normalized_touch = NormalizeCoord(_TouchPosition.xy, _AspectRatio);

    float depth = SampleDepth(uv);
    float focus = SampleDepth(_TouchPosition.xy);

    if (_RenderMode == kFocusOnWorldAnchor) {
        focus = _TouchPosition.z;
    }

    if (_RenderMode == kRenderDepthMap) {
        return TurboColormap(depth);
    }

    if (_RenderMode == kRenderColorImage) {
        return SampleColor(uv);
    }

    // Kernel size of the Gaussian blur.
    float blur_sigma = saturate(abs(depth - focus) * _Aperture * 2.0);

    // Curve the sigma in a Sigmoid-like function from [0, 1] to [0, 2].
    float normalized_sigma = SlowRaise(blur_sigma, kShift, kSlope);

    // Amplifies the sigma with aperture.
    blur_sigma = kInitialSigma + normalized_sigma * _Aperture;

    // Kernel weights for computing the Gassian blur.
    float kernels[kKernelHalfSize + 1];

    [unroll]
    for (int i = 1; i <= kKernelHalfSize; ++i) {
        float weight = Gaussian(float(i), blur_sigma);
        kernels[i] = weight;
    }

    float sum_weights = Gaussian(0.0, blur_sigma);
    float3 sum_color = SampleColor(uv) * sum_weights;

    #if kDebugDiscontinuity
        float sum_discontinuity = 0.0;
    #endif
    float discontinuity = 1.0;

    // Conducts linear Gaussian blur.
    [unroll]
    for (int i = -kKernelHalfSize; i < 0; ++i) {
        float2 sample_uv = uv + _BlurDirStep * float2(float(i), float(i));

        #if kEnableDiscontinuity
            float sample_depth = SampleDepth(sample_uv);
            float discontinuity = abs(depth - sample_depth);
            discontinuity = Decay(discontinuity, kOcclusionDecaySigma, kOcclusionDecayCutoff);
        #endif

        float3 texel = SampleColor(sample_uv);
        float weight = kernels[abs(i)];

        #if kEnableDiscontinuity
            weight *= discontinuity;
        #endif

        sum_color += weight * texel;
        sum_weights += weight;

        #if kDebugDiscontinuity
            sum_discontinuity += 0.05 * (1.0 - discontinuity);
        #endif
    }

    [unroll]
    for (int i = 1; i <= kKernelHalfSize; ++i) {
        float2 sample_uv = uv + _BlurDirStep * float2(float(i), float(i));

        #if kEnableDiscontinuity
            float sample_depth = SampleDepth(sample_uv);
            float discontinuity = abs(depth - sample_depth);
            discontinuity = Decay(discontinuity, kOcclusionDecaySigma, kOcclusionDecayCutoff);
        #endif

        float3 texel = SampleColor(sample_uv);
        float weight = kernels[abs(i)];

        #if kEnableDiscontinuity
            weight *= discontinuity;
        #endif

        sum_color += weight * texel;
        sum_weights += weight;

        #if kDebugDiscontinuity
            sum_discontinuity += 0.05 * (1.0 - discontinuity);
        #endif
    }

    fixed3 res = sum_color / sum_weights;

    #if kDebugDiscontinuity
        res = fixed3(sum_discontinuity, sum_discontinuity, sum_discontinuity);
    #endif

    if (_GreyscalePheripheral) {
        res = lerp(res, GetGreyscale(res), saturate(normalized_sigma * 0.5));
        float highlight = pow(1.0 - normalized_sigma * 0.5, 10.0);
        fixed3 highlightColor = fixed3(0.9921, 0.8392, 0.3882); // Yellow color.
        res = lerp(res, highlightColor, highlight * 0.3);
    }

    return res;
}
