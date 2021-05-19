//-----------------------------------------------------------------------
// <copyright file="ARBackgroundMaterialReplacer.cs" company="Google LLC">
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
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Replaces the AR background material with the provided material.
/// </summary>
public class ARBackgroundMaterialReplacer : MonoBehaviour
{
    /// <summary>
    /// Replacement material for the background renderer.
    /// </summary>
    public Material ReplacementMaterial;

    /// <summary>
    /// Flag indicating whether the replacement material should be destroyed at OnDestroy().
    /// </summary>
    public bool DestroyMaterial = false;

    private ARCameraBackground _backgroundRenderer;
    private static readonly int _showColorOnly = Shader.PropertyToID("_ShowColorOnly");

    private void Start()
    {
        if (ReplacementMaterial == null)
        {
            return;
        }

        _backgroundRenderer = FindObjectOfType<ARCameraBackground>();
        Debug.Assert(_backgroundRenderer);

        _backgroundRenderer.useCustomMaterial = true;
        _backgroundRenderer.customMaterial = ReplacementMaterial;

        // Reset background renderer to apply custom material change.
        _backgroundRenderer.enabled = false;
        _backgroundRenderer.enabled = true;

        // Resets the fragment shader.
        if (ReplacementMaterial.HasProperty(_showColorOnly))
        {
            ReplacementMaterial.SetFloat(_showColorOnly, 0f);
        }
    }

    /// <summary>
    /// Recovers the previous AR background material.
    /// </summary>
    private void UndoReplace()
    {
        if (_backgroundRenderer != null && _backgroundRenderer.useCustomMaterial)
        {
            _backgroundRenderer.useCustomMaterial = false;

            // Reset background renderer to apply custom material change.
            _backgroundRenderer.enabled = false;
            _backgroundRenderer.enabled = true;
        }
    }

    private void OnDestroy()
    {
        UndoReplace();

        // Sets the fragment shader to show only the camera image.
        if (ReplacementMaterial.HasProperty(_showColorOnly))
        {
            ReplacementMaterial.SetFloat(_showColorOnly, 1f);
        }

        if (DestroyMaterial && ReplacementMaterial != null)
        {
            DestroyImmediate(ReplacementMaterial);
        }
    }
}
