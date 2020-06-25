//-----------------------------------------------------------------------
// <copyright file="BackgroundTextureProvider.cs" company="Google LLC">
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

    private Camera m_Camera;
    private ARCoreBackgroundRenderer m_BackgroundRenderer;
    private CommandBuffer m_CommandBuffer;
    private int m_BackgroundTextureID = -1;

    private void Awake()
    {
        m_Camera = Camera.main;
        Debug.Assert(m_Camera != null,
                     "The scene must include a camera object to get the background texture.");

        m_BackgroundRenderer = FindObjectOfType<ARCoreBackgroundRenderer>();
        if (m_BackgroundRenderer == null)
        {
            Debug.LogError("BackgroundTextureProvider requires ARCoreBackgroundRenderer " +
                            "anywhere in the scene.");
            return;
        }

        m_CommandBuffer = new CommandBuffer();
        m_CommandBuffer.name = "Camera texture";
        m_BackgroundTextureID = Shader.PropertyToID(BackgroundTexturePropertyName);
        m_CommandBuffer.GetTemporaryRT(m_BackgroundTextureID, /*width=*/ -1, /*height=*/ -1,
                                       /*depthBuffer=*/ 0, FilterMode.Bilinear);

        // Alternatively, can blit from BuiltinRenderTextureType.CameraTarget into
        // m_BackgroundTextureID, but make sure this is executed after the renderer is initialized.
        var material = m_BackgroundRenderer.BackgroundMaterial;
        if (material != null)
        {
            m_CommandBuffer.Blit(material.mainTexture, m_BackgroundTextureID, material);
        }

        m_CommandBuffer.SetGlobalTexture(BackgroundTexturePropertyName, m_BackgroundTextureID);
        m_Camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, m_CommandBuffer);
        m_Camera.AddCommandBuffer(CameraEvent.AfterGBuffer, m_CommandBuffer);
    }

    private void OnEnable()
    {
        m_Camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, m_CommandBuffer);
        m_Camera.AddCommandBuffer(CameraEvent.AfterGBuffer, m_CommandBuffer);
    }

    private void OnDisable()
    {
        m_Camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, m_CommandBuffer);
        m_Camera.RemoveCommandBuffer(CameraEvent.AfterGBuffer, m_CommandBuffer);
    }
}
