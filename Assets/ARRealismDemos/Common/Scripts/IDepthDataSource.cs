//-----------------------------------------------------------------------
// <copyright file="IDepthDataSource.cs" company="Google LLC">
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

/// <summary>
/// An interface that defines the public properties and methods of a depth data source.
/// </summary>
public interface IDepthDataSource
{
    /// <summary>
    /// Gets a value indicating whether this class is ready to serve its callers.
    /// </summary>
    bool Initialized
    {
        get;
    }

    /// <summary>
    /// Gets the CPU array that always contains the latest depth data.
    /// </summary>
    short[] DepthArray
    {
        get;
    }

    /// <summary>
    /// Gets the CPU array that always contains the latest sparse depth confidence data.
    /// Each pixel is a 8-bit unsigned integer representing the estimated confidence of the
    /// corresponding pixel in the depth image. The confidence value is between 0 and 255,
    /// inclusive, with 0 representing 0% confidence and 255 representing 100% confidence in the
    /// measured depth values.
    /// </summary>
    byte[] ConfidenceArray
    {
        get;
    }

    /// <summary>
    /// Gets the display matrix used to covert depth texture UV to display coordinates.
    /// </summary>
    Matrix4x4 DepthDisplayMatrix
    {
        get;
    }

    /// <summary>
    /// Gets the focal length in pixels.
    /// Focal length is conventionally represented in pixels. For a detailed
    /// explanation, please see
    /// http://ksimek.github.io/2013/08/13/intrinsic.
    /// Pixels-to-meters conversion can use SENSOR_INFO_PHYSICAL_SIZE and
    /// SENSOR_INFO_PIXEL_ARRAY_SIZE in the Android CameraCharacteristics API.
    /// </summary>
    Vector2 FocalLength
    {
        get;
    }

    /// <summary>
    /// Gets the principal point in pixels.
    /// </summary>
    Vector2 PrincipalPoint
    {
        get;
    }

    /// <summary>
    /// Gets the intrinsic's width and height in pixels.
    /// </summary>
    Vector2Int ImageDimensions
    {
        get;
    }

    /// <summary>
    /// Switch to raw depth otherwise it uses smooth texture.
    /// </summary>
    /// <param name="useRawDepth">Indicates whether to use raw depth.</param>
    void SwitchToRawDepth(bool useRawDepth);

    /// <summary>
    /// Updates the texture with the latest depth data from ARCore.
    /// </summary>
    /// <param name="depthTexture">The texture to update with depth data.</param>
    void UpdateDepthTexture(ref Texture2D depthTexture);

    /// <summary>
    /// Updates the texture with the latest confidence image corresponding to the sparse depth data.
    /// Each pixel is a 8-bit unsigned integer representing the estimated confidence of the
    /// corresponding pixel in the depth image. The confidence value is between 0 and 255,
    /// inclusive, with 0 representing 0% confidence and 255 representing 100% confidence in the
    /// measured depth values.
    /// </summary>
    /// <param name="confidenceTexture">The texture to update with confidence data.</param>
    void UpdateConfidenceTexture(ref Texture2D confidenceTexture);

    /// <summary>
    /// Triggers the depth array to be updated.
    /// This is useful when UpdateDepthTexture(...) is not called frequently
    /// since the depth array is updated at each UpdateDepthTexture(...) call.
    /// </summary>
    /// <returns>
    /// Returns a reference to the depth array.
    /// </returns>
    short[] UpdateDepthArray();

    /// <summary>
    /// Triggers the confidence array to be updated from ARCore.
    /// This is useful when UpdateConfidenceTexture(...) is not called frequently
    /// since the confidence array is updated at each UpdateConfidenceTexture(...) call.
    /// </summary>
    /// <returns>
    /// Returns a reference to the confidence array.
    /// </returns>
    byte[] UpdateConfidenceArray();
}
