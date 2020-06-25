//-----------------------------------------------------------------------
// <copyright file="SpeedAdaptiveFilter.cs" company="Google LLC">
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
    private float m_LastValue;
    private float m_SensorFrequency;
    private float m_MinCutoff;
    private float m_BetaCutoffSlope;
    private float m_DerivateCutoffFrequency;
    private LowPassFilter m_XFilter;
    private LowPassFilter m_DXFilter;
    private DateTime m_PrevTime;

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
        m_XFilter = new LowPassFilter(GetWeight(mincutoff), 0);
        m_DXFilter = new LowPassFilter(GetWeight(dcutoff), 0);
        m_PrevTime = DateTime.MinValue;
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
    /// Returns the weight.
    /// </summary>
    /// <param name="cutoff">Cutoff value.</param>
    /// <returns>The filter weight.</returns>
    public float GetWeight(float cutoff)
    {
        float te = 1f / m_SensorFrequency;
        float tau = 1f / (2f * Mathf.PI * cutoff);
        return 1f / (1f + (tau / te));
    }

    /// <summary>
    /// Sets the sensor frequency.
    /// </summary>
    /// <param name="value">Frequency value.</param>
    public void SetFrequency(float value)
    {
        m_SensorFrequency = value;
    }

    /// <summary>
    /// Sets the minimum cutoff.
    /// </summary>
    /// <param name="value">Minimum cutoff.</param>
    public void SetMinimumCutoff(float value)
    {
        m_MinCutoff = value;
    }

    /// <summary>
    /// Sets the beta value.
    /// </summary>
    /// <param name="value">Beta value.</param>
    public void SetBeta(float value)
    {
        m_BetaCutoffSlope = value;
    }

    /// <summary>
    /// Sets the derivative cutoff value.
    /// </summary>
    /// <param name="value">Derivative cutoff value.</param>
    public void SetDerivateCutoff(float value)
    {
        m_DerivateCutoffFrequency = value;
    }

    /// <summary>
    /// Smoothes the input value.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>Returns the filtered value.</returns>
    public float Filter(float value)
    {
        UpdateSensorFrequency();

        float dx = m_XFilter.GetIsInitialized() ?
            (value - m_XFilter.GetRawInput()) * m_SensorFrequency :
            0f;

        float edx = m_DXFilter.UpdateFilterValue(dx, GetWeight(m_DerivateCutoffFrequency));
        float cutoff = m_MinCutoff + (m_BetaCutoffSlope * Mathf.Abs(edx));

        m_LastValue = m_XFilter.UpdateFilterValue(value, GetWeight(cutoff));
        return m_LastValue;
    }

    /// <summary>
    /// Updates the sensor frequency based on the frequency of the method call.
    /// </summary>
    private void UpdateSensorFrequency()
    {
        DateTime now = DateTime.Now;
        if (m_PrevTime != DateTime.MinValue)
        {
            m_SensorFrequency = 1f / (float)(now - m_PrevTime).TotalSeconds;
        }

        m_PrevTime = now;
    }
}
