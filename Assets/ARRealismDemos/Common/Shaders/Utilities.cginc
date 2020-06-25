//-----------------------------------------------------------------------
// <copyright file="Utilities.cginc" company="Google LLC">
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

// Useful standalone functions in HLSL / CG shader programming.

// Remaps x from [0, 1] to a sigmoid-like curve in [0, 1].
float Gain(in float x, in float low_power, in float high_power)
{
    float low = step(0.5, x);
    float high = 1.0 - low;
    float exponent = lerp(low_power, high_power, low);
    float blend = 0.5 * pow(2.0 * lerp(low, high, x), exponent);
    return lerp(low, high, blend);
}

// Increases the contrast of an intrinsic color col by an intensity.
float3 Highlight(float3 col, float intensity)
{
    const float kContrast = 2.5;
    float3 result = col + kContrast * (pow(col, 1.0 - intensity * 0.5) - col);
    return result;
}

// Returns a linear position of v between a and b, mapping to [0, 1].
float InverseLerp(in float a, in float b, in float v)
{
    return saturate((v - a) / (b - a));
}

// Returns the greyscale of an RGB color.
float GetLuminance(in fixed3 color) {
    return saturate(0.2126 * color.r + 0.7152 * color.g + 0.0722 * color.b);
}
