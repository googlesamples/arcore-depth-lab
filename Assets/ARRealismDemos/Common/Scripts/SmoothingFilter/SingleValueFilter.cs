//-----------------------------------------------------------------------
// <copyright file="SingleValueFilter.cs" company="Google LLC">
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// This is a speed adaptive low-pass filter for smoothing sensor data.
/// This is an implementation based on the paper:
///     Casiez, G., Roussel, N. and Vogel, D. (2012).
///     1€ Filter: A Simple Speed-based Low-pass Filter for Noisy Input in Interactive Systems.
/// </summary>
public class SingleValueFilter : MonoBehaviour
{
    /// <summary>
    /// Enables hysterisis-only noise filtering.
    /// </summary>
    public bool DoWindowFilterOnly;

    /// <summary>
    /// Sets the inner window size for ignoring any changes.
    /// </summary>
    public float InnerHysteresisWindowSizeM = k_InnerHysteresisWindowSizeM;

    /// <summary>
    /// Sets the out window size for smoothly blending from hysteresis to filtering value.
    /// </summary>
    public float OuterHysteresisWindowSizeM = k_OuterHysteresisWindowSizeM;

    /// <summary>
    /// Minimum cutoff value.
    /// </summary>
    public float MinCutoff = k_MinCutoff;

    /// <summary>
    /// Cutoff slope.
    /// </summary>
    public float BetaCutoffSlope = k_BetaCutoffSlope;

    /// <summary>
    /// Derivate cutoff frequency value.
    /// </summary>
    public float DerivateCutoffFrequency = k_DerivateCutoffFrequency;

    private const float k_SensorFrequency = 60;
    private const float k_InnerHysteresisWindowSizeM = 0.003f;
    private const float k_OuterHysteresisWindowSizeM = 0.015f;
    private const float k_MinCutoff = 7f;
    private const float k_BetaCutoffSlope = 0.5f;
    private const float k_DerivateCutoffFrequency = 1f;
    private SpeedAdaptiveFilter m_XFilter;
    private float m_LastValue;
    private float m_SensorFrequency = k_SensorFrequency;

    /// <summary>
    /// Default constructor for a 3D position filter.
    /// </summary>
    public SingleValueFilter()
    {
        ReinitalizeFilter();
    }

    /// <summary>
    /// Gets the last filter value.
    /// </summary>
    public float LastValue
    {
        get
        {
            return m_LastValue;
        }
    }

    /// <summary>
    /// Reinitializes the filter. Use this after changing filter parameters.
    /// </summary>
    public void ReinitalizeFilter()
    {
        m_XFilter = new SpeedAdaptiveFilter(
            m_SensorFrequency, MinCutoff, BetaCutoffSlope, DerivateCutoffFrequency);
    }

    /// <summary>
    /// Smoothes the input value.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>Returns the filtered value.</returns>
    public float Filter(float value)
    {
        UpdateFilterParameters(m_XFilter);

        float distFromLastPos = Math.Abs(m_LastValue - value);
        float ratio = (distFromLastPos - InnerHysteresisWindowSizeM) /
            (OuterHysteresisWindowSizeM - InnerHysteresisWindowSizeM);

        ratio = Mathf.Clamp01(ratio);

        float result = value;
        if (!DoWindowFilterOnly)
        {
            result = m_XFilter.Filter(value);
        }

        result = (ratio * result) + ((1.0f - ratio) * m_LastValue);
        m_LastValue = result;
        return result;
    }

    /// <summary>
    /// Sets the updated filter parameters.
    /// </summary>
    /// <param name="filter">The filter the parameters will be applied to.</param>
    private void UpdateFilterParameters(SpeedAdaptiveFilter filter)
    {
        filter.SetBeta(BetaCutoffSlope);
        filter.SetMinimumCutoff(MinCutoff);
        filter.SetDerivateCutoff(DerivateCutoffFrequency);
    }
}
