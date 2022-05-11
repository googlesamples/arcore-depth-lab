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
using System.Runtime.InteropServices;
using GoogleARCore;
using GoogleARCoreInternal;
using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
        using AndroidImport = GoogleARCoreInternal.DllImportNoop;
        using IOSImport = System.Runtime.InteropServices.DllImportAttribute;
#else
using AndroidImport = System.Runtime.InteropServices.DllImportAttribute;
using IOSImport = GoogleARCoreInternal.DllImportNoop;
#endif

/// <summary>
/// Experimental container to support depth from motion.
/// This class is forked from MotionStereoHelper.cs and adds support for
/// CPU depth data query and access to the depth map through a singleton.
/// </summary>
public class MotionStereoDepthDataSource : IDepthDataSource
{
    private short[] _depthArray = new short[0];
    private short[] _rawDepthArray = new short[0];
    private byte[] _confidenceArray = new byte[0];
    private long _rawDepthTimestamp = 0;
    private long _rawDepthTimestampPrevious = 0;
    private int _depthHeight = 0;
    private int _depthWidth = 0;
    private bool _initialized = false;
    private CameraIntrinsics _depthCameraIntrinsics;

    private delegate bool AcquireDepthImageDelegate(
        IntPtr sessionHandle, IntPtr frameHandle, ref IntPtr depthImageHandle);

    /// <summary>
    /// Gets a value indicating whether this class is ready to serve its callers.
    /// </summary>
    public bool Initialized
    {
        get
        {
            return _initialized;
        }
    }

    /// <summary>
    /// Gets the CPU array that always contains the latest depth data.
    /// </summary>
    public short[] DepthArray
    {
        get
        {
            return _depthArray;
        }
    }

    /// <summary>
    /// Gets the CPU array that always contains the latest sparse depth data.
    /// </summary>
    public short[] RawDepthArray
    {
        get
        {
            return _rawDepthArray;
        }
    }

    /// <summary>
    /// Gets the CPU array that always contains the latest sparse depth confidence data.
    /// Each pixel is a 8-bit unsigned integer representing the estimated confidence of the
    /// corresponding pixel in the depth image. The confidence value is between 0 and 255,
    /// inclusive, with 0 representing 0% confidence and 255 representing 100% confidence in the
    /// measured depth values.
    /// </summary>
    public byte[] ConfidenceArray
    {
        get
        {
            return _confidenceArray;
        }
    }

    /// <summary>
    /// Gets the focal length in pixels.
    /// Focal length is conventionally represented in pixels. For a detailed
    /// explanation, please see
    /// http://ksimek.github.io/2013/08/13/intrinsic.
    /// Pixels-to-meters conversion can use SENSOR_INFO_PHYSICAL_SIZE and
    /// SENSOR_INFO_PIXEL_ARRAY_SIZE in the Android CameraCharacteristics API.
    /// </summary>
    public Vector2 FocalLength
    {
        get
        {
            return _depthCameraIntrinsics.FocalLength;
        }
    }

    /// <summary>
    /// Gets the principal point in pixels.
    /// </summary>
    public Vector2 PrincipalPoint
    {
        get
        {
            return _depthCameraIntrinsics.PrincipalPoint;
        }
    }

    /// <summary>
    /// Gets the intrinsic's width and height in pixels.
    /// </summary>
    public Vector2Int ImageDimensions
    {
        get
        {
            return _depthCameraIntrinsics.ImageDimensions;
        }
    }

    /// <summary>
    /// Updates the texture with the latest depth data from ARCore.
    /// </summary>
    /// <param name="depthTexture">The texture to update with depth data.</param>
    public void UpdateDepthTexture(ref Texture2D depthTexture)
    {
        UpdateTexture(ref depthTexture, ref _depthArray, AcquireDepthImage);
    }

    /// <summary>
    /// Updates the texture with the latest sparse depth data from ARCore.
    /// </summary>
    /// <param name="depthTexture">The texture to update with depth data.</param>
    public void UpdateRawDepthTexture(ref Texture2D depthTexture)
    {
        UpdateTexture(ref depthTexture, ref _rawDepthArray, AcquireRawDepthImage);
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
        UpdateTexture(ref confidenceTexture, ref _confidenceArray,
                       AcquireRawDepthConfidenceImage);
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
        UpdateDepthArray(ref _depthArray, AcquireDepthImage);
        return _depthArray;
    }

    /// <summary>
    /// Triggers the sparse depth array to be updated.
    /// This is useful when UpdateSparseDepthTexture(...) is not called frequently
    /// since the sparse depth array is updated at each UpdateSparseDepthTexture(...) call.
    /// </summary>
    /// <returns>
    /// Returns a reference to the sparse depth array.
    /// </returns>
    public short[] UpdateRawDepthArray()
    {
        UpdateDepthArray(ref _rawDepthArray, AcquireRawDepthImage);
        return _rawDepthArray;
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
        UpdateDepthArray(ref _confidenceArray, AcquireRawDepthConfidenceImage);
        return _confidenceArray;
    }

    /// <summary>
    /// Query to determine if a new sparse depth frame is available.
    /// </summary>
    /// <returns>
    /// True if a new sparse depth frame is available.
    /// </returns>
    public bool NewRawDepthAvailable()
    {
        bool isNew = _rawDepthTimestampPrevious != _rawDepthTimestamp;
        _rawDepthTimestampPrevious = _rawDepthTimestamp;
        return isNew;
    }

    /// <summary>
    /// Provides an aggregate estimate of the depth confidence within the provided screen rectangle,
    /// corresponding to the depth image returned from the ArFrame.
    /// </summary>
    /// <param name="region">
    /// The screen-space rectangle. Coordinates are expressed in pixels, with (0,0) at the top
    /// left corner.
    /// </param>
    /// <returns>
    /// Aggregate estimate of confidence within the provided area within range [0,1].
    /// Confidence >= 0.5 indicates sufficient support for general depth use.
    /// </returns>
    public float GetRegionConfidence(RectInt region)
    {
        // The native session provides the session and frame handles.
        var nativeSession = LifecycleManager.Instance.NativeSession;
        if (nativeSession == null)
        {
            Debug.LogError("NativeSession is null.");
            return 0f;
        }

        float confidence = 0f;
        ExternApi.ArFrame_getDepthRegionConfidence_private(
            nativeSession.SessionHandle,
            nativeSession.FrameHandle,
            region.xMin,
            region.yMin,
            region.width,
            region.height,
            out confidence);

        return confidence;
    }

    /// <summary>
    /// Queries and correctly scales camera intrinsics for depth to vertex reprojection.
    /// </summary>
    private void InitializeCameraIntrinsics()
    {
        // Gets the camera parameters to create the required number of vertices.
        _depthCameraIntrinsics = Frame.CameraImage.TextureIntrinsics;

        // Scales camera intrinsics to the depth map size.
        Vector2 intrinsicsScale;
        intrinsicsScale.x = _depthWidth / (float)_depthCameraIntrinsics.ImageDimensions.x;
        intrinsicsScale.y = _depthHeight / (float)_depthCameraIntrinsics.ImageDimensions.y;

        _depthCameraIntrinsics.FocalLength = Utilities.MultiplyVector2(
            _depthCameraIntrinsics.FocalLength, intrinsicsScale);
        _depthCameraIntrinsics.PrincipalPoint = Utilities.MultiplyVector2(
            _depthCameraIntrinsics.PrincipalPoint, intrinsicsScale);
        _depthCameraIntrinsics.ImageDimensions =
            new Vector2Int(_depthWidth, _depthHeight);

        _initialized = true;
    }

    /// <summary>
    /// Queries the image delegate for a texture data and updates the given array and texture.
    /// </summary>
    /// <typeparam name="T">Can be either short (for depth) or byte (for confidence).</typeparam>
    /// <param name="texture">The texture to update with new data.</param>
    /// <param name="dataArray">The CPU array to update with new data.</param>
    /// <param name="acquireImageDelegate">The function to call to obtain data.</param>
    private void UpdateTexture<T>(
        ref Texture2D texture,
        ref T[] dataArray,
        AcquireDepthImageDelegate acquireImageDelegate)
    {
        // The native session provides the session and frame handles.
        var nativeSession = LifecycleManager.Instance.NativeSession;
        if (nativeSession == null)
        {
            Debug.LogError("NativeSession is null.");
            return;
        }

        // Get the current depth image.
        IntPtr imageHandle = IntPtr.Zero;
        if (acquireImageDelegate(
                nativeSession.SessionHandle,
                nativeSession.FrameHandle,
                ref imageHandle) == false)
        {
            return;
        }

        int previousDepthWidth = _depthWidth;
        int previousDepthHeight = _depthHeight;

        // Gets the size of the depth data.
        ExternApi.ArImage_getWidth(
            nativeSession.SessionHandle,
            imageHandle,
            out _depthWidth);
        ExternApi.ArImage_getHeight(nativeSession.SessionHandle,
            imageHandle,
            out _depthHeight);

        if (previousDepthWidth != _depthWidth || previousDepthHeight != _depthHeight)
        {
            InitializeCameraIntrinsics();
        }

        // Accesses the depth image surface data.
        IntPtr planeDoublePtr = IntPtr.Zero;
        int planeSize = 0;
        ExternApi.ArImage_getPlaneData(
            nativeSession.SessionHandle,
            imageHandle,
            /*plane_index*/ 0,
            ref planeDoublePtr,
            ref planeSize);
        IntPtr planeDataPtr = new IntPtr(planeDoublePtr.ToInt64());

        int pixelStride = 0;
        ExternApi.ArImage_getPlanePixelStride(nativeSession.SessionHandle,
            imageHandle,
            /*plane_index*/ 0,
            ref pixelStride);

        // Resizes the CPU data array based on the updated size.
        if (dataArray.Length != planeSize / pixelStride)
        {
            Array.Resize(ref dataArray, planeSize / pixelStride);
        }

        // Copies the depth data into the provided CPU data array.
        if (pixelStride == 1)
        {
            // Pixel stride is 1, used for confidence data.
            Marshal.Copy(planeDataPtr, dataArray as byte[], 0, dataArray.Length);
        }
        else
        {
            // Pixel stride is 2, used for depth data.
            Marshal.Copy(planeDataPtr, dataArray as short[], 0, dataArray.Length);
        }

        // Resize the depth texture if needed.
        if (_depthWidth != texture.width || _depthHeight != texture.height)
        {
            if (pixelStride == 1)
            {
                texture.Resize(_depthWidth, _depthHeight, TextureFormat.R8, false);
            }
            else
            {
                texture.Resize(_depthWidth, _depthHeight, TextureFormat.RGB565, false);
            }
        }

        // Copies the raw depth data to the texture.
        texture.LoadRawTextureData(planeDataPtr, planeSize);
        texture.Apply();

        // Releases the depth image.
        LifecycleManager.Instance.NativeSession.ImageApi.Release(imageHandle);

        return;
    }

    private void UpdateDepthArray<T>(
        ref T[] dataArray,
        AcquireDepthImageDelegate acquireImageDelegate)
    {
        // The native session provides the session and frame handles.
        var nativeSession = LifecycleManager.Instance.NativeSession;
        if (nativeSession == null)
        {
            Debug.LogError("NativeSession is null.");
            return;
        }

        // Gets the current depth image.
        IntPtr imageHandle = IntPtr.Zero;
        if (acquireImageDelegate(
                nativeSession.SessionHandle,
                nativeSession.FrameHandle,
                ref imageHandle) == false)
        {
            return;
        }

        // Gets the size of the depth data.
        int width = 0;
        int height = 0;
        ExternApi.ArImage_getWidth(
            nativeSession.SessionHandle,
            imageHandle,
            out width);
        ExternApi.ArImage_getHeight(nativeSession.SessionHandle,
            imageHandle,
            out height);

        // Accesses the depth image surface data.
        IntPtr planeDoublePtr = IntPtr.Zero;
        int planeSize = 0;
        ExternApi.ArImage_getPlaneData(
            nativeSession.SessionHandle,
            imageHandle,
            /*plane_index=*/ 0,
            ref planeDoublePtr,
            ref planeSize);
        IntPtr planeDataPtr = new IntPtr(planeDoublePtr.ToInt32());

        int depthPixelCount = width * height;

        // Resizes the CPU depth array based on the updated size.
        if (dataArray.Length != depthPixelCount)
        {
            Array.Resize(ref dataArray, depthPixelCount);
        }

        int pixelStride = 0;
        ExternApi.ArImage_getPlanePixelStride(nativeSession.SessionHandle,
            imageHandle,
            /*plane_index*/ 0,
            ref pixelStride);

        // Copies the depth data into the provided CPU depth array.
        if (pixelStride == 1)
        {
            Marshal.Copy(planeDataPtr, dataArray as byte[], 0, dataArray.Length);
        }
        else
        {
            Marshal.Copy(planeDataPtr, dataArray as short[], 0, dataArray.Length);
        }

        // Releases the depth image.
        LifecycleManager.Instance.NativeSession.ImageApi.Release(imageHandle);

        return;
    }

    private bool AcquireDepthImage(
        IntPtr sessionHandle,
        IntPtr frameHandle,
        ref IntPtr depthImageHandle)
    {
        // Get the current depth image.
        // ApiArStatus status = (ApiArStatus)ExternApi.ArFrame_acquireDepthImage(
        ApiArStatus status = (ApiArStatus)ExternApi.ArFrame_acquireDepthImage16Bits(
            sessionHandle,
            frameHandle,
            ref depthImageHandle);

        if (status != ApiArStatus.Success)
        {
            Debug.LogError(
                "DepthHelper::AcquireDepthImage could not get depth data, status: " + status);
            return false;
        }

        return true;
    }

    private bool AcquireRawDepthImage(
        IntPtr sessionHandle,
        IntPtr frameHandle,
        ref IntPtr depthImageHandle)
    {
        // Get the current depth image.
        // ApiArStatus status = (ApiArStatus)ExternApi.ArFrame_acquireRawDepthImage(
        ApiArStatus status = (ApiArStatus)ExternApi.ArFrame_acquireRawDepthImage16Bits(
            sessionHandle,
            frameHandle,
            ref depthImageHandle);

        if (status != ApiArStatus.Success)
        {
            Debug.LogError(
                "DepthHelper::AcquireSparseDepthImage could not get sparse depth data, status: " +
                status);
            return false;
        }

        ExternApi.ArImage_getTimestamp(
            sessionHandle,
            depthImageHandle,
            out _rawDepthTimestamp);

        return true;
    }

    private bool AcquireRawDepthConfidenceImage(
        IntPtr sessionHandle,
        IntPtr frameHandle,
        ref IntPtr confidenceImageHandle)
    {
        // Get the current depth image.
        ApiArStatus status = (ApiArStatus)ExternApi.ArFrame_acquireRawDepthConfidenceImage(
            sessionHandle,
            frameHandle,
            ref confidenceImageHandle);

        if (status != ApiArStatus.Success)
        {
            Debug.LogError(
                "DepthHelper::AcquireSparseDepthConfidenceImage could not get data, status: " +
                status);
            return false;
        }

        return true;
    }

    private struct ExternApi
    {
#pragma warning disable 626
        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern ApiArStatus ArFrame_acquireDepthImage16Bits(
            IntPtr sessionHandle, IntPtr frameHandle, ref IntPtr imageHandle);

        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern ApiArStatus ArFrame_acquireRawDepthImage16Bits(
            IntPtr sessionHandle, IntPtr frameHandle, ref IntPtr imageHandle);

        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern ApiArStatus ArFrame_acquireRawDepthConfidenceImage(
            IntPtr sessionHandle, IntPtr frameHandle, ref IntPtr imageHandle);

        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern void ArFrame_getDepthRegionConfidence_private(
            IntPtr sessionHandle, IntPtr frameHandle, int rectX, int rectY, int rectWidth,
            int rectHeight, out float outRegionConfidence);

        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern void ArImage_getWidth(
            IntPtr sessionHandle, IntPtr imageHandle, out int width);

        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern void ArImage_getHeight(
            IntPtr sessionHandle, IntPtr imageHandle, out int height);

        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern void ArImage_getPlaneData(
            IntPtr sessionHandle, IntPtr imageHandle, int planeIndex, ref IntPtr surfaceData,
            ref int dataLength);

        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern void ArImage_getPlanePixelStride(
            IntPtr sessionHandle, IntPtr imageHandle, int planeIndex, ref int pixelStride);

        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern void ArImage_getTimestamp(
            IntPtr sessionHandle, IntPtr imageHandle, out long timestamp);

#pragma warning restore 626
    }
}
