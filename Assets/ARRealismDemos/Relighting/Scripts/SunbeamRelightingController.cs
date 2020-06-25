//-----------------------------------------------------------------------
// <copyright file="SunbeamRelightingController.cs" company="Google LLC">
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
    public bool UseSparseDepth = false;

    private static readonly string k_LightAnchorPositionName = "_LightAnchorPosition";
    private static readonly string k_GlobalAlphaValueName = "_GlobalAlphaValue";
    private static readonly string k_NormalizedDepthMinName = "_NormalizedDepthMin";
    private static readonly string k_NormalizedDepthMaxName = "_NormalizedDepthMax";

    // Relative position of the light to the anchor placed on a surface.
    private static readonly Vector3 k_LightRelativePosition = new Vector3(0, 0.5f, 0);

    private Vector3 m_LightAnchorPosition = Vector3.zero;
    private Vector3 m_SunPosition = k_LightRelativePosition;
    private Vector2 m_LastTouchPosition;
    private bool m_Initialized = false;
    private bool m_FollowScreenCenter = false;
    private float m_GlobalAlphaValue = 1.0f;

    /// <summary>
    /// Initializes the sun position.
    /// </summary>
    private void Start()
    {
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        m_SunPosition = DepthSource.GetVertexInWorldSpaceFromScreenUV(screenCenter);
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

        m_SunPosition = worldPosition + k_LightRelativePosition;
        m_LastTouchPosition = touch.position;
        m_Initialized = true;
    }

    /// <summary>
    /// Updates touch events and passes uniform values to the GPU shader.
    /// </summary>
    private void Update()
    {
        if (m_FollowScreenCenter)
        {
            var worldPosition = DepthSource.GetVertexInWorldSpaceFromScreenXY(
                                Screen.width / 2, Screen.height / 2,
                                DepthSource.DepthArray);
            m_SunPosition = worldPosition + k_LightRelativePosition;
        }

        UpdateTouch();
        m_LightAnchorPosition = Camera.main.WorldToScreenPoint(m_SunPosition);
        float depthMin = RelightingMaterial.GetFloat(k_NormalizedDepthMinName);
        float depthMax = RelightingMaterial.GetFloat(k_NormalizedDepthMaxName);
        float depthRange = depthMax - depthMin;
        float zLightAnchorPosition = (m_LightAnchorPosition.z - depthMin) / depthRange;
        m_LightAnchorPosition.z = Mathf.Clamp01(zLightAnchorPosition);

        RelightingMaterial.SetVector(k_LightAnchorPositionName, m_LightAnchorPosition);
        RelightingMaterial.SetFloat(k_GlobalAlphaValueName, m_GlobalAlphaValue);
    }

    /// <summary>
    /// Applies the relighting (post-processing) effect after the material is initialized.
    /// </summary>
    /// <param name="sourceTexture">Camera image or composited image.</param>
    /// <param name="destTexture">Output texture.</param>
    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
    {
        if (RelightingMaterial != null && m_Initialized)
        {
            Graphics.Blit(sourceTexture, destTexture, RelightingMaterial);
        }
        else
        {
            Graphics.Blit(sourceTexture, destTexture);
        }
    }
}
