//-----------------------------------------------------------------------
// <copyright file="AttachDepthTexture.cs" company="Google LLC">
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

/// <summary>
/// Attaches and updates the depth texture each frame.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class AttachDepthTexture : MonoBehaviour
{
    private static readonly string k_CurrentDepthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string k_TopLeftRightPropertyName = "_UvTopLeftRight";
    private static readonly string k_BottomLeftRightPropertyName = "_UvBottomLeftRight";
    private Texture2D m_DepthTexture;
    private Material m_Material;

    private void Start()
    {
        // Default texture, will be updated each frame.
        m_DepthTexture = new Texture2D(2, 2);
        m_DepthTexture.filterMode = FilterMode.Bilinear;

        // Assign the texture to the material.
        m_Material = GetComponent<Renderer>().sharedMaterial;
        m_Material.SetTexture(k_CurrentDepthTexturePropertyName, m_DepthTexture);
        UpdateScreenOrientationOnMaterial();
    }

    private void UpdateScreenOrientationOnMaterial()
    {
        var uvQuad = Frame.CameraImage.TextureDisplayUvs;
        m_Material.SetVector(
            k_TopLeftRightPropertyName,
            new Vector4(
                uvQuad.TopLeft.x, uvQuad.TopLeft.y, uvQuad.TopRight.x, uvQuad.TopRight.y));
        m_Material.SetVector(
            k_BottomLeftRightPropertyName,
            new Vector4(uvQuad.BottomLeft.x, uvQuad.BottomLeft.y, uvQuad.BottomRight.x,
                uvQuad.BottomRight.y));
    }

    private void Update()
    {
        Frame.CameraImage.UpdateDepthTexture(ref m_DepthTexture);

        UpdateScreenOrientationOnMaterial();
    }
}
