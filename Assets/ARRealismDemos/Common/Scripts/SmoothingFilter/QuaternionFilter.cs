//-----------------------------------------------------------------------
// <copyright file="QuaternionFilter.cs" company="Google LLC">
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
/// This is a speed adaptive low-pass filter for smoothing rotation data.
/// This is an implementation based on the paper:
///     Casiez, G., Roussel, N. and Vogel, D. (2012).
///     1€ Filter: A Simple Speed-based Low-pass Filter for Noisy Input in Interactive Systems.
/// </summary>
public class QuaternionFilter : MonoBehaviour
{
    /// <summary>
    /// Enables hysterisis-only noise filtering.
    /// </summary>
    public bool DoWindowFilterOnly;

    /// <summary>
    /// Sets the inner window size for ignoring any changes.
    /// </summary>
    public float InnerHysteresisDegrees = k_InnerHysteresisDegrees;

    /// <summary>
    /// Sets the out window size for smoothly blending from hysteresis to filtering value.
    /// </summary>
    public float OuterHysteresisDegrees = k_OuterHysteresisDegrees;

    /// <summary>
    /// Minimum cutoff value.
    /// </summary>
    public float MinCutoff = k_MinCutoff;

    /// <summary>
    /// Beta cutoff slope value.
    /// </summary>
    public float BetaCutoffSlope = k_BetaCutoffSlope;

    /// <summary>
    /// Derivate cutoff frequency value.
    /// </summary>
    public float DerivateCutoffFrequency = k_DerivateCutoffFrequency;

    private const float k_SensorFrequency = 60;
    private const float k_InnerHysteresisDegrees = 0.5f;
    private const float k_OuterHysteresisDegrees = 1.0f;
    private const float k_MinCutoff = 7f;
    private const float k_BetaCutoffSlope = 0.5f;
    private const float k_DerivateCutoffFrequency = 1f;
    private SpeedAdaptiveFilter m_XFilter;
    private SpeedAdaptiveFilter m_YFilter;
    private SpeedAdaptiveFilter m_ZFilter;
    private SpeedAdaptiveFilter m_WFilter;
    private Quaternion m_LastValue;
    private float m_SensorFrequency = k_SensorFrequency;

    /// <summary>
    /// This is a speed-adaptive rotation filter for Quaternion input.
    /// </summary>
    public QuaternionFilter()
    {
        ReinitalizeFilter();
    }

    /// <summary>
    /// Gets the last filter value.
    /// </summary>
    public Quaternion LastValue
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
        m_XFilter = new SpeedAdaptiveFilter(m_SensorFrequency, MinCutoff, BetaCutoffSlope,
            DerivateCutoffFrequency);
        m_YFilter = new SpeedAdaptiveFilter(m_SensorFrequency, MinCutoff, BetaCutoffSlope,
            DerivateCutoffFrequency);
        m_ZFilter = new SpeedAdaptiveFilter(m_SensorFrequency, MinCutoff, BetaCutoffSlope,
            DerivateCutoffFrequency);
        m_WFilter = new SpeedAdaptiveFilter(m_SensorFrequency, MinCutoff, BetaCutoffSlope,
            DerivateCutoffFrequency);
    }

    /// <summary>
    /// Smoothes the input value.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>Returns the filtered value.</returns>
    public Quaternion Filter(Quaternion value)
    {
        UpdateFilterParameters(m_XFilter);
        UpdateFilterParameters(m_YFilter);
        UpdateFilterParameters(m_ZFilter);
        UpdateFilterParameters(m_WFilter);

        Vector4 lastRot = new Vector4(
            m_XFilter.LastValue,
            m_YFilter.LastValue,
            m_ZFilter.LastValue,
            m_WFilter.LastValue).normalized;

        Vector4 currentRot = new Vector4(
            value.x,
            value.y,
            value.z,
            value.w).normalized;

        // This ensures that rotations that avoid negative values can be used as well.
        if ((lastRot - currentRot).sqrMagnitude > 2)
        {
            value = new Quaternion(
                -currentRot.x,
                -currentRot.y,
                -currentRot.z,
                -currentRot.w);
        }

        float angleDist = Math.Abs(Quaternion.Angle(value, m_LastValue));

        float ratio = (angleDist - InnerHysteresisDegrees) /
            (OuterHysteresisDegrees - InnerHysteresisDegrees);

        ratio = Mathf.Clamp01(ratio);

        Quaternion result = value;
        if (!DoWindowFilterOnly)
        {
            result.x = m_XFilter.Filter(value.x);
            result.y = m_YFilter.Filter(value.y);
            result.z = m_ZFilter.Filter(value.z);
            result.w = m_WFilter.Filter(value.w);
        }

        result = Quaternion.Slerp(m_LastValue, result, ratio);
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
