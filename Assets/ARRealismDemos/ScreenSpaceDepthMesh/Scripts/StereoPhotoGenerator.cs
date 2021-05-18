//-----------------------------------------------------------------------
// <copyright file="StereoPhotoGenerator.cs" company="Google LLC">
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
/// Captures an animated stereoscopic photo by freezing a depth mesh and
/// projecting a cached camera image onto the mesh.
/// This scene is built upon `ScreenSpaceDepthMeshGenerator.cs`.
/// </summary>
public class StereoPhotoGenerator : MonoBehaviour
{
    /// <summary>
    /// Holds the current camera material renderer to obtain live render texture.
    /// </summary>
    public DemoARBackgroundRenderer DemoRenderer;

    /// <summary>
    /// A secondary camera to render the animated stereoscopic photo.
    /// </summary>
    public Camera StereoPhotoCamera;

    // Specifies the maximum distance between vertices of a single triangle to be rendered.
    private const float _triangleConnectivityCutOff = 0.5f;

    // Tests if a value is close to zero.
    private const float _epsilon = 1e-6f;

    // Maximum distance of the depth value in meters.
    private const float _maximumDistance = 4f;

    // Minimum rotating radius of the cached camera in meters.
    private const float _cameraRotationRadiusMin = 0.0017f;

    // Maximum rotating radius of the cached camera in meters.
    private const float _cameraRotationRadiusMax = 0.05f;

    // Rotating speed of the cached camera.
    private const float _cameraRotationSpeed = 5f;

    // Percentage to move camera towards the target mesh to crop the animated stereoscopic photo.
    private const float _cachedCameraLeaningTargetPercetangeMin = 0.05f;
    private const float _cachedCameraLeaningTargetPercetangeMax = 0.2f;

    private static readonly Vector3 _defaultMeshOffset = new Vector3(-100, -100, -100);
    private static readonly string _depthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string _cameraTexturePropertyName = "_MainTex";
    private static readonly string _cameraViewMatrixPropertyName = "_CameraViewMatrix";
    private static readonly string _vertexModelTransformPropertyName = "_VertexModelTransform";
    private static readonly string _textureProjectionMatrixPropertyName =
      "_TextureProjectionMatrix";

    // Holds the vertex and index data of the depth template mesh.
    private Mesh _mesh;

    // Whether the mesh is frozen.
    private bool _freezeMesh = false;

    // Whether the mesh is initialized.
    private bool _initialized = false;

    // Holds a copy of the depth frame at the time the frame was frozen.
    private Texture2D _staticDepthTexture = null;

    // Holds a copy of the color frame at the time the frame was frozen.
    private Texture2D _staticColorTexture = null;

    private Vector3 _cachedCameraPosition = new Vector3();
    private Vector3 _cachedCameraUp = new Vector3();
    private Vector3 _cachedTargetPosition = new Vector3();
    private Vector3 _cachedCameraPlaneAxisA = new Vector3();
    private Vector3 _cachedCameraPlaneAxisB = new Vector3();
    private Matrix4x4 _cachedModelViewMatrix = new Matrix4x4();
    private Matrix4x4 _cachedModeMatrix = new Matrix4x4();
    private float _cameraRotationRadius = _cameraRotationRadiusMax;

    /// <summary>
    /// Starts the capture coroutine, which is used to hide UI elements
    /// from the background texture grab.
    /// </summary>
    public void CaptureStereoPhoto()
    {
        StartCoroutine(CaptureStereoPhotoCoroutine());
    }

    /// <summary>
    /// Resumes to update the depth texture on every frame.
    /// </summary>
    public void UnfreezeToPreviewMode()
    {
        _freezeMesh = false;
        Material material = GetComponent<Renderer>().material;
        material.SetTexture(_depthTexturePropertyName, DepthSource.DepthTexture);
        Destroy(_staticDepthTexture);
        _staticDepthTexture = null;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        StereoPhotoCamera.enabled = false;
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

    /// <summary>
    /// Disables the stereo photo camera when started and only shows camera preview.
    /// </summary>
    private void Start()
    {
        StereoPhotoCamera.enabled = false;
    }

    /// <summary>
    /// Lerps value from min to max exponentially.
    /// </summary>
    /// <param name="min">Minimum of the lerp.</param>
    /// <param name="max">Maximum of the lerp.</param>
    /// <param name="value">Value to lerp.</param>
    /// <param name="exponent">Speed of the lerp.</param>
    /// <returns>Returns the lerped value between min and max.</returns>
    private float ExponentialLerp(float min, float max, float value, float exponent = 0.7f)
    {
        return min + (Mathf.Pow(value, exponent) * (max - min));
    }

    /// <summary>
    /// Takes a snapshot of the current depth array, the current camera parameters;
    /// computes two basis vectors of the current camera plane, and
    /// sets a static depth texture and a camera texture.
    /// </summary>
    /// <returns>Returns null.</returns>
    private IEnumerator CaptureStereoPhotoCoroutine()
    {
        // De-activate UI elements and wait for the next frame to capture.
        GameObject CarouselObj = GameObject.Find("Carousel UI");
        GameObject InfoCanvasObj = GameObject.Find("InfoCanvas");
        GameObject CanvasObj = GameObject.Find("Canvas");
        CarouselObj.SetActive(false);
        InfoCanvasObj.SetActive(false);
        CanvasObj.SetActive(false);
        yield return null;

        _freezeMesh = true;
        _cachedCameraPosition = Camera.main.transform.position;
        _cachedTargetPosition = DepthSource.GetVertexInWorldSpaceFromScreenXY(
                                    Screen.width / 2, Screen.height / 2,
                                    DepthSource.DepthArray);
        _cachedModelViewMatrix = Camera.main.worldToCameraMatrix * DepthSource.LocalToWorldMatrix;
        _cachedModeMatrix = DepthSource.LocalToWorldMatrix;

        // Computes the direction from the target to the cached camera.
        var direction = Vector3.Normalize(_cachedTargetPosition - _cachedCameraPosition);

        // To crop the boundary, adaptively moves the camera towards the target.
        var cachedCameraLeaningTargetPercetange = Mathf.SmoothStep(
            _cachedCameraLeaningTargetPercetangeMin,
            _cachedCameraLeaningTargetPercetangeMax,
            Mathf.Clamp(_cachedTargetPosition.z / _maximumDistance, 0f, 1f));

        // Changes the camera rotation radius based on the distance.
        _cameraRotationRadius = Mathf.SmoothStep(
            _cameraRotationRadiusMin,
            _cameraRotationRadiusMax,
            Mathf.Clamp(_cachedTargetPosition.z / _maximumDistance, 0f, 1f));

        // Moves the camera position a little bit towards the target to crop the boundary.
        _cachedCameraPosition += direction * cachedCameraLeaningTargetPercetange;

        // Computes an arbitrary non-zero basis of the camera plane orthognoal to the direction.
        if (Mathf.Abs(direction.z) > _epsilon)
        {
            _cachedCameraPlaneAxisA =
              new Vector3(1f, 1f, (direction.x + direction.y) / -direction.z);
        }
        else
        if (Mathf.Abs(direction.x) > _epsilon)
        {
            _cachedCameraPlaneAxisA =
              new Vector3((direction.y + direction.z) / -direction.x, 1f, 1f);
        }
        else
        if (Mathf.Abs(direction.y) > _epsilon)
        {
            _cachedCameraPlaneAxisA =
              new Vector3(1f, (direction.x + direction.z) / -direction.y, 1f);
        }
        else
        {
            Debug.LogError("Error: Camera is too close to the target object.");
        }

        // Computes two orthognonal basis of the cached camera plane.
        _cachedCameraPlaneAxisA = Vector3.Normalize(_cachedCameraPlaneAxisA);
        _cachedCameraPlaneAxisB = Vector3.Normalize(
          Vector3.Cross(_cachedCameraPlaneAxisA, direction));

        // Binds the depth texture.
        _staticDepthTexture = DepthSource.GetDepthTextureSnapshot();
        Material material = GetComponent<Renderer>().material;
        material.SetTexture(_depthTexturePropertyName, _staticDepthTexture);

        // Initializes a static color texture.
        if (_staticColorTexture == null)
        {
            _staticColorTexture = new Texture2D(
                Screen.width, Screen.height,
                TextureFormat.RGBA32, /*mipChain=*/false)
            {
                filterMode = FilterMode.Point
            };

            _staticColorTexture.Apply();
        }

        // Grabs the static color texture from the background render texture.
        Rect rectReadPicture = new Rect(0, 0, Screen.width, Screen.height);
        RenderTexture.active = DemoRenderer.BackgroundRenderTexture;
        _staticColorTexture.ReadPixels(rectReadPicture, 0, 0);
        _staticColorTexture.Apply();
        material.SetTexture(_cameraTexturePropertyName, _staticColorTexture);

        // Enables the secondary camera for the effect.
        StereoPhotoCamera.enabled = true;

        // Stores the current camera's up vector.
        _cachedCameraUp = Camera.main.transform.TransformDirection(Vector3.up);
        StereoPhotoCamera.transform.SetPositionAndRotation(
          _cachedCameraPosition, Camera.main.transform.rotation);

        // Re-activates UI elements.
        CarouselObj.SetActive(true);
        InfoCanvasObj.SetActive(true);
        CanvasObj.SetActive(true);
    }

    private void InitializeMesh()
    {
        // Creates template vertices.
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        // Creates template vertices for the mesh object.
        for (int y = 0; y < DepthSource.DepthHeight; y++)
        {
            for (int x = 0; x < DepthSource.DepthWidth; x++)
            {
                Vector3 v = new Vector3(x * 0.01f, -y * 0.01f, 0) + _defaultMeshOffset;
                vertices.Add(v);
                normals.Add(Vector3.back);
            }
        }

        // Creates template triangle list.
        int[] triangles = GenerateTriangles(DepthSource.DepthWidth, DepthSource.DepthHeight);

        // Creates the mesh object and set all template data.
        _mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        _mesh.SetVertices(vertices);
        _mesh.SetNormals(normals);
        _mesh.SetTriangles(triangles, 0);
        _mesh.bounds = new Bounds(Vector3.zero, new Vector3(50, 50, 50));
        _mesh.UploadMeshData(true);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = _mesh;

        // Sets camera intrinsics for depth reprojection.
        Material material = GetComponent<Renderer>().material;
        material.SetTexture("_CurrentDepthTexture", DepthSource.DepthTexture);
        material.SetFloat("_FocalLengthX", DepthSource.FocalLength.x);
        material.SetFloat("_FocalLengthY", DepthSource.FocalLength.y);
        material.SetFloat("_PrincipalPointX", DepthSource.PrincipalPoint.x);
        material.SetFloat("_PrincipalPointY", DepthSource.PrincipalPoint.y);
        material.SetInt("_ImageDimensionsX", DepthSource.ImageDimensions.x);
        material.SetInt("_ImageDimensionsY", DepthSource.ImageDimensions.y);
        material.SetFloat("_TriangleConnectivityCutOff", _triangleConnectivityCutOff);

        _initialized = true;
    }

    private void Update()
    {
        var mesh_renderer = GetComponent<Renderer>();

        if (!_freezeMesh)
        {
            // Enables the mesh renderer when the capture mode is enabled.
            if (mesh_renderer.enabled)
            {
                mesh_renderer.enabled = false;
            }
        }
        else
        {
            if (!mesh_renderer.enabled)
            {
                Material material = mesh_renderer.material;
                material.SetMatrix(
                  _vertexModelTransformPropertyName, DepthSource.LocalToWorldMatrix);
                material.SetMatrix(
                  _cameraViewMatrixPropertyName, Camera.main.worldToCameraMatrix);
                material.SetMatrix(
                  _textureProjectionMatrixPropertyName, Camera.main.projectionMatrix);
                mesh_renderer.enabled = true;
            }
        }

        if (!_initialized && DepthSource.Initialized)
        {
            InitializeMesh();
        }

        if (mesh_renderer.enabled)
        {
            // Computes the parametric equation of the camera path.
            float theta = Time.realtimeSinceStartup * _cameraRotationSpeed;
            StereoPhotoCamera.transform.position = _cachedCameraPosition
                + (_cameraRotationRadius *
                ((Mathf.Cos(theta) * _cachedCameraPlaneAxisA) +
                 (Mathf.Sin(theta) * _cachedCameraPlaneAxisB)));
            StereoPhotoCamera.transform.LookAt(_cachedTargetPosition, _cachedCameraUp);

            // Updates the projection matrix for correct uv coordinates.
            Material material = mesh_renderer.material;
            var inverseModelViewMatrix = Matrix4x4.Inverse(
              StereoPhotoCamera.worldToCameraMatrix * _cachedModeMatrix);
            var projectionMatrix = StereoPhotoCamera.projectionMatrix *
              _cachedModelViewMatrix * inverseModelViewMatrix;
            material.SetMatrix(_textureProjectionMatrixPropertyName, projectionMatrix);
        }
    }
}
