//-----------------------------------------------------------------------
// <copyright file="SingleValueFilter.cs" company="Google LLC">
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
    public float InnerHysteresisWindowSizeM = _innerHysteresisWindowSizeM;

    /// <summary>
    /// Sets the out window size for smoothly blending from hysteresis to filtering value.
    /// </summary>
    public float OuterHysteresisWindowSizeM = _outerHysteresisWindowSizeM;

    /// <summary>
    /// Minimum cutoff value.
    /// </summary>
    public float MinCutoff = _minCutoff;

    /// <summary>
    /// Cutoff slope.
    /// </summary>
    public float BetaCutoffSlope = _betaCutoffSlope;

    /// <summary>
    /// Derivate cutoff frequency value.
    /// </summary>
    public float DerivateCutoffFrequency = _derivateCutoffFrequency;

    private const float _initialSensorFrequency = 60;
    private const float _innerHysteresisWindowSizeM = 0.003f;
    private const float _outerHysteresisWindowSizeM = 0.015f;
    private const float _minCutoff = 7f;
    private const float _betaCutoffSlope = 0.5f;
    private const float _derivateCutoffFrequency = 1f;
    private SpeedAdaptiveFilter _xFilter;
    private float _lastValue;
    private float _sensorFrequency = _initialSensorFrequency;

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
            return _lastValue;
        }
    }

    /// <summary>
    /// Reinitializes the filter. Use this after changing filter parameters.
    /// </summary>
    public void ReinitalizeFilter()
    {
        _xFilter = new SpeedAdaptiveFilter(
            _sensorFrequency, MinCutoff, BetaCutoffSlope, DerivateCutoffFrequency);
    }

    /// <summary>
    /// Smoothes the input value.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>Returns the filtered value.</returns>
    public float Filter(float value)
    {
        UpdateFilterParameters(_xFilter);

        float distFromLastPos = Math.Abs(_lastValue - value);
        float ratio = (distFromLastPos - InnerHysteresisWindowSizeM) /
            (OuterHysteresisWindowSizeM - InnerHysteresisWindowSizeM);

        ratio = Mathf.Clamp01(ratio);

        float result = value;
        if (!DoWindowFilterOnly)
        {
            result = _xFilter.Filter(value);
        }

        result = (ratio * result) + ((1.0f - ratio) * _lastValue);
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
