//-----------------------------------------------------------------------
// <copyright file="BackgroundToFogEffectController.cs" company="Google LLC">
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

    // /// <summary>
    // /// Slider transform.
    // /// </summary>
    // public RectTransform SliderFogDepth;

    // /// <summary>
    // /// Slider start position.
    // /// </summary>
    // public RectTransform SliderFogStart;

    /// <summary>
    /// Slider that controls amount of fog effect.
    /// </summary>
    public UnityEngine.UI.Slider UISlider;

    /// <summary>
    /// Whether to output fog effect parameters in the console.
    /// </summary>
    public bool DebugFogEffect = false;

    private static readonly string
    k_HalfFogDistancePropertyName = "_FogDistance";

    private static readonly string
    k_HalfFogThicknessPropertyName = "_FogThickness";

    private static readonly string
    k_HalfFogColorPropertyName = "_FogColor";

    private float m_FogDistance;
    private float m_FogMinDistance;
    private float m_FogMaxDistance;
    private float m_FogThickness;
    private float m_FogMinThickness;
    private float m_FogMaxThickness;

    private void Start()
    {
        m_FogMinDistance = 0.01f;
        m_FogMaxDistance = 7.0f;
        m_FogMinThickness = 0.25f;
        m_FogMaxThickness = 8.0f;
        float m_FogParamStart = 0.0f;
        m_FogDistance = GetFogDistance(m_FogParamStart);
        m_FogThickness = GetFogThickness(UISlider.value);
        UpdateShaderVariables();
    }

    private void Update()
    {
        m_FogThickness = GetFogThickness(UISlider.value);
        UpdateShaderVariables();
    }

    private void UpdateShaderVariables()
    {
        BackgroundToFogEffectMaterial.SetFloat(k_HalfFogDistancePropertyName, m_FogDistance);
        BackgroundToFogEffectMaterial.SetFloat(k_HalfFogThicknessPropertyName, m_FogThickness);
        BackgroundToFogEffectMaterial.SetColor(k_HalfFogColorPropertyName, Color.white);
    }

    private void OnEnable()
    {
        // Do nothing.
    }

    private float GetFogDistance(float fogDistanceParam)
    {
        return m_FogMinDistance + ((m_FogMaxDistance - m_FogMinDistance) * fogDistanceParam);
    }

    private float GetFogThickness(float fogThicknessParam)
    {
        return m_FogMinThickness +
          (m_FogMaxThickness * Mathf.SmoothStep(0.0f, 1.0f, fogThicknessParam));
    }

    private float RemapValue(float value, float low1, float high1, float low2, float high2)
    {
        return low2 + (((value - low1) * (high2 - low2)) / (high1 - low1));
    }
}
