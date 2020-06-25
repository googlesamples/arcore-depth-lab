//-----------------------------------------------------------------------
// <copyright file="DGAA.cginc" company="Google LLC">
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
// This is a header file used together with ARCoreDepth.cginc
// For example, #include "Assets/Common/Shaders/DGAA.cginc"
// DGAA: Depth-Guided Anti-Aliasing (go/dgaa-doc)

// Returns depth value in meters for a given depth texture UV.
inline float ArCoreGetNormalizedDepth(float2 uv, float2 depthRange)
{
    float depth = ArCoreDepth_GetMeters(uv);
    return saturate((depth - depthRange.x) / (depthRange.y - depthRange.x));
}

// Returns the component-wise summation of a float4.
inline float Sum4(in float4 v) { return v.x + v.y + v.z + v.w; }

// Returns the component-wise minimum value of a float4.
inline float Min4(in float4 v) { return min(min(v.x, v.y), min(v.z, v.w)); }

// Returns the component-wise maximum value of a float4.
inline float Max4(in float4 v) { return max(max(v.x, v.y), max(v.z, v.w)); }

// DGAA Smoothes aliasing artifacts in the depth map.
// DGAA conducts 9 texture fetches in total.
// Input parameters:
// - float2 uv: screen coordiantes.
// - float MaxVisualizationDistance in meters. 7m recommended.
// Input uniforms:
// - Sampler2D uDepth: single-channel depth map.
// Output:
// - Antialiased depth value of the current pixel at uv as a float.
// Dependencies (utilities.glslh):
// - ArCoreGetNormalizedDepth(): gets the depth value of the current uv.
// - Sum4(): returns the sum of a float4.
// - Min4(): returns the minimum of a float4.
// - Max4(): returns the maximum of a float4.
float ArCoreGetNormalizedDepthWithDGAA(float2 uv, float2 depthRange) {
  const float kInvalidThreshold = 0.001;
  const float kTopLeftShift = -0.75;
  const float kReduceMinimum = 0.008;
  const float kReduceFactor = 0.03;
  const float kNearMin = -0.2;
  const float kNearMax = 0.2;
  const float kFarMin = -0.5;
  const float kFarMax = 0.5;
  const float kHalf = 0.5;
  const float2 kMinSteps = float2(9.0, 9.0);
  const float2 kMaxSteps = float2(12.0, 12.0);

  // Gets the current depth value.
  float depth = ArCoreGetNormalizedDepth(uv, depthRange);
  if (depth < kInvalidThreshold)
    return depth;

  // Derivatives for the one-ring neighborhood.
  float depth_factor = sqrt(1.0 - depth);
  float2 steps = lerp(kMinSteps, kMaxSteps, depth_factor);
  float2 dXdY = steps / _ScreenParams.xy;
  float2 dX = float2(dXdY.x, 0.0);
  float2 dY = float2(0.0, dXdY.y);

  // Gets the depth values in the one-ring neighborhood.
  float2 top_left = uv + dXdY * kTopLeftShift;
  float4 ring = float4(ArCoreGetNormalizedDepth(top_left, depthRange),
                       ArCoreGetNormalizedDepth(top_left + dX, depthRange),
                       ArCoreGetNormalizedDepth(top_left + dY, depthRange),
                       ArCoreGetNormalizedDepth(top_left + dXdY, depthRange));

  // Computes the min and max values of the local neighborhood.
  float local_min = min(depth, Min4(ring));
  float local_max = max(depth, Max4(ring));

  // Computes the horizontal and vertical directions of the current edge.
  float2 dir = float2((ring.z + ring.w) - (ring.x + ring.y),
                      (ring.z + ring.x) - (ring.w + ring.y));

  float dirReduce = max(Sum4(ring) * kReduceFactor, kReduceMinimum);
  float dirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);

  dir = clamp(dir * dirMin, -steps, steps) * dXdY;

  // Computes the results in small and large receptive fields (near / far).
  float near_mix =
      lerp(ArCoreGetNormalizedDepth(uv + dir * kNearMin, depthRange),
           ArCoreGetNormalizedDepth(uv + dir * kNearMax, depthRange), kHalf);
  float far_mix =
      lerp(ArCoreGetNormalizedDepth(uv + dir * kFarMin, depthRange),
           ArCoreGetNormalizedDepth(uv + dir * kFarMax, depthRange), kHalf);
  far_mix = lerp(near_mix, far_mix, kHalf);

  return lerp(near_mix, far_mix,
              step(local_min, far_mix) * step(far_mix, local_max));

  return depth;
}
