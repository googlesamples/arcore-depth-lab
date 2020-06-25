//-----------------------------------------------------------------------
// <copyright file="DepthVisualizationEffect.cs" company="Google LLC">
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
using UnityEngine.Serialization;

/// <summary>
/// Add comment here.
/// </summary>
[RequireComponent(typeof(Camera))]
public class DepthVisualizationEffect : MonoBehaviour
{
    /// <summary>
    /// The DepthVisualisationEffect shader for the image effect.
    /// </summary>
    [Tooltip("The DepthVisualisationEffect shader for the image effect.")]
    [FormerlySerializedAs("depthVisualizationEffectShader")]
    public Shader DepthVisualizationEffectShader;

    /// <summary>
    /// The color pallete to use in depth visualization.
    /// </summary>
    [Space, Tooltip("The color pallete to use in depth visualization.")]
    [FormerlySerializedAs("m_RampTexture")]
    public Texture2D RampTexture;

    /// <summary>
    /// Opacity (a.k.a., alpha value) of the camera stream.
    /// </summary>
    [Space, Range(0, 1)]
    [FormerlySerializedAs("m_CameraViewOpacity")]
    public float CameraViewOpacity = 0f;

    /// <summary>
    /// Maximum distance in meters to visualize.
    /// </summary>
    [Range(0f, 8.192f)]
    [FormerlySerializedAs("m_MaximumVisualizationDistance")]
    public float MaximumVisualizationDistance = 6.0f;

    private static readonly string k_CurrentDepthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string k_TopLeftRightPropertyName = "_UvTopLeftRight";
    private static readonly string k_BottomLeftRightPropertyName = "_UvBottomLeftRight";
    private static readonly string k_RampTexturePropertyName = "_RampTexture";
    private static readonly string k_CameraViewOpacityPropertyName = "_CameraViewOpacity";
    private static readonly string k_MaxVisualizationDistancePropertyName =
      "_MaxVisualizationDistance";

    private Camera m_Camera;
    private Material m_Material;
    private Texture2D m_DepthTexture;

    private void Awake()
    {
        if (DepthVisualizationEffectShader == null)
        {
            DepthVisualizationEffectShader = Shader.Find("Hidden/DepthVisualisationEffect.");
        }

        Debug.Assert(DepthVisualizationEffectShader != null,
          "DepthVisualizationEffectShader is null.");
        Debug.Assert(RampTexture != null, "m_RampTexture is null");

        // Default texture, will be updated each frame.
        m_DepthTexture = new Texture2D(2, 2, TextureFormat.R16, /*mipmap=*/ false);

        m_Material = new Material(DepthVisualizationEffectShader);
        m_Camera = GetComponent<Camera>();
        m_Camera.depthTextureMode = DepthTextureMode.Depth;
        Debug.Assert(m_Camera.depthTextureMode != DepthTextureMode.None,
          "m_Camera.depthTextureMode is set to none.");
    }

    private void OnValidate()
    {
        UpdateShader();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_Material == null)
        {
            m_Material = new Material(DepthVisualizationEffectShader);
        }

        UpdateShader();

        Graphics.Blit(source, destination, m_Material);
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

    private void UpdateShader()
    {
        if (m_Material == null)
        {
            return;
        }

        UpdateScreenOrientationOnMaterial();
        Frame.CameraImage.UpdateDepthTexture(ref m_DepthTexture);
        m_Material.SetTexture(k_CurrentDepthTexturePropertyName, m_DepthTexture);
        m_Material.SetTexture(k_RampTexturePropertyName, RampTexture);
        m_Material.SetFloat(k_CameraViewOpacityPropertyName, CameraViewOpacity);
        m_Material.SetFloat(k_MaxVisualizationDistancePropertyName, MaximumVisualizationDistance);
    }
}
