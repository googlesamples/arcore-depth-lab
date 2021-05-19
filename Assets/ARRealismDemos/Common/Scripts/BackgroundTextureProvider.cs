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

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Exposes the device's camera as a global shader texture with id equal to
/// BackgroundTextureProvider.BackgroundTexturePropertyName.
/// Can be placed anywhere in the scene, requires presence of ARCameraBackground.
/// </summary>
public class BackgroundTextureProvider : MonoBehaviour
{
    /// <summary>
    /// The global shader property name for the camera texture.
    /// </summary>
    public const string BackgroundTexturePropertyName = "_BackgroundTexture";

    public Material BackgroundMaterial;

    private Camera _arCamera;
    private ARCameraBackground _backgroundRenderer;
    private CommandBuffer _commandBuffer;
    private int _backgroundTextureID = -1;

    private static readonly int _mainTex = Shader.PropertyToID("_MainTex");

    private void Start()
    {
        _arCamera = DepthSource.ARCamera;

        Debug.Assert(_arCamera != null,
            "The scene must include a camera object to get the background texture.");
        Debug.Assert(BackgroundMaterial);

        _backgroundRenderer = _arCamera.GetComponent<ARCameraBackground>();
        if (_backgroundRenderer == null)
        {
            Debug.LogError(
                "BackgroundTextureProvider requires ARCameraBackground " +
                "anywhere in the scene.");
            return;
        }

        _backgroundRenderer.enabled = false;
        _backgroundRenderer.enabled = true;
        _commandBuffer = new CommandBuffer();
        _commandBuffer.name = "Camera texture";
        _backgroundTextureID = Shader.PropertyToID(BackgroundTexturePropertyName);
        _commandBuffer.GetTemporaryRT(_backgroundTextureID, /*width=*/ -1, /*height=*/ -1,
            /*depthBuffer=*/ 0, FilterMode.Bilinear);

        // Alternatively, can blit from BuiltinRenderTextureType.CameraTarget into
        // _backgroundTextureID, but make sure this is executed after the renderer is initialized.
        _commandBuffer.Blit(
            _backgroundRenderer.material.GetTexture(_mainTex), _backgroundTextureID,
            BackgroundMaterial);

        _commandBuffer.SetGlobalTexture(BackgroundTexturePropertyName, _backgroundTextureID);
        _arCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
    }

    private void OnEnable()
    {
        if (_arCamera != null && _commandBuffer != null)
        {
            _arCamera.AddCommandBuffer(
                CameraEvent.BeforeForwardOpaque, _commandBuffer);
        }
    }

    private void OnDisable()
    {
        if (_arCamera != null && _commandBuffer != null)
        {
            _arCamera.RemoveCommandBuffer(
                CameraEvent.BeforeForwardOpaque, _commandBuffer);
        }
    }
}