//-----------------------------------------------------------------------
// <copyright file="LightEstimation.cs" company="Google LLC">
//
// Copyright 2021 Google LLC
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

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Applys light estimation result to the scene.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightEstimation : MonoBehaviour
{
    private Light _light;
    private ARCameraManager _cameraManager;

    // Start is called before the first frame update
    private void Start()
    {
        _light = GetComponent<Light>();
        _cameraManager = FindObjectOfType<ARCameraManager>();

        if (_cameraManager != null)
        {
            _cameraManager.frameReceived += OnFrameReceived;
        }
    }

    private void OnDestroy()
    {
        if (_cameraManager != null)
        {
            _cameraManager.frameReceived -= OnFrameReceived;
        }
    }

    private void OnFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // Light intensity:
        if (eventArgs.lightEstimation.mainLightIntensityLumens.HasValue)
        {
            _light.intensity = eventArgs.lightEstimation.mainLightIntensityLumens.Value;
        }
        else if (eventArgs.lightEstimation.averageBrightness.HasValue)
        {
            _light.intensity = eventArgs.lightEstimation.averageBrightness.Value;
        }

        // Light color:
        if (eventArgs.lightEstimation.mainLightColor.HasValue)
        {
            _light.color = eventArgs.lightEstimation.mainLightColor.Value;
        }
        else if (eventArgs.lightEstimation.colorCorrection.HasValue)
        {
            _light.color = eventArgs.lightEstimation.colorCorrection.Value;
        }

        // Color Temperature:
        if (eventArgs.lightEstimation.averageColorTemperature.HasValue)
        {
            _light.colorTemperature = eventArgs.lightEstimation.averageColorTemperature.Value;
        }

        // Light direction:
        if (eventArgs.lightEstimation.mainLightDirection.HasValue)
        {
            _light.transform.rotation = Quaternion.LookRotation(
                eventArgs.lightEstimation.mainLightDirection.Value);
        }

        // Ambinent Probe:
        if (eventArgs.lightEstimation.ambientSphericalHarmonics.HasValue)
        {
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientProbe = eventArgs.lightEstimation.ambientSphericalHarmonics.Value;
        }
    }
}
