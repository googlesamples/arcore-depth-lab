//-----------------------------------------------------------------------
// <copyright file="SunbeamRelightingController.cs" company="Google LLC">
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
using GoogleARCore.Examples.Common;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Renders a sun beam in the physical world at an offset above the surface where user touches.
/// </summary>
public class SunbeamRelightingController : MonoBehaviour
{
    /// <summary>
    /// The material attached with the relighting shader.
    /// </summary>
    public Material RelightingMaterial;

    /// <summary>
    /// Whether the user is debugging.
    /// </summary>
    public bool Debugging = true;

    /// <summary>
    /// Type of depth texture to attach to the material.
    /// </summary>
    public bool UseRawDepth = false;

    private static readonly string _lightAnchorPositionName = "_LightAnchorPosition";
    private static readonly string _globalAlphaValueName = "_GlobalAlphaValue";
    private static readonly string _normalizedDepthMinName = "_NormalizedDepthMin";
    private static readonly string _normalizedDepthMaxName = "_NormalizedDepthMax";

    // Relative position of the light to the anchor placed on a surface.
    private static readonly Vector3 _lightRelativePosition = new Vector3(0, 0.5f, 0);

    private Vector3 _lightAnchorPosition = Vector3.zero;
    private Vector3 _sunPosition = _lightRelativePosition;
    private Vector2 _lastTouchPosition;
    private bool _initialized = false;
    private bool _followScreenCenter = false;
    private float _globalAlphaValue = 1.0f;

    /// <summary>
    /// Initializes the sun position.
    /// </summary>
    private void Start()
    {
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        _sunPosition = DepthSource.GetVertexInWorldSpaceFromScreenUV(screenCenter);
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
    }

    /// <summary>
    /// Updates the sun position to an offset above the surface where the user touches.
    /// </summary>
    private void UpdateTouch()
    {
        // If the player has not touched the screen, we are done with this update.
        Touch touch;
        if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
        {
            return;
        }

        var worldPosition = DepthSource.GetVertexInWorldSpaceFromScreenXY(
                               (int)touch.position.x, (int)touch.position.y,
                               DepthSource.DepthArray);

        _sunPosition = worldPosition + _lightRelativePosition;
        _lastTouchPosition = touch.position;
        _initialized = true;
    }

    /// <summary>
    /// Updates touch events and passes uniform values to the GPU shader.
    /// </summary>
    private void Update()
    {
        if (_followScreenCenter)
        {
            var worldPosition = DepthSource.GetVertexInWorldSpaceFromScreenXY(
                                Screen.width / 2, Screen.height / 2,
                                DepthSource.DepthArray);
            _sunPosition = worldPosition + _lightRelativePosition;
        }

        UpdateTouch();
        _lightAnchorPosition = Camera.main.WorldToScreenPoint(_sunPosition);
        float depthMin = RelightingMaterial.GetFloat(_normalizedDepthMinName);
        float depthMax = RelightingMaterial.GetFloat(_normalizedDepthMaxName);
        float depthRange = depthMax - depthMin;
        float zLightAnchorPosition = (_lightAnchorPosition.z - depthMin) / depthRange;
        _lightAnchorPosition.z = Mathf.Clamp01(zLightAnchorPosition);

        RelightingMaterial.SetVector(_lightAnchorPositionName, _lightAnchorPosition);
        RelightingMaterial.SetFloat(_globalAlphaValueName, _globalAlphaValue);
    }

    /// <summary>
    /// Applies the relighting (post-processing) effect after the material is initialized.
    /// </summary>
    /// <param name="sourceTexture">Camera image or composited image.</param>
    /// <param name="destTexture">Output texture.</param>
    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
    {
        if (RelightingMaterial != null && _initialized)
        {
            Graphics.Blit(sourceTexture, destTexture, RelightingMaterial);
        }
        else
        {
            Graphics.Blit(sourceTexture, destTexture);
        }
    }
}
