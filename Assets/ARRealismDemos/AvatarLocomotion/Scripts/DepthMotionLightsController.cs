//-----------------------------------------------------------------------
// <copyright file="DepthMotionLightsController.cs" company="Google LLC">
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
using UnityEngine.Serialization;

/// <summary>
/// Renders an animated virtual sun in the scene.
/// </summary>
public class DepthMotionLightsController : MonoBehaviour
{
    /// <summary>
    /// The relighting material with scree-space sunbeam shader.
    /// </summary>
    [FormerlySerializedAs("motionLightsMaterial")]
    public Material MotionLightsMaterial;

    /// <summary>
    /// The avatar controller to inform where the relighting effects is anchored.
    /// </summary>
    [FormerlySerializedAs("avatarSceneController")]
    public AvatarSceneController AvatarSceneController;

    private const float _fadeInTime = 1.0f;
    private static readonly string _lightAnchorPositionName = "_LightAnchorPosition";
    private static readonly string _globalAlphaValueName = "_GlobalAlphaValue";
    private static readonly string _normalizedDepthMinName = "_NormalizedDepthMin";
    private static readonly string _normalizedDepthMaxName = "_NormalizedDepthMax";
    private static readonly Vector3 _destinationOffset = new Vector3(0, 1.25f, -0.2f);
    private static readonly Vector3 _sourceOffset = new Vector3(0, 0.25f, -0.4f);

    private Transform _lightAnchor;
    private Vector3 _lightAnchorPosition = Vector3.zero;
    private Vector3 _sunTargetPosition = _destinationOffset;
    private Vector3 _sunPosition = _sourceOffset;
    private bool _initialized = false;
    private float _globalAlphaValue = 0.0f;
    private float _startTime = 0.0f;

    /// <summary>
    /// Enables the MotionLights effect if and only if the avatar is placed.
    /// </summary>
    /// <param name="enableLights">Enable Lights.</param>
    public void EnableLights(bool enableLights)
    {
        Debug.Log("Lights enabled.");
        _initialized = false;
        if (AvatarSceneController == null)
        {
            Debug.LogError("Avatar scene controller is not set.");
            return;
        }

        if (AvatarSceneController.Avatar == null)
        {
            Debug.LogError("Avatar is not set.");
            return;
        }

        SetAnchor(AvatarSceneController.Avatar.transform);
        _sunPosition = _lightAnchor.transform.TransformPoint(_sourceOffset);
        _sunTargetPosition = _lightAnchor.transform.TransformPoint(_destinationOffset);

        enabled = enableLights;

        if (enabled)
        {
            _startTime = Time.time + _fadeInTime;
        }
    }

    /// <summary>
    /// Sets where the light source rises.
    /// </summary>
    /// <param name="anchor">Transform anchor.</param>
    public void SetAnchor(Transform anchor)
    {
        Debug.Log("Lights anchor set.");
        _lightAnchor = anchor;
    }

    private void Update()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;

        if (AvatarSceneController == null ||
            AvatarSceneController.Avatar == null)
        {
            return;
        }

        SetAnchor(AvatarSceneController.Avatar.transform);

        // Smoothly moves the light anchor from the bottom of the avatar to the top of the avatar.
        if (_lightAnchor != null && enabled)
        {
            _globalAlphaValue = Mathf.Clamp01(Time.time - _startTime);
            if (Time.time - _startTime > 1.0)
            {
                float lerpTime = Time.deltaTime * 0.2f;
                _sunPosition = Vector3.Lerp(_sunPosition, _sunTargetPosition, lerpTime);
            }

            _lightAnchorPosition = Camera.main.WorldToScreenPoint(_sunPosition);
            float depthMin = MotionLightsMaterial.GetFloat(_normalizedDepthMinName);
            float depthMax = MotionLightsMaterial.GetFloat(_normalizedDepthMaxName);
            float depthRange = depthMax - depthMin;
            float zLightAnchorPosition = (_lightAnchorPosition.z - depthMin) / depthRange;
            _lightAnchorPosition.z = Mathf.Clamp01(zLightAnchorPosition);
            MotionLightsMaterial.SetVector(
                _lightAnchorPositionName, _lightAnchorPosition);
            MotionLightsMaterial.SetFloat(
                _globalAlphaValueName, _globalAlphaValue);
            if (!_initialized)
            {
                Debug.Log("Lights initialized in update.");
            }

            _initialized = true;
        }
    }

    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
    {
        if (MotionLightsMaterial != null && _initialized)
        {
            Debug.Log("Blit works.");
            Graphics.Blit(sourceTexture, destTexture, MotionLightsMaterial);
        }
        else
        {
            Graphics.Blit(sourceTexture, destTexture);
        }
    }
}
