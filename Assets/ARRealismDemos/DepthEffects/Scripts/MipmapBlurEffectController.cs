//-----------------------------------------------------------------------
// <copyright file="MipmapBlurEffectController.cs" company="Google LLC">
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
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Depth of field is a physical effect in digital single-lens reflex (DSLR) cameras, but is missing
/// in most mobile phones. According to Wikipedia, Depth of field is the distance between the
/// nearest and the furthest objects that are in acceptably sharp focus. [3] "Acceptably sharp
/// focus" is defined using a property called the circle of confusion.  With live depth input from
/// Motion Stereo (go/motion-stereo), we could implement depth of field effect with Gaussian
/// filters (go/gaussian-blur) in real time, see go/depth-of-field for more detail.
///
/// Attaches and de-attaches the mipmap blur effect script to the main camera.
/// </summary>
public class MipmapBlurEffectController : MonoBehaviour
{
    /// <summary>
    /// Material attached with Depth of field effect Shader.
    /// </summary>
    public Material MipmapBlurMaterial;

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

    private const int _pixelSkip = 4;

    private static readonly string _aspectRatioPropertyName = "_AspectRatio";
    private static readonly string _minDepthPropertyName = "_MinDepth";
    private static readonly string _depthRangePropertyName = "_DepthRange";
    private static readonly string _aperturePropertyName = "_Aperture";
    private static readonly string _touchPositionPropertyName = "_TouchPosition";
    private static readonly string _normalizedDepthMinName = "_NormalizedDepthMin";
    private static readonly string _normalizedDepthMaxName = "_NormalizedDepthMax";
    private static readonly string _renderModePropertyName = "_RenderMode";
    private static readonly string _greyscalePheripheralPropertyName = "_GreyscalePheripheral";
    private Vector3 _screenAnchorPosition = new Vector3(0.5f, 0.5f, 1f);
    private DepthOfFieldRenderMode _renderMode = DepthOfFieldRenderMode.FocusOnProjectedPoint;
    private float _aperture = 0.9f;
    private float _lastTouchTimestamp = 0f;

    /// <summary>
    /// Rendering modes of the demo scene:
    /// * FocusOnWorldAnchor shows depth of field with focused point at a 3D anchor.
    /// * FocusOnScreenPoint shows depth of field with focused point at a screen point.
    /// </summary>
    private enum DepthOfFieldRenderMode
    {
        FocusOnWorldAnchor,
        FocusOnScreenPoint,
        FocusOnProjectedPoint
    }

    /// <summary>
    /// Switches to the next depth of field mode.
    /// </summary>
    public void SwitchToNextDepthOfFieldMode()
    {
        var numModes = System.Enum.GetValues(typeof(DepthOfFieldRenderMode)).Length;
        _renderMode = (DepthOfFieldRenderMode)(((int)_renderMode + 1) % numModes);
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
        if (_aperture != slider.value)
        {
            _aperture = slider.value;
            GreyscalePeripheral = true;
            _lastTouchTimestamp = Time.time;
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

        Camera.main.gameObject.AddComponent<MipmapBlurPrePostRender>();
        Update();
    }

    private void Update()
    {
        UpdateShaderVariables();
        ProcessTouch();
    }

    private void OnDestroy()
    {
        Destroy(Camera.main.gameObject.GetComponent<MipmapBlurPrePostRender>());
    }

    private void UpdateShaderVariables()
    {
        if (_renderMode == DepthOfFieldRenderMode.FocusOnProjectedPoint)
        {
            _screenAnchorPosition = Camera.main.WorldToScreenPoint(FocusPoint.position);
            _screenAnchorPosition.x /= Screen.width;
            _screenAnchorPosition.y /= Screen.height;
        }

        // Cancels the highlighting effect when the user has not touched the slider for half second.
        if (Time.time - _lastTouchTimestamp > 0.5f)
        {
            GreyscalePeripheral = false;
        }

        MipmapBlurMaterial.SetVector(_touchPositionPropertyName, _screenAnchorPosition);
        Vector2 aspectRatio = new Vector2(Screen.height / Screen.width, 1f);
        MipmapBlurMaterial.SetVector(_aspectRatioPropertyName, aspectRatio);
        MipmapBlurMaterial.SetFloat(_aperturePropertyName, _aperture);

        // Updates the rendering mode.
        MipmapBlurMaterial.SetInt(_renderModePropertyName, (int)_renderMode);
        MipmapBlurMaterial.SetInt(_greyscalePheripheralPropertyName,
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
        for (int i = 0; i < mapHeight; i += _pixelSkip)
        {
            for (int j = 0; j < mapWidth; j += _pixelSkip)
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
        MipmapBlurMaterial.SetFloat(_minDepthPropertyName, minDepthM);
        MipmapBlurMaterial.SetFloat(_depthRangePropertyName, maxDepthM - minDepthM);
    }

    private void ProcessTouch()
    {
        Touch touch = Input.GetTouch(0);

        // Avoids touch event in the top bar and bottom slider.
        if (touch.position.y < 0.2 * Screen.height || touch.position.y > 0.8 * Screen.height)
        {
            return;
        }

        if (Input.touchCount == 1)
        {
            // Computes the view-space anchor based on rendering mode.
            switch (_renderMode)
            {
                case DepthOfFieldRenderMode.FocusOnWorldAnchor:
                    _screenAnchorPosition = Camera.main.WorldToScreenPoint(FocusPoint.position);
                    float depthMin = MipmapBlurMaterial.GetFloat(_normalizedDepthMinName);
                    float depthMax = MipmapBlurMaterial.GetFloat(_normalizedDepthMaxName);
                    float depthRange = depthMax - depthMin;

                    _screenAnchorPosition.x /= Screen.width;
                    _screenAnchorPosition.y /= Screen.height;
                    float zScreenAnchorPosition =
                        (_screenAnchorPosition.z - depthMin) / depthRange;
                    _screenAnchorPosition.z = Mathf.Clamp01(zScreenAnchorPosition);
                    break;

                case DepthOfFieldRenderMode.FocusOnProjectedPoint:
                case DepthOfFieldRenderMode.FocusOnScreenPoint:
                    _screenAnchorPosition.z = Input.touchCount >= 1 ? 1 : 0;
                    break;
            }
        }

        if (Input.touchCount > 0)
        {
            // For the first touch, computes the view-space anchor based on rendering mode.
            switch (_renderMode)
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
                    _screenAnchorPosition = new Vector3(touch.position.x / Screen.width,
                                                         touch.position.y / Screen.height,
                                                         1f);
                    break;
            }
        }
    }
}
