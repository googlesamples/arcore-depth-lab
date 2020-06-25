//-----------------------------------------------------------------------
// <copyright file="AttachDepthTextureToMaterial.cs" company="Google LLC">
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

using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Attaches and updates the depth texture each frame.
/// </summary>
public class AttachDepthTextureToMaterial : MonoBehaviour
{
    /// <summary>
    /// Lists of all materials which use the depth map as an input uniform (texture).
    /// </summary>
    [FormerlySerializedAs("materials")]
    public List<Material> Materials;

    private static readonly string k_CurrentDepthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string k_TopLeftRightPropertyName = "_UvTopLeftRight";
    private static readonly string k_BottomLeftRightPropertyName = "_UvBottomLeftRight";

    private Texture2D m_DepthTexture;

    /// <summary>
    /// Initializes the depth texture and filtering mode.
    /// </summary>
    private void Start()
    {
        // Default texture, will be updated each frame.
        m_DepthTexture = new Texture2D(2, 2);
        m_DepthTexture.filterMode = FilterMode.Bilinear;

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
        var uvQuad = Frame.CameraImage.TextureDisplayUvs;
        material.SetVector(
            k_TopLeftRightPropertyName,
            new Vector4(
                uvQuad.TopLeft.x, uvQuad.TopLeft.y, uvQuad.TopRight.x, uvQuad.TopRight.y));
        material.SetVector(
            k_BottomLeftRightPropertyName,
            new Vector4(uvQuad.BottomLeft.x, uvQuad.BottomLeft.y, uvQuad.BottomRight.x,
                uvQuad.BottomRight.y));
    }

    /// <summary>
    /// Retrieves the latest depth map from ARCore and assigns it to all materials.
    /// </summary>
    private void Update()
    {
        // Gets the latest depth map from ARCore.
        Frame.CameraImage.UpdateDepthTexture(ref m_DepthTexture);

        // Updates the screen orientation for each material.
        foreach (Material currentMaterial in Materials)
        {
            UpdateScreenOrientationOnMaterial(currentMaterial);
        }
    }
}
