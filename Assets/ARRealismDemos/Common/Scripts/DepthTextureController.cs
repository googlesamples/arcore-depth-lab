//-----------------------------------------------------------------------
// <copyright file="DepthTextureController.cs" company="Google LLC">
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


using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Attaches and updates the depth texture each frame.
/// </summary>
public class DepthTextureController : MonoBehaviour
{
    /// <summary>
    /// Lists of all materials which use the depth map as an input uniform (texture).
    /// </summary>
    public List<Material> Materials;

    /// <summary>
    /// Whether or not to use the sparse depth map for all materials.
    /// </summary>
    public bool UseRawDepth;

    private static readonly string _currentDepthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string _topLeftRightPropertyName = "_UvTopLeftRight";
    private static readonly string _bottomLeftRightPropertyName = "_UvBottomLeftRight";

    private Texture2D _depthTexture;

    /// <summary>
    /// Assigns the depth texture to each currently added material.
    /// </summary>
    public void Attach()
    {
        foreach (Material material in Materials)
        {
            material.SetTexture(_currentDepthTexturePropertyName, _depthTexture);
            UpdateScreenOrientationOnMaterial(material);
        }
    }

    /// <summary>
    /// Initializes the depth texture and filtering mode.
    /// </summary>
    private void Start()
    {
        // Default texture, will be updated each frame.
        _depthTexture = new Texture2D(2, 2);
        _depthTexture.filterMode = FilterMode.Bilinear;

        Attach();
    }

    /// <summary>
    /// Updates the screen orientation of the depth map.
    /// </summary>
    /// <param name="material">The material to be set.</param>
    private void UpdateScreenOrientationOnMaterial(Material material)
    {
        var uvQuad = Frame.CameraImage.TextureDisplayUvs;
        material.SetVector(
            _topLeftRightPropertyName,
            new Vector4(
                uvQuad.TopLeft.x, uvQuad.TopLeft.y, uvQuad.TopRight.x, uvQuad.TopRight.y));
        material.SetVector(
            _bottomLeftRightPropertyName,
            new Vector4(uvQuad.BottomLeft.x, uvQuad.BottomLeft.y, uvQuad.BottomRight.x,
                uvQuad.BottomRight.y));
    }

    /// <summary>
    /// Retrieves the latest depth map from ARCore and assigns it to all materials.
    /// </summary>
    private void Update()
    {
        // Gets the latest sparse or smooth depth map from ARCore.
        if (UseRawDepth)
        {
            Frame.CameraImage.UpdateRawDepthTexture(ref _depthTexture);
        }
        else
        {
            Frame.CameraImage.UpdateDepthTexture(ref _depthTexture);
        }

        // Updates the screen orientation for each material.
        foreach (Material currentMaterial in Materials)
        {
            UpdateScreenOrientationOnMaterial(currentMaterial);
        }
    }
}
