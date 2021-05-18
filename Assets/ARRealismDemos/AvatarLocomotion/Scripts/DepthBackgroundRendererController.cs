//-----------------------------------------------------------------------
// <copyright file="DepthBackgroundRendererController.cs" company="Google LLC">
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
using GoogleARCore;
using GoogleARCoreInternal;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Manages the visualization between the camera background and the depth map.
/// </summary>
public class DepthBackgroundRendererController : MonoBehaviour
{
    /// <summary>
    /// First person camera.
    /// </summary>
    public GameObject FirstPersonCamera;

    private const string _brightnessPropertyName = "_Brightness";
    private const string _gammaCorrectionPropertyName = "_GammaCorrection";

    // Whether or not to enable gamma correction for the camera image.
    private bool _gammaCorrection = true;

    /// <summary>
    /// A material used to render the AR background image.
    /// </summary>
    private DemoARBackgroundRenderer _backgroundRenderer;

    /// <summary>
    /// Enables or disables gamma correction.
    /// </summary>
    /// <param name="gamma">Enable gamma.</param>
    public void EnableGammaCorrection(bool gamma = true)
    {
        _gammaCorrection = gamma;
    }

    private void UpdateShaderVariables()
    {
        var bgMaterial = _backgroundRenderer.BackgroundMaterial;

        // Disables the fading transition.
        if (bgMaterial != null)
        {
            bgMaterial.SetFloat(_brightnessPropertyName, 1.0f);
            bgMaterial.SetFloat(_gammaCorrectionPropertyName, _gammaCorrection ? 1f : 0f);
        }
    }

    private void Start()
    {
        if (FirstPersonCamera == null)
        {
            FirstPersonCamera = Camera.main.gameObject;
        }

        _backgroundRenderer = FirstPersonCamera.GetComponent<DemoARBackgroundRenderer>();
    }

    private void Update()
    {
        UpdateShaderVariables();
    }
}
