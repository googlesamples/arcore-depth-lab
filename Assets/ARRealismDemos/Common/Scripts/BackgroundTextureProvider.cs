//-----------------------------------------------------------------------
// <copyright file="BackgroundTextureProvider.cs" company="Google LLC">
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
/// Can be placed anywhere in the scene, requires presense of ARCoreBackgroundRenderer.
/// </summary>
public class BackgroundTextureProvider : MonoBehaviour
{
    /// <summary>
    /// The global shader property name for the camera texture.
    /// </summary>
    public const string BackgroundTexturePropertyName = "_BackgroundTexture";

    private Camera _camera;
    private ARCoreBackgroundRenderer _backgroundRenderer;
    private CommandBuffer _commandBuffer;
    private int _backgroundTextureID = -1;

    private void Awake()
    {
        _camera = Camera.main;
        Debug.Assert(_camera != null,
                     "The scene must include a camera object to get the background texture.");

        _backgroundRenderer = FindObjectOfType<ARCoreBackgroundRenderer>();
        if (_backgroundRenderer == null)
        {
            Debug.LogError("BackgroundTextureProvider requires ARCoreBackgroundRenderer " +
                            "anywhere in the scene.");
            return;
        }

        _commandBuffer = new CommandBuffer();
        _commandBuffer.name = "Camera texture";
        _backgroundTextureID = Shader.PropertyToID(BackgroundTexturePropertyName);
        _commandBuffer.GetTemporaryRT(_backgroundTextureID, /*width=*/ -1, /*height=*/ -1,
                                       /*depthBuffer=*/ 0, FilterMode.Bilinear);

        // Alternatively, can blit from BuiltinRenderTextureType.CameraTarget into
        // _backgroundTextureID, but make sure this is executed after the renderer is initialized.
        var material = _backgroundRenderer.BackgroundMaterial;
        if (material != null)
        {
            _commandBuffer.Blit(material.mainTexture, _backgroundTextureID, material);
        }

        _commandBuffer.SetGlobalTexture(BackgroundTexturePropertyName, _backgroundTextureID);
        _camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
        _camera.AddCommandBuffer(CameraEvent.AfterGBuffer, _commandBuffer);
    }

    private void OnEnable()
    {
        _camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
        _camera.AddCommandBuffer(CameraEvent.AfterGBuffer, _commandBuffer);
    }

    private void OnDisable()
    {
        _camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
        _camera.RemoveCommandBuffer(CameraEvent.AfterGBuffer, _commandBuffer);
    }
}
