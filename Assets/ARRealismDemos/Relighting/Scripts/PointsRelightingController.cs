//-----------------------------------------------------------------------
// <copyright file="PointsRelightingController.cs" company="Google LLC">
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
/// Scene relighting with multiple point lights. Three points lights are rendered simultaneously
/// around the position of the center oriented reticle, or FocusPoint which the user touched on
/// the screen. The positions of points lights are animated with sine and cosine functions within
/// a bounding box of 0.3 meters.
/// </summary>
public class PointsRelightingController : MonoBehaviour
{
    /// <summary>
    /// List of point lights.
    /// </summary>
    public GameObject[] PointLights;

    /// <summary>
    /// Material attached with the relighting Shader.
    /// </summary>
    public Material RelitMaterial;

    /// <summary>
    /// Focus point or anchor for the point lights in the world space.
    /// </summary>
    public Transform FocusPoint;

    private const int _maxNumPointlights = 3;
    private static readonly string _aspectRatioPropertyName = "_AspectRatio";
    private static readonly string _touchPositionPropertyName = "_TouchPosition";
    private static readonly Vector3 _lightOffset = new Vector3(0, 0.4f, 0);
    private Vector4[] _pointlightPositions = new Vector4[_maxNumPointlights];
    private float[] _pointlightIntensities = new float[_maxNumPointlights];
    private Vector4[] _pointlightColors = new Vector4[_maxNumPointlights];
    private Vector3 _screenAnchorPosition = new Vector3(0.5f, 0.5f, 1f);
    private float _globalDarkness = 0.9f;
    private bool _enableColorDepthMode = true;
    private RelightingMode _renderMode = RelightingMode.LightsFollowScreenCenter;

    /// <summary>
    /// Relighting mode of the demo scene:
    /// * LightsFollowScreenCenter shows the relighting effect following the camera's position.
    /// * LightsAnchoredByTouch shows the relighting effect anchored by user's touch.
    /// * ColorImage shows the original color image for comparison.
    /// * DepthMap shows the input depth map.
    /// </summary>
    private enum RelightingMode
    {
        LightsFollowScreenCenter,
        LightsAnchoredByTouch,
        ColorImage,
        DepthMap
    }

    /// <summary>
    /// Toggles the mode to render relgithing effects.
    /// </summary>
    public void SwitchMode()
    {
        // Enables full modes when _enableColorDepthMode = True, otherwise the first two modes.
        var numModes =
            _enableColorDepthMode ? System.Enum.GetValues(typeof(RelightingMode)).Length : 2;
        _renderMode = (RelightingMode)(((int)_renderMode + 1) % numModes);
    }

    /// <summary>
    /// Changes the intensity of the relighting effects.
    /// </summary>
    /// <param name="slider">An UI slider in the scene.</param>
    public void ChangeRelightingIntensity(Slider slider)
    {
        _globalDarkness = slider.value;
    }

    /// <summary>
    /// Sets the focus point to be the center of the screen on start.
    /// </summary>
    private void Start()
    {
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        FocusPoint.position = DepthSource.GetVertexInWorldSpaceFromScreenUV(screenCenter);
        Update();
    }

    /// <summary>
    /// Updates the shader uniform varaiables and processes touch event per frame.
    /// </summary>
    private void Update()
    {
        UpdateShaderVariables();
        ProcessTouch();
    }

    private void UpdateShaderVariables()
    {
        if (_renderMode == RelightingMode.LightsFollowScreenCenter)
        {
            FocusPoint.position = DepthSource.GetVertexInWorldSpaceFromScreenXY(
                                    Screen.width / 2, Screen.height / 2,
                                    DepthSource.DepthArray);
        }

        _screenAnchorPosition = Camera.main.WorldToScreenPoint(FocusPoint.position);
        _screenAnchorPosition.x /= Screen.width;
        _screenAnchorPosition.y /= Screen.height;

        RelitMaterial.SetVector(_touchPositionPropertyName, _screenAnchorPosition);
        Vector2 aspectRatio = new Vector2(1f, Screen.height / Screen.width);
        RelitMaterial.SetVector(_aspectRatioPropertyName, aspectRatio);

        string DebugMessage = string.Empty;

        for (int i = 0; i < PointLights.Length; ++i)
        {
            PointLights[i].transform.position = FocusPoint.position + _lightOffset;

            var light = PointLights[i];
            Vector3 offset;

            // Assigns random movement to individual point lights.
            if (i == 0)
            {
                offset = new Vector3(
                    Mathf.Cos((Time.time * 1.2f) + 1.2f),
                    Mathf.Sin((Time.time * 1.6f) + 0.7f),
                    Mathf.Cos((Time.time * 1.5f) + 0.2f));
            }
            else
            if (i == 1)
            {
                offset = new Vector3(
                    Mathf.Cos((Time.time * 1.7f) + 1.0f),
                    Mathf.Sin(Time.time * 1.4f),
                    Mathf.Sin(Time.time * 1.2f));
            }
            else
            {
                offset = new Vector3(
                    Mathf.Cos((Time.time * 1.8f) + 0.5f),
                    Mathf.Sin((Time.time * 1.8f) + 0.2f),
                    Mathf.Cos(Time.time * 1.4f));
            }

            // Rescales the movement of the dynamic point lights to be smaller.
            offset *= 0.3f;

            var world = PointLights[i].transform.position + offset;
            var local = Camera.main.WorldToScreenPoint(world);
            local.x /= Screen.width;
            local.y /= Screen.height;

            _pointlightPositions[i] = new Vector4(local.x, local.y, local.z, 1);

            var color = light.GetComponent<Light>().color;
            _pointlightColors[i] = color;

            var intensity = light.GetComponent<Light>().intensity;
            _pointlightIntensities[i] = intensity;
        }

        RelitMaterial.SetVectorArray("_PointLightPositions", _pointlightPositions);
        RelitMaterial.SetFloatArray("_PointLightIntensities", _pointlightIntensities);
        RelitMaterial.SetVectorArray("_PointLightColors", _pointlightColors);
        RelitMaterial.SetFloat("_GlobalDarkness", _globalDarkness);
        RelitMaterial.SetFloat("_RenderMode", (int)_renderMode);

        // Updates the values related to DepthSource.
        if (!DepthSource.Initialized)
        {
            return;
        }
    }

    /// <summary>
    /// Sets the focus point's world position to be the world position of the touched vertex.
    /// </summary>
    private void ProcessTouch()
    {
        if (Input.touchCount < 1)
        {
            return;
        }

        Touch touch = Input.GetTouch(0);

        // Ignore the bottom of the screen where the slider is located.
        if (touch.position.y > 350)
        {
            FocusPoint.position = DepthSource.GetVertexInWorldSpaceFromScreenXY(
                                    (int)touch.position.x, (int)touch.position.y,
                                    DepthSource.DepthArray);
        }
    }
}
