//-----------------------------------------------------------------------
// <copyright file="DepthPulseEffect.cs" company="Google LLC">
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

using System.Collections;
using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates and starts the depth pulse. The pulse can either start from the current depth to the
/// camera or to the horizon.
/// </summary>
public class DepthPulseEffect : MonoBehaviour
{
    /// <summary>
    /// The depth value where the pulse starts fading.
    /// </summary>
    public float StartFadingDepth = _startFadingDepth;

    /// <summary>
    /// The maximum distance that the pulse can travel to.
    /// </summary>
    public float MaximumPulseDepth = _maximumPulseDepth;

    /// <summary>
    /// The duration of the pulse, in seconds.
    /// </summary>
    public float PulseDurationS = _pulseDurationS;

    /// <summary>
    /// The width of the pulse.
    /// </summary>
    public float PulseWidthM = _pulseWidthM;

    /// <summary>
    /// The attached mesh which is used to receive the shadow.
    /// </summary>
    public ShadowReceiverMesh ShadowReceiver;

    // A small default texture size to create a texture of unknown size.
    private const int _defaultTextureSize = 2;
    private const float _maximumPulseDepth = 7;
    private const float _startFadingDepth = 6;
    private const float _pulseDurationS = 5;
    private const float _pulseWidthM = 0.4f;

    private static readonly Vector3 _defaultMeshOffset = new Vector3(-100, -100, -100);

    // Holds the vertex and index data of the depth template mesh.
    private Mesh _mesh;

    // Holds the calibrated camera's intrinsic parameters.
    private CameraIntrinsics _intrinsics;

    // This is the scale vector to appropriately scale the camera intrinsics to the depth texture.
    private Vector2 _intrinsicsScale;
    private Texture2D _depthTexture;
    private bool _initialized = false;

    private Material _material;
    private float _pulseDepth = 0;
    private Coroutine _currentCoroutine;
    private Matrix4x4 _screenRotation = Matrix4x4.Rotate(Quaternion.identity);

    /// <summary>
    /// Starts generating a new pulse towards the camera. Stops the existing pulse if necessary.
    /// </summary>
    public void StartPulseToCamera()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(AnimateValue(0, PulseDurationS));
    }

    /// <summary>
    /// Starts generating a new pulse towards the horizon. Stops the existing pulse if necessary.
    /// </summary>
    public void StartPulseToHorizon()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(
            AnimateValue(MaximumPulseDepth, PulseDurationS));
    }

    private static int[] GenerateTriangles(int width, int height)
    {
        int[] indices = new int[(height - 1) * (width - 1) * 6];
        int idx = 0;
        for (int y = 0; y < (height - 1); y++)
        {
            for (int x = 0; x < (width - 1); x++)
            {
                //// Unity has a clockwise triangle winding order.
                //// Upper quad triangle
                //// Top left
                int idx0 = (y * width) + x;
                //// Top right
                int idx1 = idx0 + 1;
                //// Bottom left
                int idx2 = idx0 + width;

                //// Lower quad triangle
                //// Top right
                int idx3 = idx1;
                //// Bottom right
                int idx4 = idx2 + 1;
                //// Bottom left
                int idx5 = idx2;

                indices[idx++] = idx0;
                indices[idx++] = idx1;
                indices[idx++] = idx2;
                indices[idx++] = idx3;
                indices[idx++] = idx4;
                indices[idx++] = idx5;
            }
        }

        return indices;
    }

    private void InitializeMesh()
    {
        // Get the camera parameters to create the required number of vertices.
        _intrinsics = Frame.CameraImage.TextureIntrinsics;

        // Scale camera intrinsics to the depth map size.
        _intrinsicsScale.x = _depthTexture.width / (float)_intrinsics.ImageDimensions.x;
        _intrinsicsScale.y = _depthTexture.height / (float)_intrinsics.ImageDimensions.y;

        // Create template vertices.
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        // Create template vertices for the mesh object.
        for (int y = 0; y < _depthTexture.height; y++)
        {
            for (int x = 0; x < _depthTexture.width; x++)
            {
                Vector3 v = new Vector3(x * 0.01f, -y * 0.01f, 0) + _defaultMeshOffset;
                vertices.Add(v);
                normals.Add(Vector3.back);
            }
        }

        // Create template triangle list.
        int[] triangles = GenerateTriangles(_depthTexture.width, _depthTexture.height);

        // Create the mesh object and set all template data.
        _mesh = new Mesh();
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _mesh.SetVertices(vertices);
        _mesh.SetNormals(normals);
        _mesh.SetTriangles(triangles, 0);
        _mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000));
        _mesh.UploadMeshData(true);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = _mesh;

        float principalPointX = _intrinsics.PrincipalPoint.x * _intrinsicsScale.x;
        float principalPointY = _intrinsics.PrincipalPoint.y * _intrinsicsScale.y;

        // Set camera intrinsics for depth reprojection.
        _material.SetFloat("_FocalLengthX", _intrinsics.FocalLength.x * _intrinsicsScale.x);
        _material.SetFloat("_FocalLengthY", _intrinsics.FocalLength.y * _intrinsicsScale.y);
        _material.SetFloat("_PrincipalPointX", principalPointX);
        _material.SetFloat("_PrincipalPointY", principalPointY);
        _material.SetInt("_ImageDimensionsX", _depthTexture.width);
        _material.SetInt("_ImageDimensionsY", _depthTexture.height);

        _initialized = true;
    }

    private void Start()
    {
        // Default texture, will be updated each frame.
        _depthTexture = new Texture2D(_defaultTextureSize, _defaultTextureSize);

        // Assign the texture to the material.
        _material = GetComponent<Renderer>().material;
        _material.SetTexture("_CurrentDepthTexture", _depthTexture);
        UpdateScreenOrientation();
    }

    private void Update()
    {
        // Get the latest depth data from ARCore.
        Frame.CameraImage.UpdateDepthTexture(ref _depthTexture);
        UpdateShaderVariables();
        UpdateScreenOrientation();

        if (!_initialized && _depthTexture.width != _defaultTextureSize
            && _depthTexture.height != _defaultTextureSize)
        {
            InitializeMesh();
        }
    }

    private void UpdateShaderVariables()
    {
        _material.SetFloat("_PulseWidth", PulseWidthM);
        _material.SetFloat("_PulseDepth", _pulseDepth);
        _material.SetFloat("_MaximumPulseDepth", MaximumPulseDepth);
        _material.SetFloat("_StartFadingDepth", StartFadingDepth);
        ShadowReceiver.MaximumMeshDistance = _pulseDepth;
    }

    private IEnumerator AnimateValue(float targetValue, float animationTime)
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = true;

        float originalPulseDepth = _pulseDepth;
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / animationTime)
        {
            _pulseDepth = Mathf.Lerp(originalPulseDepth, targetValue, t);
            yield return null;
        }

        _pulseDepth = targetValue;

        meshRenderer.enabled = false;
    }

    private void UpdateScreenOrientation()
    {
        switch (Screen.orientation)
        {
            case ScreenOrientation.Portrait:
                _screenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90));
                break;
            case ScreenOrientation.LandscapeLeft:
                _screenRotation = Matrix4x4.Rotate(Quaternion.identity);
                break;
            case ScreenOrientation.PortraitUpsideDown:
                _screenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, -90));
                break;
            case ScreenOrientation.LandscapeRight:
                _screenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 180));
                break;
        }

        _material.SetMatrix("_ScreenRotation", _screenRotation);
    }
}
