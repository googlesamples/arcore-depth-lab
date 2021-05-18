//-----------------------------------------------------------------------
// <copyright file="BackgroundToFogEffectController.cs" company="Google LLC">
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
using UnityEngine.UI;

/// <summary>
/// Blends exponential fog with the camera view based on the real-time depth map.
/// </summary>
public class BackgroundToFogEffectController : MonoBehaviour
{
    /// <summary>
    /// Material attached with Background Depth Fog Shader.
    /// </summary>
    public Material BackgroundToFogEffectMaterial;

    /// <summary>
    /// Slider that controls amount of fog effect.
    /// </summary>
    public UnityEngine.UI.Slider UISlider;

    /// <summary>
    /// Whether to output fog effect parameters in the console.
    /// </summary>
    public bool DebugFogEffect = false;
    private const float _fogMinDistance = 0.01f;
    private const float _fogMaxDistance = 7.0f;
    private const float _fogMinThickness = 0.25f;
    private const float _fogMaxThickness = 8.0f;
    private static readonly string _halfFogDistancePropertyName = "_FogDistance";
    private static readonly string _halfFogThicknessPropertyName = "_FogThickness";
    private static readonly string _halfFogColorPropertyName = "_FogColor";
    private float _fogThickness;
    private float _fogDistance;

    private void Start()
    {
        _fogDistance = GetFogDistance(/*fogDistanceParam=*/0);
        _fogThickness = GetFogThickness(UISlider.value);
        UpdateShaderVariables();
    }

    private void Update()
    {
        _fogThickness = GetFogThickness(UISlider.value);
        UpdateShaderVariables();
    }

    private void UpdateShaderVariables()
    {
        BackgroundToFogEffectMaterial.SetFloat(_halfFogDistancePropertyName, _fogDistance);
        BackgroundToFogEffectMaterial.SetFloat(_halfFogThicknessPropertyName, _fogThickness);
        BackgroundToFogEffectMaterial.SetColor(_halfFogColorPropertyName, Color.white);
    }

    private float GetFogDistance(float fogDistanceParam)
    {
        return _fogMinDistance + ((_fogMaxDistance - _fogMinDistance) * fogDistanceParam);
    }

    private float GetFogThickness(float fogThicknessParam)
    {
        return _fogMinThickness +
          (_fogMaxThickness * Mathf.SmoothStep(0.0f, 1.0f, 1f - fogThicknessParam));
    }

    private float RemapValue(float value, float low1, float high1, float low2, float high2)
    {
        return low2 + (((value - low1) * (high2 - low2)) / (high1 - low1));
    }
}
