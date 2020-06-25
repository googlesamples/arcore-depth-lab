//-----------------------------------------------------------------------
// <copyright file="DepthOfFieldEffectController.cs" company="Google LLC">
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
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Depth-of-field is a physical effect in digital single-lens reflex (DSLR) cameras, but is missing
/// in most mobile phones. According to Wikipedia, depth-of-field is the distance between the
/// nearest and the furthest objects that are in acceptably sharp focus. [3] "Acceptably sharp
/// focus" is defined using a property called the circle of confusion.  With live depth input from
/// Motion Stereo (go/motion-stereo), we could implement depth-of-field effect with Gaussian
/// filters (go/gaussian-blur) in real time, see go/depth-of-field for more detail.
/// </summary>
public class DepthOfFieldEffectController : MonoBehaviour
{
    /// <summary>
    /// Material attached with depth-of-field effect Shader.
    /// </summary>
    public Material DepthOfFieldMaterial;

    /// <summary>
    /// Focus point or anchor for the depth of effect in the world space.
    /// </summary>
    public Transform FocusPoint;

    /// <summary>
    /// Automatically changes the focus point to the tapped surface vertex upon touch event.
    /// </summary>
    public bool TapToFocus = true;

    /// <summary>
    /// Whether to turn the peripheral region into greyscale.
    /// </summary>
    public bool GreyscalePeripheral = false;

    private const int k_PixelSkip = 4;

    private static readonly string k_AspectRatioPropertyName = "_AspectRatio";
    private static readonly string k_MinDepthPropertyName = "_MinDepth";
    private static readonly string k_DepthRangePropertyName = "_DepthRange";
    private static readonly string k_AperturePropertyName = "_Aperture";
    private static readonly string k_TouchPositionPropertyName = "_TouchPosition";
    private static readonly string k_NormalizedDepthMinName = "_NormalizedDepthMin";
    private static readonly string k_NormalizedDepthMaxName = "_NormalizedDepthMax";
    private static readonly string k_RenderModePropertyName = "_RenderMode";
    private static readonly string k_GreyscalePheripheralPropertyName = "_GreyscalePheripheral";
    private Vector3 m_ScreenAnchorPosition = new Vector3(0.5f, 0.5f, 1f);
    private DepthOfFieldRenderMode m_RenderMode = DepthOfFieldRenderMode.FocusOnProjectedPoint;
    private float m_Aperture = 0.9f;
    private float m_LastTouchTimestamp = 0f;

    /// <summary>
    /// Rendering modes of the demo scene:
    /// * FocusOnWorldAnchor shows depth-of-field with focused point at a 3D anchor.
    /// * FocusOnScreenPoint shows depth-of-field with focused point at a screen point.
    /// * FocusOnProjectedPoint shows depth-of-field with screen point projected to world.
    /// * DepthMap shows the underlying depth map.
    /// * CameraImage shows the input camera stream.
    /// </summary>
    private enum DepthOfFieldRenderMode
    {
        FocusOnWorldAnchor,
        FocusOnScreenPoint,
        FocusOnProjectedPoint,
        DepthMap,
        CameraImage
    }

    /// <summary>
    /// Switches to the next depth-of-field mode.
    /// </summary>
    public void SwitchToNextDepthOfFieldMode()
    {
        var numModes = System.Enum.GetValues(typeof(DepthOfFieldRenderMode)).Length;
        m_RenderMode = (DepthOfFieldRenderMode)(((int)m_RenderMode + 1) % numModes);
    }

    /// <summary>
    /// Toggles the greyscale of the pheripheral region.
    /// </summary>
    public void ToggleGreyscalePheripheral()
    {
        GreyscalePeripheral = !GreyscalePeripheral;
    }

    /// <summary>
    /// Change the aperture with a Slider's value. More contrast with higher aperture.
    /// </summary>
    /// <param name="slider">An UI slider in the scene.</param>
    public void ChangeAperture(Slider slider)
    {
        if (m_Aperture != slider.value)
        {
            m_Aperture = slider.value;
            GreyscalePeripheral = true;
            m_LastTouchTimestamp = Time.time;
        }
    }

    private void Start()
    {
        if (!TapToFocus)
        {
            FocusPoint = new GameObject().transform;
        }

        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        FocusPoint.position = DepthSource.GetVertexInWorldSpaceFromScreenUV(screenCenter);
        Update();
    }

    private void Update()
    {
        UpdateShaderVariables();
        ProcessTouch();
    }

    private void UpdateShaderVariables()
    {
        if (m_RenderMode == DepthOfFieldRenderMode.FocusOnProjectedPoint)
        {
            m_ScreenAnchorPosition = Camera.main.WorldToScreenPoint(FocusPoint.position);
            m_ScreenAnchorPosition.x /= Screen.width;
            m_ScreenAnchorPosition.y /= Screen.height;
        }

        // Cancels the highlighting effect when the user has not touched the slider for half second.
        if (Time.time - m_LastTouchTimestamp > 0.5f)
        {
            GreyscalePeripheral = false;
        }

        DepthOfFieldMaterial.SetVector(k_TouchPositionPropertyName, m_ScreenAnchorPosition);
        Vector2 aspectRatio = new Vector2(Screen.height / Screen.width, 1f);
        DepthOfFieldMaterial.SetVector(k_AspectRatioPropertyName, aspectRatio);
        DepthOfFieldMaterial.SetFloat(k_AperturePropertyName, m_Aperture);

        // Updates the rendering mode.
        DepthOfFieldMaterial.SetInt(k_RenderModePropertyName, (int)m_RenderMode);
        DepthOfFieldMaterial.SetInt(k_GreyscalePheripheralPropertyName,
                                    GreyscalePeripheral ? 1 : 0);

        // Updates the values related to DepthSource.
        if (!DepthSource.Initialized)
        {
            return;
        }

        var depthArray = DepthSource.DepthArray;
        var minDepth = depthArray[0];
        var maxDepth = depthArray[0];
        var mapHeight = DepthSource.DepthHeight;
        var mapWidth = DepthSource.DepthWidth;

        // Looks up the global minimum and maximum depth value for depth normalization.
        for (int i = 0; i < mapHeight; i += k_PixelSkip)
        {
            for (int j = 0; j < mapWidth; j += k_PixelSkip)
            {
                var depth = depthArray[(i * mapWidth) + j];
                if (depth > maxDepth)
                {
                    maxDepth = depth;
                }
                else if (depth < minDepth)
                {
                    minDepth = depth;
                }
            }
        }

        // Updates the minimum depth and depth range in meters in the shader.
        float minDepthM = minDepth * DepthSource.MillimeterToMeter;
        float maxDepthM = maxDepth * DepthSource.MillimeterToMeter;
        DepthOfFieldMaterial.SetFloat(k_MinDepthPropertyName, minDepthM);
        DepthOfFieldMaterial.SetFloat(k_DepthRangePropertyName, maxDepthM - minDepthM);
    }

    private void ProcessTouch()
    {
        if (Input.touchCount < 1)
        {
            return;
        }

        Touch touch = Input.GetTouch(0);

        // Avoids touch event in the top bar and bottom slider.
        if (touch.position.y < 0.2 * Screen.height || touch.position.y > 0.8 * Screen.height)
        {
            return;
        }

        // Computes the view-space anchor based on rendering mode.
        switch (m_RenderMode)
        {
            case DepthOfFieldRenderMode.FocusOnWorldAnchor:
                m_ScreenAnchorPosition = Camera.main.WorldToScreenPoint(FocusPoint.position);
                float depthMin = DepthOfFieldMaterial.GetFloat(k_NormalizedDepthMinName);
                float depthMax = DepthOfFieldMaterial.GetFloat(k_NormalizedDepthMaxName);
                float depthRange = depthMax - depthMin;

                m_ScreenAnchorPosition.x /= Screen.width;
                m_ScreenAnchorPosition.y /= Screen.height;
                float zScreenAnchorPosition =
                    (m_ScreenAnchorPosition.z - depthMin) / depthRange;
                m_ScreenAnchorPosition.z = Mathf.Clamp01(zScreenAnchorPosition);
                break;

            case DepthOfFieldRenderMode.FocusOnProjectedPoint:
            case DepthOfFieldRenderMode.FocusOnScreenPoint:
                m_ScreenAnchorPosition.z = Input.touchCount >= 1 ? 1 : 0;
                break;
        }

        // For the first touch, computes the view-space anchor based on rendering mode.
        switch (m_RenderMode)
        {
            case DepthOfFieldRenderMode.FocusOnProjectedPoint:
            case DepthOfFieldRenderMode.FocusOnWorldAnchor:
                FocusPoint.position = DepthSource.GetVertexInWorldSpaceFromScreenXY(
                                        (int)touch.position.x, (int)touch.position.y,
                                        DepthSource.DepthArray);
                break;

            case DepthOfFieldRenderMode.FocusOnScreenPoint:
                // Touch position corresponds to the image UV in the portrait mode.
                // Depth map UV is in landscape mode.
                m_ScreenAnchorPosition = new Vector3(touch.position.x / Screen.width,
                                                        touch.position.y / Screen.height,
                                                        1f);
                break;
        }
    }
}
