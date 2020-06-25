//-----------------------------------------------------------------------
// <copyright file="AttachFakeDepthTextureToMaterial.cs" company="Google LLC">
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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simulates the AttachDepthTextureToMaterial, but updates the texture with fake data, consisting
/// on a fixed distance plane in front of the camera.
/// </summary>
public class AttachFakeDepthTextureToMaterial : MonoBehaviour
{
    /// <summary>
    /// Lists of all materials which use the depth map as an input uniform (texture).
    /// </summary>
    public List<Material> Materials;

    /// <summary>
    /// The static depth in mm, that the depth image is filled with.
    /// </summary>
    public short Depth = 2000;

    private static readonly string k_CurrentDepthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string k_TopLeftRightPropertyName = "_UvTopLeftRight";
    private static readonly string k_BottomLeftRightPropertyName = "_UvBottomLeftRight";

    private Texture2D m_DepthTexture;

    /// <summary>
    /// Initializes the depth texture and filtering mode.
    /// </summary>
    private void Start()
    {
        m_DepthTexture = new Texture2D(2, 2, TextureFormat.RGB565, false);
        m_DepthTexture.filterMode = FilterMode.Bilinear;

        byte[] depthValueInBytes = BitConverter.GetBytes(Depth);
        byte[] depth_bytes = new byte[m_DepthTexture.width * m_DepthTexture.height * 2];
        for (int i = 0; i < m_DepthTexture.width * m_DepthTexture.height; ++i)
        {
            depth_bytes[i * 2] = depthValueInBytes[0];
            depth_bytes[(i * 2) + 1] = depthValueInBytes[1];
        }

        m_DepthTexture.LoadRawTextureData(depth_bytes);
        m_DepthTexture.Apply();

        // Assigns the texture to the material.
        foreach (Material currentMaterial in Materials)
        {
            currentMaterial.SetTexture(k_CurrentDepthTexturePropertyName, m_DepthTexture);
            UpdateScreenOrientationOnMaterial(currentMaterial);
        }
    }

    /// <summary>
    /// Updates the screen orientation of the depth map.
    /// </summary>
    /// <param name="material">The material to be set.</param>
    private void UpdateScreenOrientationOnMaterial(Material material)
    {
        //// Top left     x: 1 y: 1.0
        //// Top right    x: 1 y: 0.0
        //// Bottom Left  x: 0 y: 1.0
        //// Bottom Right x: 0 y: 0.0
        material.SetVector(
        k_TopLeftRightPropertyName, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        material.SetVector(
        k_BottomLeftRightPropertyName, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
    }

    /// <summary>
    /// Retrieves the latest depth map from ARCore and assigns it to all materials.
    /// </summary>
    private void Update()
    {
        // Updates the screen orientation for each material.
        foreach (Material currentMaterial in Materials)
        {
            UpdateScreenOrientationOnMaterial(currentMaterial);
        }
    }
}
