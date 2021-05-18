//-----------------------------------------------------------------------
// <copyright file="DepthSource.cs" company="Google LLC">
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
using System.Collections;
using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;

/// <summary>
/// This component should be attached to one active GameObject in the scene.
/// This centrally keeps depth textures up to date and serves all consumers of depth in the scene.
/// </summary>
public class DepthSource : MonoBehaviour
{
    /// <summary>
    /// Value for testing whether a depth pixel has an invalid value.
    /// </summary>
    public const float InvalidDepthValue = 0;

    /// <summary>
    /// Value for converting millimeter depth to meter.
    /// </summary>
    public const float MillimeterToMeter = 0.001f;

    /// <summary>
    /// Reproject intermediate sparse depth frames.
    /// </summary>
    public bool ReprojectIntermediateRawDepth = true;

    private static readonly string _currentDepthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string _topLeftRightPropertyName = "_UvTopLeftRight";
    private static readonly string _bottomLeftRightPropertyName = "_UvBottomLeftRight";
    private static readonly string _occlusionBlendingScale = "_OcclusionBlendingScale";

    private static Texture2D _depthTexture;
    private static Texture2D _rawDepthTexture;
    private static Texture2D _confidenceTexture;
    private static bool _newRawDepthAvailable;
    private static List<DepthTarget> _depthTargets = new List<DepthTarget>();
    private static DepthSource _instance;
    private static Matrix4x4 _screenRotation = Matrix4x4.Rotate(Quaternion.identity);
    private static Matrix4x4 _localToWorldTransform = Matrix4x4.identity;
    private static bool _updateDepth;
    private static bool _updateRawDepth;
    private static bool _updateConfidence;
    private static bool _alwaysUpdateDepth;
    private static bool _alwaysUpdateRawDepth;
    private static bool _alwaysUpdateConfidence;
    private static IDepthDataSource _depthDataSource;

    /// <summary>
    /// Gets a value indicating whether this class is ready to serve its callers.
    /// </summary>
    public static bool Initialized
    {
        get
        {
            CheckAttachedToScene();
            return _depthDataSource.Initialized;
        }
    }

    /// <summary>
    /// Gets the reference to the singleton object of DepthSource.
    /// </summary>
    public static DepthSource Instance => _instance;

    /// <summary>
    /// Gets the reference to the instance of IDepthDataSource.
    /// </summary>
    public static IDepthDataSource DepthDataSource
    {
        get
        {
            CheckAttachedToScene();
            return _depthDataSource;
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
    public static Vector2 FocalLength
    {
        get
        {
            CheckAttachedToScene();
            return _depthDataSource.FocalLength;
        }
    }

    /// <summary>
    /// Gets the principal point in pixels.
    /// </summary>
    public static Vector2 PrincipalPoint
    {
        get
        {
            CheckAttachedToScene();
            return _depthDataSource.PrincipalPoint;
        }
    }

    /// <summary>
    /// Gets the intrinsic's width and height in pixels.
    /// </summary>
    public static Vector2Int ImageDimensions
    {
        get
        {
            CheckAttachedToScene();
            return _depthDataSource.ImageDimensions;
        }
    }

    /// <summary>
    /// Gets the camera to world matrix for transforming depth vertices.
    /// </summary>
    public static Matrix4x4 LocalToWorldMatrix
    {
        get
        {
            return _localToWorldTransform;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current sparse depth map is unique or not.
    /// </summary>
    public static bool NewRawDepthAvailable
    {
        get
        {
            CheckAttachedToScene();
            return _newRawDepthAvailable;
        }
    }

    /// <summary>
    /// Gets the global reference to the depth texture.
    /// </summary>
    public static Texture2D DepthTexture
    {
        get
        {
            CheckAttachedToScene();

            if (!_updateDepth)
            {
                _depthDataSource.UpdateDepthTexture(ref _depthTexture);
                _updateDepth = true;
            }

            return _depthTexture;
        }
    }

    /// <summary>
    /// Gets the global reference to the sparse depth texture.
    /// </summary>
    public static Texture2D RawDepthTexture
    {
        get
        {
            CheckAttachedToScene();

            if (!_updateRawDepth)
            {
                _depthDataSource.UpdateRawDepthTexture(ref _rawDepthTexture);
                _updateRawDepth = true;
            }

            return _rawDepthTexture;
        }
    }

    /// <summary>
    /// Gets the global reference to the confidence texture.
    /// </summary>
    public static Texture2D ConfidenceTexture
    {
        get
        {
            CheckAttachedToScene();

            if (!(_updateDepth || _updateRawDepth || _updateConfidence))
            {
                _depthDataSource.UpdateConfidenceTexture(ref _confidenceTexture);
                _updateConfidence = true;
            }

            return _confidenceTexture;
        }
    }

    /// <summary>
    /// Gets the global reference to the CPU depth array.
    /// </summary>
    public static short[] DepthArray
    {
        get
        {
            CheckAttachedToScene();

            if (!_updateDepth)
            {
                _depthDataSource.UpdateDepthArray();
            }

            return _depthDataSource.DepthArray;
        }
    }

    /// <summary>
    /// Gets the global reference to the CPU sparse depth array.
    /// </summary>
    public static short[] RawDepthArray
    {
        get
        {
            CheckAttachedToScene();

            if (!_updateRawDepth)
            {
                _depthDataSource.UpdateRawDepthArray();
            }

            return _depthDataSource.RawDepthArray;
        }
    }

    /// <summary>
    /// Gets the global reference to the confidence array.
    /// </summary>
    public static byte[] ConfidenceArray
    {
        get
        {
            CheckAttachedToScene();

            if (!_updateDepth && !_updateRawDepth)
            {
                _depthDataSource.UpdateConfidenceArray();
            }

            return _depthDataSource.ConfidenceArray;
        }
    }

    /// <summary>
    /// Gets the width of the depth map.
    /// </summary>
    public static int DepthWidth => _depthDataSource.ImageDimensions.x;

    /// <summary>
    /// Gets the height of the depth map.
    /// </summary>
    public static int DepthHeight => _depthDataSource.ImageDimensions.y;

    /// <summary>
    /// Gets or sets a value indicating whether depth should be updated even if there is no
    /// DepthTarget in the scene.
    /// </summary>
    public static bool AlwaysUpdateDepth
    {
        get => _alwaysUpdateDepth;

        set => _alwaysUpdateDepth = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether sparse depth should be updated even if there is no
    /// DepthTarget in the scene.
    /// </summary>
    public static bool AlwaysUpdateRawDepth
    {
        get => _alwaysUpdateRawDepth;

        set => _alwaysUpdateRawDepth = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether depth should be updated even if there is no
    /// DepthTarget in the scene.
    /// </summary>
    public static bool AlwaysUpdateConfidence
    {
        get => _alwaysUpdateConfidence;

        set => _alwaysUpdateConfidence = value;
    }

    /// <summary>
    /// Gets the screen rotation transform to be used with a vertex.
    /// </summary>
    public static Matrix4x4 ScreenRotation => _screenRotation;

    /// <summary>
    /// Returns a copy of the latest depth texture. A new texture is generated unless a texture
    /// is provided.
    /// The provided texture will be resized, if the size is different.
    /// </summary>
    /// <param name="snapshot">Texture to hold the snapshot depth data.</param>
    /// <returns>Returns a texture snapshot with the latest depth data.</returns>
    public static Texture2D GetDepthTextureSnapshot(Texture2D snapshot = null)
    {
        CheckAttachedToScene();

        if (snapshot == null)
        {
            snapshot = new Texture2D(DepthTexture.width, DepthTexture.height,
                            DepthTexture.format, false);
            snapshot.Apply();
        }
        else if (snapshot.width != DepthTexture.width || snapshot.height != DepthTexture.height)
        {
            snapshot.Resize(DepthTexture.width, DepthTexture.height);
            snapshot.Apply();
        }

        Graphics.CopyTexture(DepthTexture, snapshot);
        return snapshot;
    }

    /// <summary>
    /// Returns a copy of the latest sparse depth texture.
    /// A new texture is generated unless a texture is provided.
    /// The provided texture will be resized, if the size is different.
    /// </summary>
    /// <param name="snapshot">Texture to hold the snapshot sparse depth data.</param>
    /// <returns>Returns a texture snapshot with the latest sparse depth data.</returns>
    public static Texture2D GetRawDepthTextureSnapshot(Texture2D snapshot = null)
    {
        CheckAttachedToScene();

        if (snapshot == null)
        {
            snapshot = new Texture2D(RawDepthTexture.width, RawDepthTexture.height,
                            RawDepthTexture.format, false);
            snapshot.Apply();
        }
        else if (snapshot.width != RawDepthTexture.width ||
            snapshot.height != RawDepthTexture.height)
        {
            snapshot.Resize(RawDepthTexture.width, RawDepthTexture.height);
            snapshot.Apply();
        }

        Graphics.CopyTexture(RawDepthTexture, snapshot);
        return snapshot;
    }

    /// <summary>
    /// Returns a copy of the latest confidence texture.
    /// A new texture is generated unless a texture is provided.
    /// The provided texture will be resized, if the size is different.
    /// </summary>
    /// <param name="snapshot">Texture to hold the snapshot confidence data.</param>
    /// <returns>Returns a texture snapshot with the latest confidence data.</returns>
    public static Texture2D GetConfidenceTextureSnapshot(Texture2D snapshot = null)
    {
        CheckAttachedToScene();

        if (snapshot == null)
        {
            snapshot = new Texture2D(ConfidenceTexture.width, ConfidenceTexture.height,
                            ConfidenceTexture.format, false);
            snapshot.Apply();
        }
        else if (snapshot.width != ConfidenceTexture.width ||
            snapshot.height != ConfidenceTexture.height)
        {
            snapshot.Resize(ConfidenceTexture.width, ConfidenceTexture.height);
            snapshot.Apply();
        }

        Graphics.CopyTexture(ConfidenceTexture, snapshot);
        return snapshot;
    }

    /// <summary>
    /// Returns a copy of the latest depth array. A new array is generated unless an array
    /// is provided. The provided array will be resized, if the array length is different.
    /// </summary>
    /// <param name="snapshot">Array to hold the snapshot depth data.</param>
    /// <returns>Returns an array snapshot with the latest depth data.</returns>
    public static short[] GetDepthArraySnapshot(short[] snapshot = null)
    {
        CheckAttachedToScene();

        if (snapshot == null)
        {
            snapshot = new short[DepthArray.Length];
        }
        else if (snapshot.Length != DepthArray.Length)
        {
            Array.Resize(ref snapshot, DepthArray.Length);
        }

        Array.Copy(DepthArray, snapshot, snapshot.Length);

        return snapshot;
    }

    /// <summary>
    /// Returns a copy of the latest sparse depth array. A new array is generated unless an array
    /// is provided. The provided array will be resized, if the array length is different.
    /// </summary>
    /// <param name="snapshot">Array to hold the snapshot sparse depth data.</param>
    /// <returns>Returns an array snapshot with the latest sparse depth data.</returns>
    public static short[] GetRawDepthArraySnapshot(short[] snapshot = null)
    {
        CheckAttachedToScene();

        if (snapshot == null)
        {
            snapshot = new short[RawDepthArray.Length];
        }

        Array.Copy(RawDepthArray, snapshot, snapshot.Length);

        return snapshot;
    }

    /// <summary>
    /// Returns a copy of the latest confidence array. A new array is generated unless an array
    /// is provided. The provided array will be resized, if the array length is different.
    /// </summary>
    /// <param name="snapshot">Array to hold the snapshot confidence data.</param>
    /// <returns>Returns an array snapshot with the latest confidence data.</returns>
    public static byte[] GetConfidenceArraySnapshot(byte[] snapshot = null)
    {
        CheckAttachedToScene();

        if (snapshot == null)
        {
            snapshot = new byte[ConfidenceArray.Length];
        }

        Array.Copy(ConfidenceArray, snapshot, snapshot.Length);

        return snapshot;
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
    public static float GetRegionConfidence(RectInt region)
    {
        return _depthDataSource.GetRegionConfidence(region);
    }

    /// <summary>
    /// Reprojects a depth point to a 3D vertex given the image x, y coordinates and depth z.
    /// </summary>
    /// <param name="x">Image coordinate x value.</param>
    /// <param name="y">Image coordinate y value.</param>
    /// <param name="z">Depth z from the depth map.</param>
    /// <returns>Computed 3D vertex in world space.</returns>
    public static Vector3 ComputeVertex(int x, int y, float z)
    {
        Vector3 vertex = Vector3.negativeInfinity;

        if (z > 0)
        {
            float vertex_x = (x - PrincipalPoint.x) * z / FocalLength.x;
            float vertex_y = (y - PrincipalPoint.y) * z / FocalLength.y;
            vertex.x = vertex_x;
            vertex.y = -vertex_y;
            vertex.z = z;
        }

        return vertex;
    }

    /// <summary>
    /// Reprojects a depth point to a 3D vertex given the image uv coordinates and depth z.
    /// </summary>
    /// <param name="uv">Image uv value.</param>
    /// <param name="z">Depth z from the depth map.</param>
    /// <returns>Computed 3D vertex in world space.</returns>
    public static Vector3 ComputeVertex(Vector2 uv, float z)
    {
        int x = (int)(uv.x * (DepthWidth - 1));
        int y = (int)(uv.y * (DepthHeight - 1));

        return ComputeVertex(x, y, z);
    }

    /// <summary>
    /// Converts normalized uv coordinates to integer xy coordinates.
    /// </summary>
    /// <param name="uv">Input uv coordinates.</param>
    /// <returns>Returns integer xy coordinates.</returns>
    public static Vector2Int DepthUVtoXY(Vector2 uv)
    {
        return new Vector2Int((int)(uv.x * (DepthWidth - 1)), (int)(uv.y * (DepthHeight - 1)));
    }

    /// <summary>
    /// Converts xy coordinates to normalized uv coordinates.
    /// </summary>
    /// <param name="x">The x component.</param>
    /// <param name="y">The y component.</param>
    /// <returns>Returns the normalized uv coordinates.</returns>
    public static Vector2 DepthXYtoUV(int x, int y)
    {
        return new Vector2(x / (float)(DepthWidth - 1), y / (float)(DepthHeight - 1));
    }

    /// <summary>
    /// Converts xy coordinates to normalized uv coordinates.
    /// </summary>
    /// <param name="xy">The xy coordinates.</param>
    /// <returns>Returns the normalized uv coordinates.</returns>
    public static Vector2 DepthXYtoUV(Vector2Int xy)
    {
        return DepthXYtoUV(xy.x, xy.y);
    }

    /// <summary>
    /// Transform a camera-space vertex into world space.
    /// </summary>
    /// <param name="vertex">Camera-space vertex.</param>
    /// <returns>Returns the transformed vertex.</returns>
    public static Vector3 TransformVertexToWorldSpace(Vector3 vertex)
    {
        return LocalToWorldMatrix.MultiplyPoint(vertex);
    }

    /// <summary>
    /// Given screen-space x, y, returns a world-space vertex from the provided depth map.
    /// </summary>
    /// <param name="x">Screen space x.</param>
    /// <param name="y">Screen space y.</param>
    /// <param name="depthArray">Depth map to calculate the vertex from.</param>
    /// <returns>Returns a world-space vertex.</returns>
    public static Vector3 GetVertexInWorldSpaceFromScreenXY(int x, int y, short[] depthArray = null)
    {
        Vector2Int depthXY = ScreenToDepthXY(x, y);

        if (depthArray == null)
        {
            depthArray = DepthArray;
        }

        float depth = GetDepthFromXY(depthXY.x, depthXY.y, depthArray);
        Vector3 vertex = ComputeVertex(depthXY.x, depthXY.y, depth);
        return TransformVertexToWorldSpace(vertex);
    }

    /// <summary>
    /// Given screen-space x, y, returns a world-space vertex from the provided depth map.
    /// </summary>
    /// <param name="uv">Screen space uv.</param>
    /// <param name="depthArray">Depth map to calculate the vertex from.</param>
    /// <returns>Returns a world-space vertex.</returns>
    public static Vector3 GetVertexInWorldSpaceFromScreenUV(Vector2 uv, short[] depthArray = null)
    {
        int screenX = (int)(uv.x * (Screen.width - 1));
        int screenY = (int)(uv.y * (Screen.height - 1));

        return GetVertexInWorldSpaceFromScreenXY(screenX, screenY, depthArray);
    }

    /// <summary>
    /// Obtains the depth value in meters at a normalized screen point.
    /// </summary>
    /// <param name="uv">The normalized screen point in portrait mode.</param>
    /// <param name="depthArray">The depth array to be used.</param>
    /// <returns>The depth value in meters.</returns>
    public static float GetDepthFromUV(Vector2 uv, short[] depthArray)
    {
        int depthX = (int)(uv.x * (DepthWidth - 1));
        int depthY = (int)(uv.y * (DepthHeight - 1));

        return GetDepthFromXY(depthX, depthY, depthArray);
    }

    /// <summary>
    /// Obtains the depth value in meters at the specified x, y location.
    /// </summary>
    /// <param name="x">The x offset in the depth map.</param>
    /// <param name="y">The y offset in the depth map.</param>
    /// <param name="depthArray">The depth array to be used.</param>
    /// <returns>The depth value in meters.</returns>
    public static float GetDepthFromXY(int x, int y, short[] depthArray)
    {
        if (!Initialized)
        {
            return InvalidDepthValue;
        }

        if (x >= DepthWidth || x < 0 || y >= DepthHeight || y < 0)
        {
            return InvalidDepthValue;
        }

        var depthIndex = (y * DepthWidth) + x;
        var depthInShort = depthArray[depthIndex];
        var depthInMeters = depthInShort * MillimeterToMeter;
        return depthInMeters;
    }

    /// <summary>
    /// Converts the screen uv coordinates to depth uv coordinates.
    /// </summary>
    /// <param name="uv">The screen uv coordinates.</param>
    /// <returns>Returns the depth uv coordinates.</returns>
    public static Vector2 ScreenToDepthUV(Vector2 uv)
    {
        Vector2 uvTop = Vector2.Lerp(Frame.CameraImage.TextureDisplayUvs.TopLeft,
            Frame.CameraImage.TextureDisplayUvs.TopRight, uv.x);
        Vector2 uvBottom = Vector2.Lerp(Frame.CameraImage.TextureDisplayUvs.BottomLeft,
            Frame.CameraImage.TextureDisplayUvs.BottomRight, uv.x);
        return Vector2.Lerp(uvTop, uvBottom, uv.y);
    }

    /// <summary>
    /// Converts the screen xy coordinates to depth xy coordinates.
    /// </summary>
    /// <param name="x">The screen x component.</param>
    /// <param name="y">The screen y component.</param>
    /// <returns>Returns the depth xy coordinates.</returns>
    public static Vector2Int ScreenToDepthXY(int x, int y)
    {
        Vector2 uv = new Vector2(x / (float)(Screen.width - 1), y / (float)(Screen.height - 1));
        uv = ScreenToDepthUV(uv);
        x = (int)(uv.x * (DepthWidth - 1));
        y = (int)(uv.y * (DepthHeight - 1));

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Adds the DepthTarget to a list of depth consumers.
    /// </summary>
    /// <param name="target">A DepthTarget instance, which uses depth.</param>
    public static void AddDepthTarget(DepthTarget target)
    {
        CheckAttachedToScene();

        if (!_depthTargets.Contains(target))
        {
            _depthTargets.Add(target);

            if (target.DepthTargetMaterial != null)
            {
                SetDepthTexture(target);
            }
        }
    }

    /// <summary>
    /// When the provided x,y depth coordinates are aligned with the phone's current orientation,
    /// reorients the coordinates for a correct look up in the depth map.
    /// </summary>
    /// <param name="x">The x component.</param>
    /// <param name="y">The y component.</param>
    /// <param name="relativeCoordinates">Indicates whether the coordinates are relative.</param>
    /// <returns>Returns the reoriented x,y depth coordinates.</returns>
    public static Vector2Int ReorientDepthXY(int x, int y, bool relativeCoordinates = true)
    {
        Vector2Int result = new Vector2Int();
        int coeff = relativeCoordinates ? 0 : 1;

        switch (Screen.orientation)
        {
            case ScreenOrientation.Portrait:
                result.x = y;
                result.y = (coeff * (DepthHeight - 1)) - x;
                break;
            case ScreenOrientation.LandscapeRight:
                result = new Vector2Int(x, y);
                break;
            case ScreenOrientation.PortraitUpsideDown:
                result.x = (coeff * (DepthWidth - 1)) - y;
                result.y = x;
                break;
            case ScreenOrientation.LandscapeLeft:
                result.x = (coeff * (DepthWidth - 1)) - x;
                result.y = (coeff * (DepthHeight - 1)) - y;
                break;
        }

        return result;
    }

    /// <summary>
    /// When the provided u,v depth coordinates are aligned with the phones current orientation,
    /// then this function reorients the coordinates for a correct look up in the depth map.
    /// </summary>
    /// <param name="uv">The uv coordinates.</param>
    /// <param name="relativeCoordinates">Indicates whether the coordinates are relative.</param>
    /// <returns>Returns the reoriented u,v depth coordinates.</returns>
    public static Vector2 ReorientDepthUV(Vector2 uv, bool relativeCoordinates = true)
    {
        Vector2 result = uv;
        float coeff = relativeCoordinates ? 0 : 1;

        switch (Screen.orientation)
        {
            case ScreenOrientation.Portrait:
                result.x = uv.y;
                result.y = coeff - uv.x;
                break;
            case ScreenOrientation.LandscapeRight:
                result = uv;
                break;
            case ScreenOrientation.PortraitUpsideDown:
                result.x = coeff - uv.y;
                result.y = uv.x;
                break;
            case ScreenOrientation.LandscapeLeft:
                result.x = coeff - uv.x;
                result.y = coeff - uv.y;
                break;
        }

        return result;
    }

    /// <summary>
    /// Removes the DepthTarget from a list of depth consumers.
    /// </summary>
    /// <param name="target">A DepthTarget instance, which uses depth.</param>
    public static void RemoveDepthTarget(DepthTarget target)
    {
        CheckAttachedToScene();

        if (_depthTargets.Contains(target))
        {
            _depthTargets.Remove(target);
        }
    }

    /// <summary>
    /// Checks whether this component is part of the scene.
    /// </summary>
    private static void CheckAttachedToScene()
    {
        if (_instance == null)
        {
            if (Camera.main != null)
            {
                _instance = Camera.main.gameObject.AddComponent<DepthSource>();
            }
        }
    }

    private static void SetDepthTexture(DepthTarget target)
    {
        Texture2D depthTexture = target.UseRawDepth ? RawDepthTexture : DepthTexture;

        if (target.SetAsMainTexture)
        {
            if (target.DepthTargetMaterial.mainTexture != depthTexture)
            {
                target.DepthTargetMaterial.mainTexture = depthTexture;
            }
        }
        else if (target.DepthTargetMaterial.HasProperty(_currentDepthTexturePropertyName) &&
                 target.DepthTargetMaterial.GetTexture(_currentDepthTexturePropertyName) !=
            depthTexture)
        {
            target.DepthTargetMaterial.SetTexture(_currentDepthTexturePropertyName,
                depthTexture);
        }
    }

    /// <summary>
    /// Sets a rotation Matrix4x4 to correctly transform the point cloud when the phone is used
    /// in different screen orientations.
    /// </summary>
    private static void UpdateScreenOrientation()
    {
        switch (Screen.orientation)
        {
            case ScreenOrientation.Portrait:
                _screenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, -90));
                break;
            case ScreenOrientation.LandscapeLeft:
                _screenRotation = Matrix4x4.Rotate(Quaternion.identity);
                break;
            case ScreenOrientation.PortraitUpsideDown:
                _screenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90));
                break;
            case ScreenOrientation.LandscapeRight:
                _screenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 180));
                break;
        }
    }

    private void Start()
    {
        var config = (DepthDataSourceConfig)Resources.Load("DepthDataSourceConfig");
        if (config != null && config.DepthDataSource != null)
        {
            _depthDataSource = config.DepthDataSource;
        }

        _instance = this;
        _alwaysUpdateDepth = true;

        // Default texture, will be updated each frame.
        _depthTexture = new Texture2D(2, 2);
        _rawDepthTexture = new Texture2D(2, 2);
        _confidenceTexture = new Texture2D(2, 2);

        _depthTexture.filterMode = FilterMode.Bilinear;
        _rawDepthTexture.filterMode = FilterMode.Point;
        _confidenceTexture.filterMode = FilterMode.Point;

        _depthTexture.wrapMode = TextureWrapMode.Clamp;
        _rawDepthTexture.wrapMode = TextureWrapMode.Clamp;
        _confidenceTexture.wrapMode = TextureWrapMode.Clamp;

        foreach (DepthTarget target in _depthTargets)
        {
            if (target.DepthTargetMaterial != null)
            {
                SetDepthTexture(target);
                UpdateScreenOrientationOnMaterial(target.DepthTargetMaterial);
                SetAlphaForBlendedOcclusionProperties(target.DepthTargetMaterial);
            }
        }
    }

    private void UpdateScreenOrientationOnMaterial(Material material)
    {
        var uvQuad = Frame.CameraImage.TextureDisplayUvs;
        material.SetVector(
            _topLeftRightPropertyName,
            new Vector4(
                uvQuad.TopLeft.x, uvQuad.TopLeft.y, uvQuad.TopRight.x, uvQuad.TopRight.y));
        material.SetVector(
            _bottomLeftRightPropertyName,
            new Vector4(uvQuad.BottomLeft.x, uvQuad.BottomLeft.y, uvQuad.BottomRight.x,
                uvQuad.BottomRight.y));
    }

    private void SetAlphaForBlendedOcclusionProperties(Material material)
    {
        material.SetFloat(_occlusionBlendingScale, 0.5f);
    }

    private void Update()
    {
        UpdateScreenOrientation();

        _localToWorldTransform = Camera.main.transform.localToWorldMatrix *
            DepthSource.ScreenRotation;

        bool updateSparseDepth = false;
        bool updateDepth = false;

        foreach (DepthTarget target in _depthTargets)
        {
            if (!target.UseRawDepth && !updateDepth)
            {
                updateDepth = true;
            }

            if (target.UseRawDepth && !updateSparseDepth)
            {
                updateSparseDepth = true;
            }

            if (target.DepthTargetMaterial != null)
            {
                SetDepthTexture(target);
                UpdateScreenOrientationOnMaterial(target.DepthTargetMaterial);
                SetAlphaForBlendedOcclusionProperties(target.DepthTargetMaterial);
            }
        }

        _updateDepth = updateDepth || _alwaysUpdateDepth;
        _updateRawDepth = updateSparseDepth || _alwaysUpdateRawDepth;
        _updateConfidence = _updateDepth || _updateRawDepth || _alwaysUpdateConfidence;
        _newRawDepthAvailable = _depthDataSource.NewRawDepthAvailable();

        if (_updateRawDepth)
        {
            // Updates depth from ARCore, only if at least one DepthTarget uses depth.
            if (ReprojectIntermediateRawDepth)
            {
                // Always get the depth frame, reprojected as needed.
                _depthDataSource.UpdateRawDepthTexture(ref _rawDepthTexture);
            }
            else
            {
                // Only get new depth frames as they are available.
                if (_newRawDepthAvailable)
                {
                    _depthDataSource.UpdateRawDepthTexture(ref _rawDepthTexture);
                }
            }
        }

        if (_updateConfidence)
        {
            // Gets the latest confidence texture from ARCore.
            _depthDataSource.UpdateConfidenceTexture(ref _confidenceTexture);
        }

        if (_updateDepth)
        {
            // Updates depth from ARCore, only if at least one DepthTarget uses depth.
            _depthDataSource.UpdateDepthTexture(ref _depthTexture);
        }
    }
}
