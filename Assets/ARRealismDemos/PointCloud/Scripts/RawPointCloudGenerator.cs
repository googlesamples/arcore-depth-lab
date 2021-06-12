//-----------------------------------------------------------------------
// <copyright file="PointCloudGenerator.cs" company="Google LLC">
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

using System;
using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Computes a point cloud from the depth map on the CPU.
/// </summary>
public class RawPointCloudGenerator : MonoBehaviour
{
    /// <summary>
    /// Type of depth texture to attach to the material.
    /// </summary>
    public bool UseRawDepth = false;

    private const float _maxVisualizationDistanceM = 7;
    private const float _minVisualizationDistanceM = 0.4f;
    private bool _initialized;
    private CameraIntrinsics _cameraIntrinsics;
    private Mesh _mesh;


    //From Blender//
    // Limit the number of points to bound the performance cost of rendering the point cloud.
    private const int _maxVerticesInBuffer = 1000000;
    private const double _maxUpdateInvervalInSeconds = 0.5f;
    private const double _minUpdateInvervalInSeconds = 0.07f;
    //private static readonly string _confidenceThresholdPropertyName = "_ConfidenceThreshold";
   

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
    ///

    /// <summary>
    /// Computes 3D vertices from the depth map and creates a Mesh() object with the Point primitive
    /// type. Each point differently colored based on a depth color ramp.
    /// </summary>
    
    public void UpdateRawPointCloud()
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


        Reset(); //Clear previously create pointcloud
        //_pointCloudMaterial.SetFloat(_confidenceThresholdPropertyName, ConfidenceSlider.value);

        // Color and depth images usually have different aspect ratios. The depth image corresponds
        // to the region of the camera image that is center-cropped to the depth aspect ratio.
        float depthAspectRatio = (float)DepthSource.DepthHeight / DepthSource.DepthWidth;
        int colorHeightDepthAspectRatio = (int)(_cameraWidth * depthAspectRatio);
        int colorHeightOffset = (_cameraHeight - colorHeightDepthAspectRatio) / 2;

        // Creates point clouds from the depth map.
        for (int y = 0; y < DepthSource.DepthHeight; y++)
        {
            for (int x = 0; x < DepthSource.DepthWidth; x++)
            {
                int depthIndex = (y * DepthSource.DepthWidth) + x;
                float depthInM = (UseRawDepth ? DepthSource.RawDepthArray[depthIndex] :
                    DepthSource.DepthArray[depthIndex]) * DepthSource.MillimeterToMeter;
                float confidence = DepthSource.ConfidenceArray[depthIndex] / 255f;

                // Ignore missing depth values to improve runtime performance. //Only for raw depth
                if ((depthInM == 0f || confidence == 0f) && UseRawDepth)
                {
                    continue;
                }

                // Computes world-space coordinates.
                Vector3 vertex = DepthSource.TransformVertexToWorldSpace(
                    DepthSource.ComputeVertex(x, y, depthInM));


                //Add Color
                /*int colorX = x * _cameraWidth / DepthSource.DepthWidth;
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
                Color32 color = new Color32(rgb[0], rgb[1], rgb[2], confidenceByte);*/
                float depthRange = _maxVisualizationDistanceM - _minVisualizationDistanceM;
                float normalizedDepth = (depthInM - _minVisualizationDistanceM) / depthRange;
                Color32 color = ColorRampGenerator.Turbo(normalizedDepth);



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

        _mesh.SetVertices(_vertices, 0, _verticesCount);
        _mesh.SetIndices(_indices, 0, _verticesCount, MeshTopology.Points, 0);
        _mesh.SetColors(_colors, 0, _verticesCount);
        _mesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = _mesh;
        _lastUpdateTimeSeconds = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// Converts a new CPU image into byte buffers and caches to be accessed later.
    /// </summary>
    /// <param name="image">The new CPU image to process.</param>
    private void OnImageAvailable(CameraImageBytes image)
    {
        // Initializes the camera buffer and the composited texture.
        if (_cameraBufferY == null || _cameraBufferU == null || _cameraBufferV == null)
        {
            _cameraWidth = image.Width;
            _cameraHeight = image.Height;
            _rowStrideY = image.YRowStride;
            _rowStrideUV = image.UVRowStride;
            _pixelStrideUV = image.UVPixelStride;
            _cameraBufferY = new byte[image.Width * image.Height];
            _cameraBufferU = new byte[image.Width * image.Height];
            _cameraBufferV = new byte[image.Width * image.Height];
        }

        // Copies raw data into managed camera buffer.
        System.Runtime.InteropServices.Marshal.Copy(image.Y, _cameraBufferY, 0,
            image.Height * image.YRowStride);
        System.Runtime.InteropServices.Marshal.Copy(image.U, _cameraBufferU, 0,
            image.Height * image.UVRowStride / 2);
        System.Runtime.InteropServices.Marshal.Copy(image.V, _cameraBufferV, 0,
            image.Height * image.UVRowStride / 2);
    }

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
        return new[] { r, g, b };
    }

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

        // Sets the index buffer.
        for (int i = 0; i < _maxVerticesInBuffer; ++i)
        {
            _indices[i] = i;
        }

        Reset();
    }

    private void Update()
    {
        // Waits until Depth API is initialized.
        if (!_initialized && DepthSource.Initialized)
        {
            _initialized = true;
        }

        if (DepthSource.NewRawDepthAvailable)
        {
            // Fetches CPU image.
            using (var image = Frame.CameraImage.AcquireCameraImageBytes())
            {
                if (!image.IsAvailable)
                {
                    return;
                }

                OnImageAvailable(image);
            }

            //UpdateRawPointCloud();
        }

        /*transform.position = Camera.main.transform.forward * OffsetFromCamera;
        float normalizedDeltaTime = Mathf.Clamp01(
            (float)(Time.deltaTime - _minUpdateInvervalInSeconds));
        _updateInvervalInSeconds = Mathf.Lerp((float)_minUpdateInvervalInSeconds,
            (float)_maxUpdateInvervalInSeconds,
            normalizedDeltaTime);*/
    }
}
