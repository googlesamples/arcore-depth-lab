//-----------------------------------------------------------------------
// <copyright file="AttachDepthTexture.cs" company="Google LLC">
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

/// <summary>
/// Attaches and updates the depth texture each frame.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class AttachDepthTexture : MonoBehaviour
{
    /// <summary>
    /// Type of depth texture to attach to the material.
    /// </summary>
    public bool UseRawDepth = false;

    /// <summary>
    /// Reproject intermediate sparse depth frames.
    /// </summary>
    public bool ReprojectIntermediateRawDepth = true;

    private static readonly string _currentDepthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string _topLeftRightPropertyName = "_UvTopLeftRight";
    private static readonly string _bottomLeftRightPropertyName = "_UvBottomLeftRight";
    private Texture2D _depthTexture;
    private Material _material;

    private void Start()
    {
        // Default texture, will be updated each frame.
        _depthTexture = new Texture2D(2, 2);
        _depthTexture.filterMode = FilterMode.Bilinear;

        // Assign the texture to the material.
        _material = GetComponent<Renderer>().sharedMaterial;
        _material.SetTexture(_currentDepthTexturePropertyName, _depthTexture);
        UpdateScreenOrientationOnMaterial();
    }

    private void UpdateScreenOrientationOnMaterial()
    {
        var uvQuad = Frame.CameraImage.TextureDisplayUvs;
        _material.SetVector(
            _topLeftRightPropertyName,
            new Vector4(
                uvQuad.TopLeft.x, uvQuad.TopLeft.y, uvQuad.TopRight.x, uvQuad.TopRight.y));
        _material.SetVector(
            _bottomLeftRightPropertyName,
            new Vector4(uvQuad.BottomLeft.x, uvQuad.BottomLeft.y, uvQuad.BottomRight.x,
                uvQuad.BottomRight.y));
    }

    private void Update()
    {
        // Get the latest depth data from ARCore.
        if (UseRawDepth == true)
        {
            if (ReprojectIntermediateRawDepth)
            {
                Frame.CameraImage.UpdateRawDepthTexture(ref _depthTexture);
            }
        }
        else
        {
            Frame.CameraImage.UpdateDepthTexture(ref _depthTexture);
        }

        UpdateScreenOrientationOnMaterial();
    }
}
