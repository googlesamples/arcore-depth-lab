//-----------------------------------------------------------------------
// <copyright file="LowPassFilter.cs" company="Google LLC">
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This low pass filter allows a single value to be smoothed.
/// </summary>
public class LowPassFilter
{
    private float _outputSmoothed;
    private float _weight;
    private float _inputData;
    private bool _isInitialized;

    /// <summary>
    /// Creates an instance of the filter with a weight and initial value.
    /// </summary>
    /// <param name="weight">Weight of the filter.</param>
    /// <param name="initialValue">Initial value of the filter.</param>
    public LowPassFilter(float weight, float initialValue)
    {
        _weight = weight;
        _inputData = initialValue;
        _outputSmoothed = initialValue;
        _isInitialized = false;
    }

    /// <summary>
    /// Sets the weight.
    /// </summary>
    /// <param name="weight">Updates the weight.</param>
    public void SetWeight(float weight)
    {
        _weight = weight;
    }

    /// <summary>
    /// Gives access to the raw input data.
    /// </summary>
    /// <returns>Returns the raw input data.</returns>
    public float GetRawInput()
    {
        return _inputData;
    }

    /// <summary>
    /// Checks whether the filter is initialized.
    /// </summary>
    /// <returns>Returns the initialization state.</returns>
    public bool GetIsInitialized()
    {
        return _isInitialized;
    }

    /// <summary>
    /// Smoothes the input value with a pre-set weight.
    /// </summary>
    /// <param name="val">Input value.</param>
    /// <returns>Returns the filtered value.</returns>
    public float UpdateFilterValue(float val)
    {
        // Checks for not a value or infinity.
        if (System.Single.IsNaN(val) || System.Single.IsInfinity(val))
        {
            return _outputSmoothed;
        }

        if (_isInitialized)
        {
            _outputSmoothed = (_weight * val) + ((1f - _weight) * _outputSmoothed);
        }
        else
        {
            _outputSmoothed = val;
            _isInitialized = true;
        }

        _inputData = val;
        return _outputSmoothed;
    }

    /// <summary>
    /// Smoothes the input value while also using a new weight.
    /// </summary>
    /// <param name="val">Input value.</param>
    /// <param name="weight">The weight of the filter operation.</param>
    /// <returns>Returns the filtered value.</returns>
    public float UpdateFilterValue(float val, float weight)
    {
        SetWeight(weight);
        return UpdateFilterValue(val);
    }
}
