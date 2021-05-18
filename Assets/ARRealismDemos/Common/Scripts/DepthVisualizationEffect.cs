//-----------------------------------------------------------------------
// <copyright file="DepthVisualizationEffect.cs" company="Google LLC">
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

    private static readonly string _currentDepthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string _topLeftRightPropertyName = "_UvTopLeftRight";
    private static readonly string _bottomLeftRightPropertyName = "_UvBottomLeftRight";
    private static readonly string _rampTexturePropertyName = "_RampTexture";
    private static readonly string _cameraViewOpacityPropertyName = "_CameraViewOpacity";
    private static readonly string _maxVisualizationDistancePropertyName =
      "_MaxVisualizationDistance";

    private Camera _camera;
    private Material _material;
    private Texture2D _depthTexture;

    private void Awake()
    {
        if (DepthVisualizationEffectShader == null)
        {
            DepthVisualizationEffectShader = Shader.Find("Hidden/DepthVisualisationEffect.");
        }

        Debug.Assert(DepthVisualizationEffectShader != null,
          "DepthVisualizationEffectShader is null.");
        Debug.Assert(RampTexture != null, "_rampTexture is null");

        // Default texture, will be updated each frame.
        _depthTexture = new Texture2D(2, 2, TextureFormat.R16, /*mipmap=*/ false);

        _material = new Material(DepthVisualizationEffectShader);
        _camera = GetComponent<Camera>();
        _camera.depthTextureMode = DepthTextureMode.Depth;
        Debug.Assert(_camera.depthTextureMode != DepthTextureMode.None,
          "_camera.depthTextureMode is set to none.");
    }

    private void OnValidate()
    {
        UpdateShader();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_material == null)
        {
            _material = new Material(DepthVisualizationEffectShader);
        }

        UpdateShader();

        Graphics.Blit(source, destination, _material);
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

    private void UpdateShader()
    {
        if (_material == null)
        {
            return;
        }

        UpdateScreenOrientationOnMaterial();
        Frame.CameraImage.UpdateDepthTexture(ref _depthTexture);
        _material.SetTexture(_currentDepthTexturePropertyName, _depthTexture);
        _material.SetTexture(_rampTexturePropertyName, RampTexture);
        _material.SetFloat(_cameraViewOpacityPropertyName, CameraViewOpacity);
        _material.SetFloat(_maxVisualizationDistancePropertyName, MaximumVisualizationDistance);
    }
}
