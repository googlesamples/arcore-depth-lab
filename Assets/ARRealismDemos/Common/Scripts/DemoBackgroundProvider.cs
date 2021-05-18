//-----------------------------------------------------------------------
// <copyright file="DemoBackgroundProvider.cs" company="Google LLC">
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

using GoogleARCore;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Exposes the device's camera as a global shader texture with id equal to
/// BackgroundTextureProvider.BackgroundTexturePropertyName.
/// Can be placed anywhere in the scene, requires presense of
/// ARCoreBackgroundRenderer or DemoARBackgroundRenderer, whichever exists first.
/// </summary>
public class DemoBackgroundProvider : MonoBehaviour
{
    /// <summary>
    /// The global shader property name for the camera texture.
    /// </summary>
    public const string BackgroundTexturePropertyName = "_BackgroundTexture";

    private Camera _camera;
    private Material _material;
    private ARCoreBackgroundRenderer _backgroundRenderer;
    private DemoARBackgroundRenderer _demoRenderer;
    private CommandBuffer _commandBuffer;
    private bool _useDemoRenderer = false;
    private int _backgroundTextureID = -1;

    private void Start()
    {
        _camera = Camera.main;

        _backgroundRenderer = FindObjectOfType<ARCoreBackgroundRenderer>();
        if (_backgroundRenderer == null)
        {
            _useDemoRenderer = true;
            _demoRenderer = FindObjectOfType<DemoARBackgroundRenderer>();
            if (_demoRenderer == null)
            {
                Debug.LogError("DemoBackgroundProvider requires ARCoreBackgroundRenderer or" +
                               "DemoARBackgroundRenderer anywhere in the scene.");
                return;
            }

            Debug.Log("DemoARBackgroundRenderer loaded.");
        }
        else
        {
            Debug.Log("ARCoreTextureProvider loaded.");
        }

        _commandBuffer = new CommandBuffer();
        _commandBuffer.name = "Camera texture";
        _backgroundTextureID = Shader.PropertyToID(BackgroundTexturePropertyName);
        _commandBuffer.GetTemporaryRT(_backgroundTextureID, /*width=*/ -1, /*height=*/ -1,
                                       /*depthBuffer=*/ 0, FilterMode.Bilinear);

        // Alternatively, can blit from BuiltinRenderTextureType.CameraTarget into
        // _backgroundTextureID, but make sure this is executed after the renderer is initialized.
        var material = _useDemoRenderer ?
                         _demoRenderer.BackgroundMaterial :
                         _backgroundRenderer.BackgroundMaterial;
        if (material != null)
        {
            _commandBuffer.Blit(material.mainTexture, _backgroundTextureID, material);
            Debug.Log("BackgroundTextureProvider material blited.");
        }

        _commandBuffer.SetGlobalTexture(BackgroundTexturePropertyName, _backgroundTextureID);
        _camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
        _camera.AddCommandBuffer(CameraEvent.BeforeGBuffer, _commandBuffer);
    }

    private void OnEnable()
    {
        if (_camera != null && _commandBuffer != null)
        {
            _camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
            _camera.AddCommandBuffer(CameraEvent.BeforeGBuffer, _commandBuffer);
        }
    }

    private void OnDisable()
    {
        if (_camera != null && _commandBuffer != null)
        {
            _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
            _camera.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, _commandBuffer);
        }
    }
}
