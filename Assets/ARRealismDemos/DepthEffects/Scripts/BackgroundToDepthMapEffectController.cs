//-----------------------------------------------------------------------
// <copyright file="BackgroundToDepthMapEffectController.cs" company="Google LLC">
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
using UnityEngine;
using UnityEngine.XR.ARFoundation;

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
    /// Percentage of the transition between the camera view and the depth map visualization.
    /// </summary>
    public float Transition = 0;

    /// <summary>
    /// Whether or not to blend in the luminance of the camera view.
    /// </summary>
    public float CameraViewOpacity = 1;

    /// <summary>
    /// Rendering the depth map, camera view, or the transition between them.
    /// </summary>
    public Material BackgroundToDepthMapMaterial;

    private const float _kTransitionDurationS = 4;
    private const float _kMaxVisualizationDistanceM = 7;
    private const float _kMinVisualizationDistanceM = 0.4f;
    private const float _kFarFadePortion = 0.15f;
    private const float _kHalfTransitionHighlightWidth = 0.15f;

    private static readonly string
    _transitionPropertyName = "_Transition";

    private static readonly string
    _cameraViewOpacityPropertyName = "_CameraViewOpacity";

    private static readonly string
    _maxVisualizationDistancePropertyName = "_MaxVisualizationDistance";

    private static readonly string
    _minVisualizationDistancePropertyName = "_MinVisualizationDistance";

    private static readonly string
    _farFadePortionPropertyName = "_FarFadePortion";

    private static readonly string
    _halfTransitionHighlightWidtPropertyName = "_HalfTransitionHighlightWidth";

    private Coroutine _currentCoroutine;

    private bool _enableOcclusionTransition = false;
    private bool _occlusionOn = false;
    private float _transitionDurationS = _kTransitionDurationS;
    private float _maxVisualizationDistanceM = _kMaxVisualizationDistanceM;
    private float _minVisualizationDistanceM = _kMinVisualizationDistanceM;
    private float _farFadePortion = _kFarFadePortion;
    private float _halfTransitionHighlightWidth = _kHalfTransitionHighlightWidth;
    private float _applyAntiAliasing = 0;

    private Material _shadowReceiverMaterial;

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
        _enableOcclusionTransition = false;
        Transition = 1;

        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(AnimateTransition(0, _transitionDurationS));
    }

    /// <summary>
    /// Animates from camera view to visualization of the depth map.
    /// </summary>
    public void StartTransitionToDepth()
    {
        _enableOcclusionTransition = false;
        _applyAntiAliasing = 1f - _applyAntiAliasing;
        CameraViewOpacity = 0;
        Transition = 0;

        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(
            AnimateTransition(1, _transitionDurationS));
    }

    /// <summary>
    /// Toggles between with and without the occlusion effects.
    /// </summary>
    public void ToggleOcclusionEffect()
    {
        if (!_occlusionOn)
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
        _occlusionOn = false;
        CameraViewOpacity = 1;
        _enableOcclusionTransition = true;
        Transition = 1;

        if (ShadowReceiver != null)
        {
            ShadowReceiver.MaximumMeshDistance = _maxVisualizationDistanceM;
        }

        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(AnimateTransition(0, _transitionDurationS));
    }

    /// <summary>
    /// Enables occlusion AR effects.
    /// </summary>
    public void StartOcclusionEffect()
    {
        _occlusionOn = true;
        CameraViewOpacity = 1;
        _enableOcclusionTransition = true;
        Transition = 0;

        if (ShadowReceiver != null)
        {
            ShadowReceiver.MaximumMeshDistance = 0;
        }

        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(
            AnimateTransition(1, _transitionDurationS));
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
        // Use smooth depth for depth effects.
        DepthSource.SwitchToRawDepth(false);
        if (ShadowReceiver != null)
        {
            ShadowReceiver.MaximumMeshDistance = 0;
            _shadowReceiverMaterial = ShadowReceiver.GetComponent<MeshRenderer>().material;
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
        BackgroundToDepthMapMaterial.SetFloat(_transitionPropertyName, Transition);
        BackgroundToDepthMapMaterial.SetFloat(_cameraViewOpacityPropertyName, CameraViewOpacity);
        BackgroundToDepthMapMaterial.SetFloat(_maxVisualizationDistancePropertyName,
          _maxVisualizationDistanceM);
        BackgroundToDepthMapMaterial.SetFloat(_minVisualizationDistancePropertyName,
          _minVisualizationDistanceM);
        BackgroundToDepthMapMaterial.SetFloat(_farFadePortionPropertyName, _farFadePortion);
        BackgroundToDepthMapMaterial.SetFloat(_halfTransitionHighlightWidtPropertyName,
          _halfTransitionHighlightWidth);
    }

    /// <summary>
    /// Animates the transition effects.
    /// </summary>
    /// <param name="targetValue">The target transition percentage.</param>
    /// <param name="animationTime">The animation duration in seconds.</param>
    /// <returns>Enumerator of the animator.</returns>
    private IEnumerator AnimateTransition(float targetValue, float animationTime)
    {
        if (_enableOcclusionTransition && OcclusionChangedEvent != null)
        {
            if (_shadowReceiverMaterial != null)
            {
                _shadowReceiverMaterial.SetInt("_ZWrite", 1);
            }

            if (!_occlusionOn)
            {
                // Notifies event handler before the occlusion is turned off.
                OcclusionChangedEvent(_occlusionOn);
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
            if (_enableOcclusionTransition && ShadowReceiver != null)
            {
                ShadowReceiver.MaximumMeshDistance = Transition * _maxVisualizationDistanceM;
            }

            yield return null;
        }

        Transition = targetValue;

        if (_enableOcclusionTransition && OcclusionChangedEvent != null)
        {
            if (_shadowReceiverMaterial != null)
            {
                _shadowReceiverMaterial.SetInt("_ZWrite", 0);
            }

            if (_occlusionOn)
            {
                // Notifies event handler before the occlusion is turned on.
                OcclusionChangedEvent(_occlusionOn);
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
