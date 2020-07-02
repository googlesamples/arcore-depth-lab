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
        0.13572138f, 4.61539260f, -42.66032258f, 132.13108234f);

    private static readonly Vector4 k_TurboGreenVec4 = new Vector4(
        0.09140261f, 2.19418839f, 4.84296658f, -14.18503333f);

    private static readonly Vector4 k_TurboBlueVec4 = new Vector4(
        0.10667330f, 12.64194608f, -60.58204836f, 110.36276771f);

    private static readonly Vector2 k_TurboRedVec2 = new Vector2(-152.94239396f, 59.28637943f);

    private static readonly Vector2 k_TurboGreenVec2 = new Vector2(4.27729857f, 2.82956604f);

    private static readonly Vector2 k_TurboBlueVec2 = new Vector2(-89.90310912f, 27.34824973f);

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
            1 - x, k_TurboRedVec4, k_TurboGreenVec4, k_TurboBlueVec4,
            k_TurboRedVec2, k_TurboGreenVec2, k_TurboBlueVec2);
    }
}
