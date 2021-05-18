//-----------------------------------------------------------------------
// <copyright file="SpeedAdaptiveFilter.cs" company="Google LLC">
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is a speed adaptive low-pass filter for smooting sensor data.
/// This is an implementation based on the paper:
///     Casiez, G., Roussel, N. and Vogel, D. (2012).
///     1€ Filter: A Simple Speed-based Low-pass Filter for Noisy Input in Interactive Systems.
/// </summary>
public class SpeedAdaptiveFilter
{
    private float _lastValue;
    private float _sensorFrequency;
    private float _minCutoff;
    private float _betaCutoffSlope;
    private float _derivateCutoffFrequency;
    private LowPassFilter _xFilter;
    private LowPassFilter _dxFilter;
    private DateTime _prevTime;

    /// <summary>
    /// This is a speed adaptive low-pass filter for smooting sensor data.
    /// </summary>
    /// <param name="frequency">Sensor frequency value.</param>
    /// <param name="mincutoff">Mincutoff value.</param>
    /// <param name="beta">Beta value.</param>
    /// <param name="dcutoff">D cutoff value.</param>
    public SpeedAdaptiveFilter(
        float frequency, float mincutoff = 1, float beta = 0, float dcutoff = 1)
    {
        SetFrequency(frequency);
        SetMinimumCutoff(mincutoff);
        SetBeta(beta);
        SetDerivateCutoff(dcutoff);
        _xFilter = new LowPassFilter(GetWeight(mincutoff), 0);
        _dxFilter = new LowPassFilter(GetWeight(dcutoff), 0);
        _prevTime = DateTime.MinValue;
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
    /// Returns the weight.
    /// </summary>
    /// <param name="cutoff">Cutoff value.</param>
    /// <returns>The filter weight.</returns>
    public float GetWeight(float cutoff)
    {
        float te = 1f / _sensorFrequency;
        float tau = 1f / (2f * Mathf.PI * cutoff);
        return 1f / (1f + (tau / te));
    }

    /// <summary>
    /// Sets the sensor frequency.
    /// </summary>
    /// <param name="value">Frequency value.</param>
    public void SetFrequency(float value)
    {
        _sensorFrequency = value;
    }

    /// <summary>
    /// Sets the minimum cutoff.
    /// </summary>
    /// <param name="value">Minimum cutoff.</param>
    public void SetMinimumCutoff(float value)
    {
        _minCutoff = value;
    }

    /// <summary>
    /// Sets the beta value.
    /// </summary>
    /// <param name="value">Beta value.</param>
    public void SetBeta(float value)
    {
        _betaCutoffSlope = value;
    }

    /// <summary>
    /// Sets the derivative cutoff value.
    /// </summary>
    /// <param name="value">Derivative cutoff value.</param>
    public void SetDerivateCutoff(float value)
    {
        _derivateCutoffFrequency = value;
    }

    /// <summary>
    /// Smoothes the input value.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>Returns the filtered value.</returns>
    public float Filter(float value)
    {
        UpdateSensorFrequency();

        float dx = _xFilter.GetIsInitialized() ?
            (value - _xFilter.GetRawInput()) * _sensorFrequency :
            0f;

        float edx = _dxFilter.UpdateFilterValue(dx, GetWeight(_derivateCutoffFrequency));
        float cutoff = _minCutoff + (_betaCutoffSlope * Mathf.Abs(edx));

        _lastValue = _xFilter.UpdateFilterValue(value, GetWeight(cutoff));
        return _lastValue;
    }

    /// <summary>
    /// Updates the sensor frequency based on the frequency of the method call.
    /// </summary>
    private void UpdateSensorFrequency()
    {
        DateTime now = DateTime.Now;
        if (_prevTime != DateTime.MinValue)
        {
            _sensorFrequency = 1f / (float)(now - _prevTime).TotalSeconds;
        }

        _prevTime = now;
    }
}
