//-----------------------------------------------------------------------
// <copyright file="ColorRampGenerator.cs" company="Google LLC">
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class generates a color ramp for depth visualization.
/// </summary>
public class ColorRampGenerator
{
    private static readonly Vector4 k_TurboRedVec4 = new Vector4(
        -0.04055352f, 2.99182795f, -25.61337602f, 98.59276138f);

    private static readonly Vector4 k_TurboGreenVec4 = new Vector4(
        0.05766247f, -2.96213607f, 36.10218175f, -75.68669889f);

    private static readonly Vector4 k_TurboBlueVec4 = new Vector4(
        -0.08097428f, 4.73175779f, 7.22769359f, -60.61628371f);

    private static readonly Vector2 k_TurboRedVec2 = new Vector2(-133.13583994f, 57.30010939f);

    private static readonly Vector2 k_TurboGreenVec2 = new Vector2(50.48734173f, -7.85878422f);

    private static readonly Vector2 k_TurboBlueVec2 = new Vector2(82.75631656f, -34.06506997f);

    /// <summary>
    /// Calculates a polynomial color based on a linear input value x and four co-efficients.
    /// </summary>
    /// <param name="x">Normalized value between 0 and 1.</param>
    /// <param name="redVec4">Red ramp Vector4.</param>
    /// <param name="greenVec4">Green ramp Vector4.</param>
    /// <param name="blueVec4">Blue ramp Vector4.</param>
    /// <param name="redVec2">Red ramp Vector2.</param>
    /// <param name="greenVec2">Green ramp Vector2.</param>
    /// <param name="blueVec2">Blue ramp Vector2.</param>
    /// <returns>Color calculated from the polynomial function.</returns>
    public static Color GetPolynomialColor(
        float x, Vector4 redVec4, Vector4 greenVec4,
        Vector4 blueVec4, Vector2 redVec2, Vector2 greenVec2, Vector2 blueVec2)
    {
        x = Mathf.Clamp01(x);
        Vector4 v4 = new Vector4(1.0f, x, x * x, x * x * x);
        Vector2 v2 = new Vector2(v4.z, v4.w) * v4.z;

        return new Color(
            Vector4.Dot(v4, redVec4) + Vector2.Dot(v2, redVec2),
            Vector4.Dot(v4, greenVec4) + Vector2.Dot(v2, greenVec2),
            Vector4.Dot(v4, blueVec4) + Vector2.Dot(v2, blueVec2));
    }

    /// <summary>
    /// Calculates a Turbo ramp color for a normalized value x.
    /// </summary>
    /// <param name="x">Normalized value between 0 and 1.</param>
    /// <returns>Color calculated from the Turbo ramp function.</returns>
    public static Color Turbo(float x)
    {
        return GetPolynomialColor(
            x, k_TurboRedVec4, k_TurboGreenVec4, k_TurboBlueVec4,
            k_TurboRedVec2, k_TurboGreenVec2, k_TurboBlueVec2);
    }
}
