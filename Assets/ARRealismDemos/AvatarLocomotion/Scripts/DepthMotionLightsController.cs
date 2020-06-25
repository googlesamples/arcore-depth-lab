//-----------------------------------------------------------------------
// <copyright file="DepthMotionLightsController.cs" company="Google LLC">
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

    private const float k_FadeInTime = 1.0f;
    private static readonly string k_LightAnchorPositionName = "_LightAnchorPosition";
    private static readonly string k_GlobalAlphaValueName = "_GlobalAlphaValue";
    private static readonly string k_NormalizedDepthMinName = "_NormalizedDepthMin";
    private static readonly string k_NormalizedDepthMaxName = "_NormalizedDepthMax";
    private static readonly Vector3 k_DestinationOffset = new Vector3(0, 1.25f, -0.2f);
    private static readonly Vector3 k_SourceOffset = new Vector3(0, 0.25f, -0.4f);

    private Transform m_LightAnchor;
    private Vector3 m_LightAnchorPosition = Vector3.zero;
    private Vector3 m_SunTargetPosition = k_DestinationOffset;
    private Vector3 m_SunPosition = k_SourceOffset;
    private bool m_Initialized = false;
    private float m_GlobalAlphaValue = 0.0f;
    private float m_StartTime = 0.0f;

    /// <summary>
    /// Enables the MotionLights effect if and only if the avatar is placed.
    /// </summary>
    /// <param name="enableLights">Enable Lights.</param>
    public void EnableLights(bool enableLights)
    {
        Debug.Log("Lights enabled.");
        m_Initialized = false;
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
        m_SunPosition = m_LightAnchor.transform.TransformPoint(k_SourceOffset);
        m_SunTargetPosition = m_LightAnchor.transform.TransformPoint(k_DestinationOffset);

        enabled = enableLights;

        if (enabled)
        {
            m_StartTime = Time.time + k_FadeInTime;
        }
    }

    /// <summary>
    /// Sets where the light source rises.
    /// </summary>
    /// <param name="anchor">Transform anchor.</param>
    public void SetAnchor(Transform anchor)
    {
        Debug.Log("Lights anchor set.");
        m_LightAnchor = anchor;
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
        if (m_LightAnchor != null && enabled)
        {
            m_GlobalAlphaValue = Mathf.Clamp01(Time.time - m_StartTime);
            if (Time.time - m_StartTime > 1.0)
            {
                float lerpTime = Time.deltaTime * 0.2f;
                m_SunPosition = Vector3.Lerp(m_SunPosition, m_SunTargetPosition, lerpTime);
            }

            m_LightAnchorPosition = Camera.main.WorldToScreenPoint(m_SunPosition);
            float depthMin = MotionLightsMaterial.GetFloat(k_NormalizedDepthMinName);
            float depthMax = MotionLightsMaterial.GetFloat(k_NormalizedDepthMaxName);
            float depthRange = depthMax - depthMin;
            float zLightAnchorPosition = (m_LightAnchorPosition.z - depthMin) / depthRange;
            m_LightAnchorPosition.z = Mathf.Clamp01(zLightAnchorPosition);
            MotionLightsMaterial.SetVector(
                k_LightAnchorPositionName, m_LightAnchorPosition);
            MotionLightsMaterial.SetFloat(
                k_GlobalAlphaValueName, m_GlobalAlphaValue);
            if (!m_Initialized)
            {
                Debug.Log("Lights initialized in update.");
            }

            m_Initialized = true;
        }
    }

    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
    {
        if (MotionLightsMaterial != null && m_Initialized)
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
