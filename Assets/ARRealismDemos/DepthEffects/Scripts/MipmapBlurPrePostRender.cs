//-----------------------------------------------------------------------
// <copyright file="MipmapBlurPrePostRender.cs" company="Google LLC">
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
using UnityEngine;

/// <summary>
/// Pre and post rendering event for efficient multipass on the GPU.
/// Note that this avoids glReadPixels incurred by OnImageRender() or grabpass() in the shader.
/// </summary>
public class MipmapBlurPrePostRender : MonoBehaviour
{
    /// <summary>
    /// The MipmapBlur material to use.
    /// </summary>
    public Material BlurMaterial;

    private const int k_AntiAliasing = 1;
    private const int k_DepthBits = 24;
    private RenderTexture m_RenderTexture;

    private void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        Camera.main.allowHDR = false;
        QualitySettings.antiAliasing = k_AntiAliasing;
        m_RenderTexture = new RenderTexture(Screen.width, Screen.height, k_DepthBits,
                              RenderTextureFormat.Default, RenderTextureReadWrite.Default);
        m_RenderTexture.useMipMap = true;
        m_RenderTexture.autoGenerateMips = true;
    }

    private void OnPreRender()
    {
        Camera.main.targetTexture = m_RenderTexture;
    }

    private void OnPostRender()
    {
        Camera.main.targetTexture = null;
        Graphics.Blit(m_RenderTexture, null, BlurMaterial);
    }
}
