//-----------------------------------------------------------------------
// <copyright file="MotionStereoDepthDataSource.cs" company="Google LLC">
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
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = UnityEngine.Object;

/// <summary>
/// Contains the CPU images and Texture2D related to depth estimation.
/// </summary>
public class MotionStereoDepthDataSource : IDepthDataSource
{
    private Texture2D _confidenceTexture = Texture2D.blackTexture;
    private Matrix4x4 _depthDisplayMatrix = Matrix4x4.identity;

    private short[] _depthArray = new short[0];
    private byte[] _depthBuffer = new byte[0];
    private byte[] _confidenceArray = new byte[0];

    private int _depthHeight = 0;
    private int _depthWidth = 0;
    private bool _initialized = false;
    private bool _useRawDepth = false;

    private XRCameraIntrinsics _depthCameraIntrinsics;
    private ARCameraManager _cameraManager;
    private AROcclusionManager _occlusionManager;
    private ARCameraBackground _cameraBackground;

    private delegate bool AcquireDepthImageDelegate(
        IntPtr sessionHandle, IntPtr frameHandle, ref IntPtr depthImageHandle);

    public MotionStereoDepthDataSource()
    {
        InitializeCameraIntrinsics();
    }

    /// <summary>
    /// Gets a value indicating whether this class is ready to serve its callers.
    /// </summary>
    public bool Initialized
    {
        get
        {
            if (!_initialized)
            {
                InitializeCameraIntrinsics();
            }

            return _initialized;
        }
    }

    /// <summary>
    /// Gets the CPU array that always contains the latest depth data.
    /// </summary>
    public short[] DepthArray => _depthArray;

    /// <summary>
    /// Gets the CPU array that always contains the latest sparse depth confidence data.
    /// Each pixel is a 8-bit unsigned integer representing the estimated confidence of the
    /// corresponding pixel in the depth image. The confidence value is between 0 and 255,
    /// inclusive, with 0 representing 0% confidence and 255 representing 100% confidence in the
    /// measured depth values.
    /// </summary>
    public byte[] ConfidenceArray => _confidenceArray;

    /// <summary>
    /// Gets the display matrix used to covert depth texture UV to display coordinates.
    /// </summary>
    public Matrix4x4 DepthDisplayMatrix => _depthDisplayMatrix;

    /// <summary>
    /// Gets the focal length in pixels.
    /// Focal length is conventionally represented in pixels. For a detailed
    /// explanation, please see
    /// http://ksimek.github.io/2013/08/13/intrinsic.
    /// Pixels-to-meters conversion can use SENSOR_INFO_PHYSICAL_SIZE and
    /// SENSOR_INFO_PIXEL_ARRAY_SIZE in the Android CameraCharacteristics API.
    /// </summary>
    public Vector2 FocalLength => _depthCameraIntrinsics.focalLength;

    /// <summary>
    /// Gets the principal point in pixels.
    /// </summary>
    public Vector2 PrincipalPoint => _depthCameraIntrinsics.principalPoint;

    /// <summary>
    /// Gets the intrinsic's width and height in pixels.
    /// </summary>
    public Vector2Int ImageDimensions => _depthCameraIntrinsics.resolution;

    /// <summary>
    /// Updates the texture with the latest depth data from ARCore.
    /// </summary>
    /// <param name="depthTexture">The texture to update with depth data.</param>
    public void UpdateDepthTexture(ref Texture2D depthTexture)
    {
        depthTexture = _occlusionManager.environmentDepthTexture;
    }

    /// <summary>
    /// Switch to raw depth otherwise it uses smooth texture.
    /// </summary>
    /// <param name="useRawDepth">Indicates whether to use raw depth.</param>
    public void SwitchToRawDepth(bool useRawDepth)
    {
        // Enable smooth depth by default.
        _occlusionManager.environmentDepthTemporalSmoothingRequested = !_useRawDepth;
    }

    /// <summary>
    /// Updates the texture with the latest confidence image corresponding to the sparse depth data.
    /// Each pixel is a 8-bit unsigned integer representing the estimated confidence of the
    /// corresponding pixel in the depth image. The confidence value is between 0 and 255,
    /// inclusive, with 0 representing 0% confidence and 255 representing 100% confidence in the
    /// measured depth values.
    /// </summary>
    /// <param name="confidenceTexture">The texture to update with confidence data.</param>
    public void UpdateConfidenceTexture(ref Texture2D confidenceTexture)
    {
        confidenceTexture = _confidenceTexture;
    }

    /// <summary>
    /// Triggers the depth array to be updated.
    /// This is useful when UpdateDepthTexture(...) is not called frequently
    /// since the depth array is updated at each UpdateDepthTexture(...) call.
    /// </summary>
    /// <returns>
    /// Returns a reference to the depth array.
    /// </returns>
    public short[] UpdateDepthArray()
    {
        int bufferLength = _depthWidth * _depthHeight;
        if (_depthArray.Length != bufferLength)
        {
            _depthArray = new short[bufferLength];
        }

        Buffer.BlockCopy(_depthBuffer, 0, _depthArray, 0, _depthBuffer.Length);
        return _depthArray;
    }

    /// <summary>
    /// Triggers the confidence array to be updated from ARCore.
    /// This is useful when UpdateConfidenceTexture(...) is not called frequently
    /// since the confidence array is updated at each UpdateConfidenceTexture(...) call.
    /// </summary>
    /// <returns>
    /// Returns a reference to the confidence array.
    /// </returns>
    public byte[] UpdateConfidenceArray()
    {
        _confidenceArray = _confidenceTexture.GetRawTextureData();
        return _confidenceArray;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        _depthDisplayMatrix = eventArgs.displayMatrix.GetValueOrDefault();

        if (_cameraBackground.useCustomMaterial && _cameraBackground.customMaterial != null)
        {
            _cameraBackground.customMaterial.SetTexture(DepthSource.DepthTexturePropertyName,
                _occlusionManager.environmentDepthTexture);
            _cameraBackground.customMaterial.SetMatrix(DepthSource.DisplayTransformPropertyName,
                _depthDisplayMatrix);
        }

        UpdateEnvironmentDepthImage();
        UpdateEnvironmentDepthConfidenceImage();
    }

    /// <summary>
    /// Queries and correctly scales camera intrinsics for depth to vertex reprojection.
    /// </summary>
    private void InitializeCameraIntrinsics()
    {
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            Debug.LogWarningFormat("ARSession is not ready yet: {0}", ARSession.state);
            return;
        }

        _cameraManager ??= Object.FindObjectOfType<ARCameraManager>();
        Debug.Assert(_cameraManager);
        _cameraManager.frameReceived += OnCameraFrameReceived;

        _occlusionManager ??= Object.FindObjectOfType<AROcclusionManager>();
        Debug.Assert(_occlusionManager);

        // Enable smooth depth by default.
        _occlusionManager.environmentDepthTemporalSmoothingRequested = !_useRawDepth;

        _cameraBackground ??= Object.FindObjectOfType<ARCameraBackground>();
        Debug.Assert(_cameraBackground);

        // Gets the camera parameters to create the required number of vertices.
        if (!_cameraManager.TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics))
        {
            Debug.LogError("MotionStereoDepthDataSource: Failed to obtain camera intrinsics.");
            return;
        }

        // Scales camera intrinsics to the depth map size.
        Vector2 intrinsicsScale;
        intrinsicsScale.x = _depthWidth / (float)cameraIntrinsics.resolution.x;
        intrinsicsScale.y = _depthHeight / (float)cameraIntrinsics.resolution.y;

        var focalLength = Utilities.MultiplyVector2(cameraIntrinsics.focalLength, intrinsicsScale);
        var principalPoint =
            Utilities.MultiplyVector2(cameraIntrinsics.principalPoint, intrinsicsScale);
        var resolution = new Vector2Int(_depthWidth, _depthHeight);
        _depthCameraIntrinsics = new XRCameraIntrinsics(focalLength, principalPoint, resolution);

        if (_depthCameraIntrinsics.resolution != Vector2.zero)
        {
            _initialized = true;
            Debug.Log("MotionStereoDepthDataSource intrinsics initialized.");
        }
    }

    void UpdateEnvironmentDepthImage()
    {
        if (_occlusionManager &&
            _occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image))
        {
            using (image)
            {
                _depthWidth = image.width;
                _depthHeight = image.height;

                int numPixels = image.width * image.height;
                int numBytes = numPixels * image.GetPlane(0).pixelStride;

                if (_depthBuffer.Length != numBytes)
                {
                    _depthBuffer = new byte[numBytes];
                }

                image.GetPlane(0).data.CopyTo(_depthBuffer);
            }
        }
    }

    void UpdateEnvironmentDepthConfidenceImage()
    {
        if (_occlusionManager &&
            _occlusionManager.TryAcquireEnvironmentDepthConfidenceCpuImage(out XRCpuImage image))
        {
            using (image)
            {
                UpdateRawImage(ref _confidenceTexture, image, TextureFormat.R8, TextureFormat.R8);
            }
        }
    }

    static void UpdateRawImage(ref Texture2D texture, XRCpuImage cpuImage,
        TextureFormat conversionFormat, TextureFormat textureFormat)
    {
        if (texture == null || texture.width != cpuImage.width || texture.height != cpuImage.height)
        {
            texture = new Texture2D(cpuImage.width, cpuImage.height, textureFormat, false);
        }

        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, conversionFormat);
        var rawTextureData = texture.GetRawTextureData<byte>();

        cpuImage.Convert(conversionParams, rawTextureData);
        texture.Apply();
    }
}