//-----------------------------------------------------------------------
// <copyright file="QuaternionFilter.cs" company="Google LLC">
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
    public float InnerHysteresisDegrees = _innerHysteresisDegrees;

    /// <summary>
    /// Sets the out window size for smoothly blending from hysteresis to filtering value.
    /// </summary>
    public float OuterHysteresisDegrees = _outerHysteresisDegrees;

    /// <summary>
    /// Minimum cutoff value.
    /// </summary>
    public float MinCutoff = _minCutoff;

    /// <summary>
    /// Beta cutoff slope value.
    /// </summary>
    public float BetaCutoffSlope = _betaCutoffSlope;

    /// <summary>
    /// Derivate cutoff frequency value.
    /// </summary>
    public float DerivateCutoffFrequency = _derivateCutoffFrequency;

    private const float _initialSensorFrequency = 60;
    private const float _innerHysteresisDegrees = 0.5f;
    private const float _outerHysteresisDegrees = 1.0f;
    private const float _minCutoff = 7f;
    private const float _betaCutoffSlope = 0.5f;
    private const float _derivateCutoffFrequency = 1f;
    private SpeedAdaptiveFilter _xFilter;
    private SpeedAdaptiveFilter _yFilter;
    private SpeedAdaptiveFilter _zFilter;
    private SpeedAdaptiveFilter _wFilter;
    private Quaternion _lastValue;
    private float _sensorFrequency = _initialSensorFrequency;

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
            return _lastValue;
        }
    }

    /// <summary>
    /// Reinitializes the filter. Use this after changing filter parameters.
    /// </summary>
    public void ReinitalizeFilter()
    {
        _xFilter = new SpeedAdaptiveFilter(_sensorFrequency, MinCutoff, BetaCutoffSlope,
            DerivateCutoffFrequency);
        _yFilter = new SpeedAdaptiveFilter(_sensorFrequency, MinCutoff, BetaCutoffSlope,
            DerivateCutoffFrequency);
        _zFilter = new SpeedAdaptiveFilter(_sensorFrequency, MinCutoff, BetaCutoffSlope,
            DerivateCutoffFrequency);
        _wFilter = new SpeedAdaptiveFilter(_sensorFrequency, MinCutoff, BetaCutoffSlope,
            DerivateCutoffFrequency);
    }

    /// <summary>
    /// Smoothes the input value.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>Returns the filtered value.</returns>
    public Quaternion Filter(Quaternion value)
    {
        UpdateFilterParameters(_xFilter);
        UpdateFilterParameters(_yFilter);
        UpdateFilterParameters(_zFilter);
        UpdateFilterParameters(_wFilter);

        Vector4 lastRot = new Vector4(
            _xFilter.LastValue,
            _yFilter.LastValue,
            _zFilter.LastValue,
            _wFilter.LastValue).normalized;

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

        float angleDist = Math.Abs(Quaternion.Angle(value, _lastValue));

        float ratio = (angleDist - InnerHysteresisDegrees) /
            (OuterHysteresisDegrees - InnerHysteresisDegrees);

        ratio = Mathf.Clamp01(ratio);

        Quaternion result = value;
        if (!DoWindowFilterOnly)
        {
            result.x = _xFilter.Filter(value.x);
            result.y = _yFilter.Filter(value.y);
            result.z = _zFilter.Filter(value.z);
            result.w = _wFilter.Filter(value.w);
        }

        result = Quaternion.Slerp(_lastValue, result, ratio);
        _lastValue = result;
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
