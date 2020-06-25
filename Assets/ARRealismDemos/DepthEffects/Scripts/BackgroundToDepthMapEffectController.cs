//-----------------------------------------------------------------------
// <copyright file="BackgroundToDepthMapEffectController.cs" company="Google LLC">
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

/// <summary>
/// Triggers when the user presses the "occlusion change" button.
/// </summary>
/// <param name="occlusionOn">Is occlusion on.</param>
public delegate void OcclusionChanged(bool occlusionOn);

/// <summary>
/// Manages the visualization of the depth map, camera view, and the animation in between.
/// </summary>
public class BackgroundToDepthMapEffectController : MonoBehaviour
{
    /// <summary>
    /// Mesh with shadows empowered by the depth map.
    /// </summary>
    public ShadowReceiverMesh ShadowReceiver;

    /// <summary>
    /// Percentage of the transtion between the camera view and the depth map visualization.
    /// </summary>
    public float Transition = 0;

    /// <summary>
    /// Whether or not to blend in the luminance of the camera view.
    /// </summary>
    public float CameraViewOpacity = 1;

    /// <summary>
    /// Rendering the depth map, camera view, or the transition betweem them.
    /// </summary>
    public Material BackgroundToDepthMapMaterial;

    private const float k_TransitionDurationS = 4;
    private const float k_MaxVisualizationDistanceM = 7;
    private const float k_MinVisualizationDistanceM = 0.4f;
    private const float k_FarFadePortion = 0.15f;
    private const float k_HalfTransitionHighlightWidth = 0.15f;

    private static readonly string
    k_TransitionPropertyName = "_Transition";

    private static readonly string
    k_CameraViewOpacityPropertyName = "_CameraViewOpacity";

    private static readonly string
    k_MaxVisualizationDistancePropertyName = "_MaxVisualizationDistance";

    private static readonly string
    k_MinVisualizationDistancePropertyName = "_MinVisualizationDistance";

    private static readonly string
    k_FarFadePortionPropertyName = "_FarFadePortion";

    private static readonly string
    k_ApplyAntiAliasingPropertyName = "_ApplyAntiAliasing";

    private static readonly string
    k_HalfTransitionHighlightWidtPropertyName = "_HalfTransitionHighlightWidth";

    private Coroutine m_CurrentCoroutine;

    private bool m_EnableOcclusionTransition = false;
    private bool m_OcclusionOn = false;
    private float m_TransitionDurationS = k_TransitionDurationS;
    private float m_MaxVisualizationDistanceM = k_MaxVisualizationDistanceM;
    private float m_MinVisualizationDistanceM = k_MinVisualizationDistanceM;
    private float m_FarFadePortion = k_FarFadePortion;
    private float m_HalfTransitionHighlightWidth = k_HalfTransitionHighlightWidth;
    private float m_ApplyAntiAliasing = 0;

    private Material m_ShadowReceiverMaterial;

    /// <summary>
    /// Event triggered when the user touched the "Change Occlusion" button.
    /// </summary>
    public event OcclusionChanged OcclusionChangedEvent;

    /// <summary>
    /// Animates from depth map visualization to the camera view.
    /// </summary>
    public void StartTransitionToCamera()
    {
        CameraViewOpacity = 0;
        m_EnableOcclusionTransition = false;
        Transition = 1;

        if (m_CurrentCoroutine != null)
        {
            StopCoroutine(m_CurrentCoroutine);
        }

        m_CurrentCoroutine = StartCoroutine(AnimateTransition(0, m_TransitionDurationS));
    }

    /// <summary>
    /// Animates from camera view to visualization of the depth map.
    /// </summary>
    public void StartTransitionToDepth()
    {
        m_EnableOcclusionTransition = false;
        m_ApplyAntiAliasing = 1f - m_ApplyAntiAliasing;
        CameraViewOpacity = 0;
        Transition = 0;

        if (m_CurrentCoroutine != null)
        {
            StopCoroutine(m_CurrentCoroutine);
        }

        m_CurrentCoroutine = StartCoroutine(
            AnimateTransition(1, m_TransitionDurationS));
    }

    /// <summary>
    /// Toggles between with and without the occlusion effects.
    /// </summary>
    public void ToggleOcclusionEffect()
    {
        if (!m_OcclusionOn)
        {
            StartOcclusionEffect();
        }
        else
        {
            StartNoOcclusionEffect();
        }
    }

    /// <summary>
    /// Disables occlusion AR effects.
    /// </summary>
    public void StartNoOcclusionEffect()
    {
        m_OcclusionOn = false;
        CameraViewOpacity = 1;
        m_EnableOcclusionTransition = true;
        Transition = 1;

        if (ShadowReceiver != null)
        {
            ShadowReceiver.MaximumMeshDistance = m_MaxVisualizationDistanceM;
        }

        if (m_CurrentCoroutine != null)
        {
            StopCoroutine(m_CurrentCoroutine);
        }

        m_CurrentCoroutine = StartCoroutine(AnimateTransition(0, m_TransitionDurationS));
    }

    /// <summary>
    /// Enables occlusion AR effects.
    /// </summary>
    public void StartOcclusionEffect()
    {
        m_OcclusionOn = true;
        CameraViewOpacity = 1;
        m_EnableOcclusionTransition = true;
        Transition = 0;

        if (ShadowReceiver != null)
        {
            ShadowReceiver.MaximumMeshDistance = 0;
        }

        if (m_CurrentCoroutine != null)
        {
            StopCoroutine(m_CurrentCoroutine);
        }

        m_CurrentCoroutine = StartCoroutine(
            AnimateTransition(1, m_TransitionDurationS));
    }

    /// <summary>
    /// Toggles the animated visualization between the depth map and the camera image.
    /// </summary>
    public void ToggleTransition()
    {
        if (Transition < 0.5f)
        {
            StartTransitionToDepth();
        }
        else
        {
            StartTransitionToCamera();
        }
    }

    /// <summary>
    /// Triggers when the script initializes.
    /// </summary>
    private void Start()
    {
        if (ShadowReceiver != null)
        {
            ShadowReceiver.MaximumMeshDistance = 0;
            m_ShadowReceiverMaterial = ShadowReceiver.GetComponent<MeshRenderer>().material;
        }
    }

    /// <summary>
    /// Triggers every frame.
    /// </summary>
    private void Update()
    {
        UpdateShaderVariables();
    }

    /// <summary>
    /// Update the depth map visualization material.
    /// </summary>
    private void UpdateShaderVariables()
    {
        BackgroundToDepthMapMaterial.SetFloat(k_TransitionPropertyName, Transition);
        BackgroundToDepthMapMaterial.SetFloat(k_CameraViewOpacityPropertyName, CameraViewOpacity);
        BackgroundToDepthMapMaterial.SetFloat(k_MaxVisualizationDistancePropertyName,
          m_MaxVisualizationDistanceM);
        BackgroundToDepthMapMaterial.SetFloat(k_MinVisualizationDistancePropertyName,
          m_MinVisualizationDistanceM);
        BackgroundToDepthMapMaterial.SetFloat(k_FarFadePortionPropertyName, m_FarFadePortion);
        BackgroundToDepthMapMaterial.SetFloat(k_HalfTransitionHighlightWidtPropertyName,
          m_HalfTransitionHighlightWidth);
        BackgroundToDepthMapMaterial.SetFloat(k_ApplyAntiAliasingPropertyName, m_ApplyAntiAliasing);
    }

    /// <summary>
    /// Animates the transition effects.
    /// </summary>
    /// <param name="targetValue">The target transition percentage.</param>
    /// <param name="animationTime">The animation duration in seconds.</param>
    /// <returns>Enumerator of the animator.</returns>
    private IEnumerator AnimateTransition(float targetValue, float animationTime)
    {
        if (m_EnableOcclusionTransition && OcclusionChangedEvent != null)
        {
            if (m_ShadowReceiverMaterial != null)
            {
                m_ShadowReceiverMaterial.SetInt("_ZWrite", 1);
            }

            if (!m_OcclusionOn)
            {
                // Notifies event handler before the occlusion is turned off.
                OcclusionChangedEvent(m_OcclusionOn);
            }
            else
            {
                if (ShadowReceiver != null)
                {
                    // Activates the shadow receiver before it is turned on.
                    ShadowReceiver.GetComponent<MeshRenderer>().enabled = true;
                }
            }
        }

        float originalTransition = Transition;
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / animationTime)
        {
            Transition = Mathf.Lerp(originalTransition, targetValue, t);
            if (m_EnableOcclusionTransition && ShadowReceiver != null)
            {
                ShadowReceiver.MaximumMeshDistance = Transition * m_MaxVisualizationDistanceM;
            }

            yield return null;
        }

        Transition = targetValue;

        if (m_EnableOcclusionTransition && OcclusionChangedEvent != null)
        {
            if (m_ShadowReceiverMaterial != null)
            {
                m_ShadowReceiverMaterial.SetInt("_ZWrite", 0);
            }

            if (m_OcclusionOn)
            {
                // Notifies event handler before the occlusion is turned on.
                OcclusionChangedEvent(m_OcclusionOn);
            }
            else
            {
                if (ShadowReceiver != null)
                {
                    // Dectivates the shadow receiver after it is turned off.
                    ShadowReceiver.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }
    }
}
