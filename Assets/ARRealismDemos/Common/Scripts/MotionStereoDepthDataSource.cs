//-----------------------------------------------------------------------
// <copyright file="MotionStereoDepthDataSource.cs" company="Google LLC">
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
    private short[] m_DepthArray = new short[0];
    private int m_DepthHeight = 0;
    private int m_DepthWidth = 0;
    private bool m_Initialized = false;
    private CameraIntrinsics m_DepthCameraIntrinsics;

    private delegate bool AcquireDepthImageDelegate(
        IntPtr sessionHandle, IntPtr frameHandle, ref IntPtr depthImageHandle);

    /// <summary>
    /// Gets a value indicating whether this class is ready to serve its callers.
    /// </summary>
    public bool Initialized
    {
        get
        {
            return m_Initialized;
        }
    }

    /// <summary>
    /// Gets the CPU array that always contains the latest depth data.
    /// </summary>
    public short[] DepthArray
    {
        get
        {
            return m_DepthArray;
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
            return m_DepthCameraIntrinsics.FocalLength;
        }
    }

    /// <summary>
    /// Gets the principal point in pixels.
    /// </summary>
    public Vector2 PrincipalPoint
    {
        get
        {
            return m_DepthCameraIntrinsics.PrincipalPoint;
        }
    }

    /// <summary>
    /// Gets the intrinsic's width and height in pixels.
    /// </summary>
    public Vector2Int ImageDimensions
    {
        get
        {
            return m_DepthCameraIntrinsics.ImageDimensions;
        }
    }

    /// <summary>
    /// Updates the texture with the latest depth data from ARCore.
    /// </summary>
    /// <param name="depthTexture">The texture to update with depth data.</param>
    public void UpdateDepthTexture(ref Texture2D depthTexture)
    {
        _UpdateTexture(ref depthTexture, ref m_DepthArray, _AcquireDepthImage);
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
        _UpdateDepthArray(ref m_DepthArray, _AcquireDepthImage);
        return m_DepthArray;
    }





    /// <summary>
    /// Queries and correctly scales camera intrinsics for depth to vertex reprojection.
    /// </summary>
    private void _InitializeCameraIntrinsics()
    {
        // Gets the camera parameters to create the required number of vertices.
        m_DepthCameraIntrinsics = Frame.CameraImage.TextureIntrinsics;

        // Scales camera intrinsics to the depth map size.
        Vector2 intrinsicsScale;
        intrinsicsScale.x = m_DepthWidth / (float)m_DepthCameraIntrinsics.ImageDimensions.x;
        intrinsicsScale.y = m_DepthHeight / (float)m_DepthCameraIntrinsics.ImageDimensions.y;

        m_DepthCameraIntrinsics.FocalLength = Utilities.MultiplyVector2(
            m_DepthCameraIntrinsics.FocalLength, intrinsicsScale);
        m_DepthCameraIntrinsics.PrincipalPoint = Utilities.MultiplyVector2(
            m_DepthCameraIntrinsics.PrincipalPoint, intrinsicsScale);
        m_DepthCameraIntrinsics.ImageDimensions =
            new Vector2Int(m_DepthWidth, m_DepthHeight);

        m_Initialized = true;
    }


    /// <summary>
    /// Queries the image delegate for a texture data and updates the given array and texture.
    /// </summary>
    /// <typeparam name="T">Can be either short (for depth) or byte (for confidence).</typeparam>
    /// <param name="texture">The texture to update with new data.</param>
    /// <param name="dataArray">The CPU array to update with new data.</param>
    /// <param name="acquireImageDelegate">The function to call to obtain data.</param>
    private void _UpdateTexture<T>(
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

        int previousDepthWidth = m_DepthWidth;
        int previousDepthHeight = m_DepthHeight;

        // Gets the size of the depth data.
        ExternApi.ArImage_getWidth(
            nativeSession.SessionHandle,
            imageHandle,
            out m_DepthWidth);
        ExternApi.ArImage_getHeight(nativeSession.SessionHandle,
            imageHandle,
            out m_DepthHeight);

        if (previousDepthWidth != m_DepthWidth || previousDepthHeight != m_DepthHeight)
        {
            _InitializeCameraIntrinsics();
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
            // Pixel stride is 2, used for confidence data.
            Marshal.Copy(planeDataPtr, dataArray as byte[], 0, dataArray.Length);
        }
        else
        {
            // Pixel stride is 2, used for depth data.
            Marshal.Copy(planeDataPtr, dataArray as short[], 0, dataArray.Length);
        }

        // Resize the depth texture if needed.
        if (m_DepthWidth != texture.width || m_DepthHeight != texture.height)
        {
            if (pixelStride == 1)
            {
                texture.Resize(m_DepthWidth, m_DepthHeight, TextureFormat.R8, false);
            }
            else
            {
                texture.Resize(m_DepthWidth, m_DepthHeight, TextureFormat.RGB565, false);
            }
        }

        // Copies the raw depth data to the texture.
        texture.LoadRawTextureData(planeDataPtr, planeSize);
        texture.Apply();

        // Releases the depth image.
        LifecycleManager.Instance.NativeSession.ImageApi.Release(imageHandle);

        return;
    }

    private void _UpdateDepthArray<T>(
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

    private bool _AcquireDepthImage(
        IntPtr sessionHandle,
        IntPtr frameHandle,
        ref IntPtr depthImageHandle)
    {
        // Get the current depth image.
        ApiArStatus status = (ApiArStatus)ExternApi.ArFrame_acquireDepthImage(
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



    private struct ExternApi
    {
#pragma warning disable 626
        [AndroidImport(ApiConstants.ARCoreNativeApi)]
        public static extern ApiArStatus ArFrame_acquireDepthImage(
            IntPtr sessionHandle, IntPtr frameHandle, ref IntPtr imageHandle);





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
#pragma warning restore 626
    }
}
