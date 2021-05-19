//-----------------------------------------------------------------------
// <copyright file="RawPointCloudBlender.cs" company="Google LLC">
//
// Copyright 2021 Google LLC
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
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Computes a point cloud from the depth map on the CPU.
/// </summary>
public class RawPointCloudBlender : MonoBehaviour
{
    /// <summary>
    /// Type of depth texture to attach to the material.
    /// </summary>
    public bool UseRawDepth = true;

    /// <summary>
    /// Slider that controls visibility of point clouds based on confidence threshold.
    /// </summary>
    public UnityEngine.UI.Slider ConfidenceSlider;

    /// <summary>
    /// The translation between the point cloud and the device camera along the camera's forward
    /// axis. This can be used to visualize a larger area of the point cloud on the screen at a
    /// time, creating a "third person" camera effect.
    /// </summary>
    public float OffsetFromCamera = 1.0f;

    // Limit the number of points to bound the performance cost of rendering the point cloud.
    private const int _maxVerticesInBuffer = 1000000;
    private const double _maxUpdateInvervalInSeconds = 0.5f;
    private const double _minUpdateInvervalInSeconds = 0.07f;
    private static readonly string _confidenceThresholdPropertyName = "_ConfidenceThreshold";
    private bool _initialized;
    private ARCameraManager _cameraManager;
    private XRCameraIntrinsics _cameraIntrinsics;
    private Mesh _mesh;

    private Vector3[] _vertices = new Vector3[_maxVerticesInBuffer];
    private int _verticesCount = 0;
    private int _verticesIndex = 0;
    private int[] _indices = new int[_maxVerticesInBuffer];
    private Color32[] _colors = new Color32[_maxVerticesInBuffer];

    // Buffers that store the color camera image (in YUV420_888 format) each frame.
    private byte[] _cameraBufferY;
    private byte[] _cameraBufferU;
    private byte[] _cameraBufferV;
    private int _cameraHeight;
    private int _cameraWidth;
    private int _pixelStrideUV;
    private int _rowStrideY;
    private int _rowStrideUV;
    private double _updateInvervalInSeconds = _minUpdateInvervalInSeconds;
    private double _lastUpdateTimeSeconds;
    private Material _pointCloudMaterial;
    private bool _cachedUseRawDepth = false;

    /// <summary>
    /// Resets the point cloud renderer.
    /// </summary>
    public void Reset()
    {
        _verticesCount = 0;
        _verticesIndex = 0;
    }

    private void Start()
    {
        _mesh = new Mesh();
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _pointCloudMaterial = GetComponent<Renderer>().material;
        _cameraManager = FindObjectOfType<ARCameraManager>();
        _cameraManager.frameReceived += OnCameraFrameReceived;

        // Sets the index buffer.
        for (int i = 0; i < _maxVerticesInBuffer; ++i)
        {
            _indices[i] = i;
        }

        Reset();

        if (ConfidenceSlider == null)
        {
            throw new Exception("ConfidenceSlider is not assigned.");
        }
    }

    private void Update()
    {
        // Waits until Depth API is initialized.
        if (!_initialized && DepthSource.Initialized)
        {
            _initialized = true;
        }

        if (_initialized)
        {
            if (_cachedUseRawDepth != UseRawDepth)
            {
                DepthSource.SwitchToRawDepth(UseRawDepth);
                _cachedUseRawDepth = UseRawDepth;
            }

            UpdateRawPointCloud();
        }

        transform.position = DepthSource.ARCamera.transform.forward * OffsetFromCamera;
        float normalizedDeltaTime = Mathf.Clamp01(
            (float)(Time.deltaTime - _minUpdateInvervalInSeconds));
        _updateInvervalInSeconds = Mathf.Lerp((float)_minUpdateInvervalInSeconds,
            (float)_maxUpdateInvervalInSeconds,
            normalizedDeltaTime);
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {   
        if (_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cameraImage))
        {
            using(cameraImage)
            {
                if (cameraImage.format == XRCpuImage.Format.AndroidYuv420_888)
                {
                    OnImageAvailable(cameraImage);
                }
            }
        }
    }

    /// <summary>
    /// Converts a new CPU image into byte buffers and caches to be accessed later.
    /// </summary>
    /// <param name="image">The new CPU image to process.</param>
    private void OnImageAvailable(XRCpuImage image)
    {
        if (_cameraBufferY == null || _cameraBufferU == null || _cameraBufferV == null)
        {
            _cameraWidth = image.width;
            _cameraHeight = image.height;
            _rowStrideY = image.GetPlane(0).rowStride;
            _rowStrideUV = image.GetPlane(1).rowStride;
            _pixelStrideUV = image.GetPlane(1).pixelStride;
            _cameraBufferY = new byte[image.GetPlane(0).data.Length];
            _cameraBufferU = new byte[image.GetPlane(1).data.Length];
            _cameraBufferV = new byte[image.GetPlane(2).data.Length];
        }

        image.GetPlane(0).data.CopyTo(_cameraBufferY);
        image.GetPlane(1).data.CopyTo(_cameraBufferU);
        image.GetPlane(2).data.CopyTo(_cameraBufferV);
    }

    /// <summary>
    /// Computes 3D vertices from the depth map and updates mesh with the Point primitive type.
    /// </summary>
    private void UpdateRawPointCloud()
    {
        // Exits when ARCore is not ready.
        if (!_initialized || _cameraBufferY == null)
        {
            return;
        }

        // Exits if updating the point cloud too frequently for better performance.
        if (Time.realtimeSinceStartup - _lastUpdateTimeSeconds < _updateInvervalInSeconds)
        {
            return;
        }

        _pointCloudMaterial.SetFloat(_confidenceThresholdPropertyName, ConfidenceSlider.value);

        // Color and depth images usually have different aspect ratios. The depth image corresponds
        // to the region of the camera image that is center-cropped to the depth aspect ratio.
        float depthAspectRatio = (float)DepthSource.DepthHeight / DepthSource.DepthWidth;
        int colorHeightDepthAspectRatio = (int)(_cameraWidth * depthAspectRatio);
        int colorHeightOffset = (_cameraHeight - colorHeightDepthAspectRatio) / 2;

        short[] depthArray = DepthSource.DepthArray;
        if (depthArray.Length != DepthSource.DepthWidth * DepthSource.DepthHeight)
        {
            // Depth array is not yet available.
            return;
        }

        byte[] confidenceArray = DepthSource.ConfidenceArray;
        bool noConfidenceAvailable = depthArray.Length != confidenceArray.Length;

        // Creates point clouds from the depth map.
        for (int y = 0; y < DepthSource.DepthHeight; y++)
        {
            for (int x = 0; x < DepthSource.DepthWidth; x++)
            {
                int depthIndex = (y * DepthSource.DepthWidth) + x;
                float depthInM = depthArray[depthIndex] * DepthSource.MillimeterToMeter;
                float confidence = noConfidenceAvailable ? 1f : confidenceArray[depthIndex] / 255f;

                // Ignore missing depth values to improve runtime performance.
                if (depthInM == 0f || confidence == 0f)
                {
                    continue;
                }

                // Computes world-space coordinates.
                Vector3 vertex = DepthSource.TransformVertexToWorldSpace(
                    DepthSource.ComputeVertex(x, y, depthInM));

                int colorX = x * _cameraWidth / DepthSource.DepthWidth;
                int colorY = colorHeightOffset +
                    (y * colorHeightDepthAspectRatio / DepthSource.DepthHeight);
                int linearIndexY = (colorY * _rowStrideY) + colorX;
                int linearIndexUV = ((colorY / 2) * _rowStrideUV) + ((colorX / 2) * _pixelStrideUV);

                // Each channel value is an unsigned byte.
                byte channelValueY = _cameraBufferY[linearIndexY];
                byte channelValueU = _cameraBufferU[linearIndexUV];
                byte channelValueV = _cameraBufferV[linearIndexUV];

                byte[] rgb = ConvertYuvToRgb(channelValueY, channelValueU, channelValueV);
                byte confidenceByte = (byte)(confidence * 255f);
                Color32 color = new Color32(rgb[0], rgb[1], rgb[2], confidenceByte);

                if (_verticesCount < _maxVerticesInBuffer - 1)
                {
                    ++_verticesCount;
                }

                // Replaces old vertices in the buffer after reaching the maximum capacity.
                if (_verticesIndex >= _maxVerticesInBuffer)
                {
                    _verticesIndex = 0;
                }

                _vertices[_verticesIndex] = vertex;
                _colors[_verticesIndex] = color;
                ++_verticesIndex;
            }
        }

        if (_verticesCount == 0)
        {
            return;
        }

        // Assigns graphical buffers.
#if UNITY_2019_3_OR_NEWER
        _mesh.SetVertices(_vertices, 0, _verticesCount);
        _mesh.SetIndices(_indices, 0, _verticesCount, MeshTopology.Points, 0);
        _mesh.SetColors(_colors, 0, _verticesCount);
#else
        // Note that we recommend using Unity 2019.3 or above to compile this scene.
        List<Vector3> vertexList = new List<Vector3>();
        List<Color32> colorList = new List<Color32>();
        List<int> indexList = new List<int>();

        for (int i = 0; i < _verticesCount; ++i)
        {
            vertexList.Add(_vertices[i]);
            indexList.Add(_indices[i]);
            colorList.Add(_colors[i]);
        }

        _mesh.SetVertices(vertexList);
        _mesh.SetIndices(indexList.ToArray(), MeshTopology.Points, 0);
        _mesh.SetColors(colorList);
#endif // UNITY_2019_3_OR_NEWER

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = _mesh;
        _lastUpdateTimeSeconds = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// Converts a YUV color value into RGB. Input YUV values are expected in the range [0, 255].
    /// </summary>
    /// <param name="y">The pixel value of the Y plane in the range [0, 255].</param>
    /// <param name="u">The pixel value of the U plane in the range [0, 255].</param>
    /// <param name="v">The pixel value of the V plane in the range [0, 255].</param>
    /// <returns>RGB values are in the range [0.0, 1.0].</returns>
    private byte[] ConvertYuvToRgb(byte y, byte u, byte v)
    {
        // See https://en.wikipedia.org/wiki/YUV.
        float yFloat = y / 255.0f; // Range [0.0, 1.0].
        float uFloat = (u * 0.872f / 255.0f) - 0.436f; // Range [-0.436, 0.436].
        float vFloat = (v * 1.230f / 255.0f) - 0.615f; // Range [-0.615, 0.615].
        float rFloat = Mathf.Clamp01(yFloat + (1.13983f * vFloat));
        float gFloat = Mathf.Clamp01(yFloat - (0.39465f * uFloat) - (0.58060f * vFloat));
        float bFloat = Mathf.Clamp01(yFloat + (2.03211f * uFloat));
        byte r = (byte)(rFloat * 255f);
        byte g = (byte)(gFloat * 255f);
        byte b = (byte)(bFloat * 255f);
        return new[] {r, g, b};
    }
}
