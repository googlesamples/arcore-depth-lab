//-----------------------------------------------------------------------
// <copyright file="PointsRelightingCore.cginc" company="Google LLC">
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

#define kMaxNumDirectionalLights (3)
#define uResolution (_ScreenParams.xy)
#define uTime (_Time.z)
#define kRenderCameraImage (2)
#define kRenderDepthMap (3)
uniform int _RenderMode;
uniform float3 _PointLightPositions[kMaxNumDirectionalLights];
uniform float _PointLightIntensities[kMaxNumDirectionalLights];
uniform float4 _PointLightColors[kMaxNumDirectionalLights];
uniform float _GlobalDarkness;
uniform float2 _AspectRatio;
uniform float3 _TouchPosition;

// Samples depth value in meters from a uv coordinates.
float SampleDepth(in float2 uv) {
    float2 depth_uv = ArCoreDepth_GetUv(uv);
    return saturate(ArCoreDepth_GetMeters(depth_uv) / (_NormalizedDepthMax - _NormalizedDepthMin));
}

// Returns the squared distance between two 2D points.
float SquaredDistance(in float2 a, in float2 b) {
    return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y);
}

// Returns the distance field of a 2D circle with anti-aliasing.
float SmoothCircle(in float2 uv, in float2 origin, in float radius, in float alias) {
    return 1.0 - smoothstep(radius - alias, radius + alias,
    length(uv - origin));
}

// Returns a simplified distance field of a 3D sphere with anti-aliasing
float SmoothSphere(in float2 uv, in float3 origin, in float depth, in float radius,
in float delta, in float alias) {
    radius = lerp(radius - delta, radius + delta, -log(origin.z));
    float lightSphericalDepth = radius * radius - SquaredDistance(origin.xy, uv) * 2.0;
    return SmoothCircle(uv, origin.xy, radius, alias) *
    smoothstep(origin.z- delta, origin.z + delta, depth + lightSphericalDepth);
}
// Computes the aspect ratio for portait and landscape modes.
float2 CalculateAspectRatio(in float2 size) {
    return size.yy / size.yx;
}

// Normalizes a coordinate system from [0, 1] to [-1, 1].
float2 NormalizeCoord(in float2 coord, in float2 aspectRatio) {
    return (2.0 * coord - float2(1.0, 1.0)) * aspectRatio;
}

// Reverts a coordinate system from [-1, 1] to [0, 1].
float2 ReverseNormalizeCoord(in float2 pos, in float2 aspect_ratio) {
    return (pos / aspect_ratio + 1.0) * 0.5;
}

// Computes the radiance from the distance dist.
float Radiance(in float dist, in float gradient, in float cutoff) {
    float x = dist + gradient;
    float y = x * step(cutoff, x) / gradient;
    return exp(1.0 - y) * y;
}

// GPU Hashing: float2 in, float2 out.
float2 Hash22(float2 p) {
    const float2 kHash2 = float2(25.5359, 77.1053);
    const float3 kHash3 = float3(55.9973, 65.9157, 31.3163);
    float3 a = float3(p.xyx) * kHash3;
    float3 p3 = frac(a);
    p3 += dot(p3, p3.yzx + kHash2.x);
    return frac((p3.xx + p3.yz) * p3.zy);
}

// Gets a random walk position in [0.9, 1.0] to simulate Monte Carlo raytracing.
float2 GetScatterFactorOverTime(in float2 uv) {
    return 0.9 + 0.1 * Hash22(uv + uTime);
}

// Distance field for a 2D grid in Cartesian coordinates.
float CartesianGrid(in float2 uv) {
    const float kTickWidth = 0.1;
    const float kGridWidth = 0.008;
    const float kAxesWidth = 0.015;
    // Distance to the axes.
    float d = 2.0 - smoothstep(0.0, kAxesWidth, abs(uv.x)) -
    smoothstep(0.0, kAxesWidth, abs(uv.y));
    // Distance to the grid.
    for (float i = -2.0; i < 2.0; i += kTickWidth) {
        d += 2.0 - smoothstep(0.0, kGridWidth, abs(uv.x - i)) -
        smoothstep(0.0, kGridWidth, abs(uv.y - i));
    }
    return d;
}

// Renders the depth-of-field effect.
float3 Render(in float2 uv) {
    // Samples:
    float3 result = SampleColor(uv);
    float depth = SampleDepth(uv);
    if (_RenderMode == kRenderCameraImage) {
        return result;
    }

    if (_RenderMode == kRenderDepthMap) {
        return TurboColormap(depth);
    }

    result = lerp(result, result * 0.5, _GlobalDarkness);

    // Common inputs:
    float2 aspectRatio = CalculateAspectRatio(uResolution);
    // aspectRatio = _AspectRatio;
    float2 normalizedUv = NormalizeCoord(uv, aspectRatio);
    float3 samplePos = float3(normalizedUv, depth);
    float2 sampleUv = normalizedUv;
    float3 relightsSum = 0;
    float intensitySum = 0;

    for (int i = 0; i < kMaxNumDirectionalLights; ++i) {
        float3 lightColor = _PointLightColors[i].rgb;

        float3 lightPos = _PointLightPositions[i].xyz;
        lightPos.z /= _NormalizedDepthMax - _NormalizedDepthMin;
        float lightDepth = lightPos.z;
        float2 lightUv = NormalizeCoord(lightPos.xy, aspectRatio);

        const float kGlobalDepthWeight = 0.6;
        float uvDist = distance(sampleUv, lightUv);
        float depthDist = distance(samplePos.zz, lightPos.zz);
        float globalDist = lerp(uvDist, depthDist, kGlobalDepthWeight);

        float2 lightDirection = lightUv - sampleUv;
        float distFactor = lerp(0.1, 3.0, 1.0 - saturate(globalDist * 0.5));
        float photon_energy = 4.0 + distFactor;
        float2 photonUv = sampleUv;

        float intensity = 0.0;

        const float kLowerIntensity = 0.5;
        const float kHigherIntensity = 1.5;
        const float kMaxIntensity = 3.0;
        const float kEnergyDecayFactor = 0.5;
        const int kNumPasses = 8;
        const int kNumPassesFloat = float(kNumPasses);

        // Marches the ray to the light source.
        for (int j = 0; j < kNumPasses; ++j) {
            float2 photonDepthUv = ReverseNormalizeCoord(photonUv, aspectRatio);
            float photonDepth = SampleDepth(photonDepthUv);

            float uvDist = distance(photonUv, lightUv);
            float depthDist = distance(float2(photonDepth, photonDepth), lightPos.zz);
            const float depthWeight = 0.8;
            float dist = lerp(uvDist, depthDist, depthWeight);
            float deltaIntensity = (1.0 - dist) * (1.0 - dist) * photon_energy * kMaxIntensity;
            photon_energy *= kEnergyDecayFactor;
            photonUv += lightDirection / kNumPassesFloat;

            intensity += deltaIntensity;
            break;
        }

        intensity *= saturate(1.4 - depth * 1.2);
        intensity /= kNumPassesFloat * kMaxIntensity;
        intensity = clamp(intensity, -6 / kMaxNumDirectionalLights, 0.85);
        intensitySum += saturate(intensity);
        relightsSum += intensity * lightColor;
    }

    // The relighting pass.
    result += 3.0 * abs(0.5 - relightsSum) * (pow(result, float3(1.5 - relightsSum)) - result);

    // Renders the light sources.
    float3 outerColor = 0;
    float3 outerSingle = 0;
    float outerAlpha = 0;
    float3 innerColor = 0;
    float3 innerSingle = 0;
    float innerAlpha = 0;

    for (int i = 0; i < kMaxNumDirectionalLights; ++i) {
        float3 samplePos = float3(normalizedUv, depth);
        float3 lightPos =  _PointLightPositions[i].xyz;
        lightPos.z /= _NormalizedDepthMax - _NormalizedDepthMin;
        float3 lightColor = _PointLightColors[i].rgb;

        float3 lightNormalizedPos = float3(NormalizeCoord(lightPos.xy, _AspectRatio), lightPos.z);
        float lightDepth = lightPos.z;

        if (lightDepth <= 0) {
            continue;
        }

        // Renders the light sources.
        const float kPointLightRadius = 0.04;
        const float kPointLightFeathering = 0.02;
        float sphereDist = SmoothSphere(normalizedUv, lightNormalizedPos, depth,
        kPointLightRadius, kPointLightFeathering, 0.04);

        lightColor = lightColor * 2.0;

        outerColor += lerp(0, lightColor, sphereDist);
        outerSingle += lerp(0, lightColor, step(0.01, sphereDist));
        outerAlpha = max(outerAlpha, sphereDist);

        lightColor = lerp(lightColor, float3(1, 1, 1), 0.9);
        float innerDist = SmoothSphere(normalizedUv, lightNormalizedPos, depth, 0.025, 0.01, 0.003);

        innerColor += lerp(0, lightColor, innerDist);
        innerSingle += lerp(0, lightColor, step(0.03, innerDist));
        innerAlpha = max(innerAlpha, innerDist);
    }

    // Blends in global darkness around the point lights to increase the relighting effects.
    const float kFalloff = 0.001;
    const float kShadow = 0.3;
    const float kReduction = 0.3;
    float feathering = (1.0 - saturate(intensitySum * 0.9)) * (kShadow + kFalloff);
    float3 darkness = result * smoothstep(kReduction, kFalloff, feathering);
    result = lerp(result, darkness, _GlobalDarkness);

    // Blends in with the point light sources.
    result = lerp(result,  outerColor, 0.3);
    result = lerp(result, innerSingle, innerAlpha);

    return result;
}
