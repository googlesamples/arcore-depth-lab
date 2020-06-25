//-----------------------------------------------------------------------
// <copyright file="DepthTextureProvider.cs" company="Google LLC">
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
/// This script simply queries the depth texture and applies it as the main texture
/// of the material of this GameObject.
/// </summary>
public class DepthTextureProvider : MonoBehaviour
{
    /// <summary>
    /// Type of depth texture to attach to the material.
    /// </summary>
    public bool UseSparseDepth = false;

    /// <summary>
    /// Reproject intermediate sparse depth frames.
    /// </summary>
    public bool ReprojectIntermediateSparseDepth = true;

    private Texture2D m_DepthTexture;

    private void Start()
    {
        // Default texture, will be updated each frame.
        m_DepthTexture = new Texture2D(2, 2);

        // Assign the depth texture as the main texture in the material.
        Material material = GetComponent<Renderer>().sharedMaterial;
        material.mainTexture = m_DepthTexture;
    }

    private void Update()
    {
        DepthSource.DepthDataSource.UpdateDepthTexture(ref m_DepthTexture);
    }
}
