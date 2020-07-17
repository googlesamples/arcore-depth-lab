//-----------------------------------------------------------------------
// <copyright file="StereoPhotoGenerator.cs" company="Google LLC">
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
    private const float k_TriangleConnectivityCutOff = 0.5f;

    // Tests if a value is close to zero.
    private const float k_Epsilon = 1e-6f;

    // Maximum distance of the depth value in meters.
    private const float k_MaximumDistance = 4f;

    // Minimum rotating radius of the cached camera in meters.
    private const float k_CameraRotationRadiusMin = 0.0017f;

    // Maximum rotating radius of the cached camera in meters.
    private const float k_CameraRotationRadiusMax = 0.05f;

    // Rotating speed of the cached camera.
    private const float k_CameraRotationSpeed = 5f;

    // Percentage to move camera towards the target mesh to crop the animated stereoscopic photo.
    private const float k_CachedCameraLeaningTargetPercetangeMin = 0.05f;
    private const float k_CachedCameraLeaningTargetPercetangeMax = 0.2f;

    private static readonly Vector3 k_DefaultMeshOffset = new Vector3(-100, -100, -100);
    private static readonly string k_DepthTexturePropertyName = "_CurrentDepthTexture";
    private static readonly string k_CameraTexturePropertyName = "_MainTex";
    private static readonly string k_CameraViewMatrixPropertyName = "_CameraViewMatrix";
    private static readonly string k_VertexModelTransformPropertyName = "_VertexModelTransform";
    private static readonly string k_TextureProjectionMatrixPropertyName =
      "_TextureProjectionMatrix";

    // Holds the vertex and index data of the depth template mesh.
    private Mesh m_Mesh;

    // Whether the mesh is frozen.
    private bool m_FreezeMesh = false;

    // Whether the mesh is initialized.
    private bool m_Initialized = false;

    // Holds a copy of the depth frame at the time the frame was frozen.
    private Texture2D m_StaticDepthTexture = null;

    // Holds a copy of the color frame at the time the frame was frozen.
    private Texture2D m_StaticColorTexture = null;

    private Vector3 m_CachedCameraPosition = new Vector3();
    private Vector3 m_CachedCameraUp = new Vector3();
    private Vector3 m_CachedTargetPosition = new Vector3();
    private Vector3 m_CachedCameraPlaneAxisA = new Vector3();
    private Vector3 m_CachedCameraPlaneAxisB = new Vector3();
    private Matrix4x4 m_CachedModelViewMatrix = new Matrix4x4();
    private Matrix4x4 m_CachedModeMatrix = new Matrix4x4();
    private float m_CameraRotationRadius = k_CameraRotationRadiusMax;

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
        m_FreezeMesh = false;
        Material material = GetComponent<Renderer>().material;
        material.SetTexture(k_DepthTexturePropertyName, DepthSource.DepthTexture);
        Destroy(m_StaticDepthTexture);
        m_StaticDepthTexture = null;

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

        m_FreezeMesh = true;
        m_CachedCameraPosition = Camera.main.transform.position;
        m_CachedTargetPosition = DepthSource.GetVertexInWorldSpaceFromScreenXY(
                                    Screen.width / 2, Screen.height / 2,
                                    DepthSource.DepthArray);
        m_CachedModelViewMatrix = Camera.main.worldToCameraMatrix * DepthSource.LocalToWorldMatrix;
        m_CachedModeMatrix = DepthSource.LocalToWorldMatrix;

        // Computes the direction from the target to the cached camera.
        var direction = Vector3.Normalize(m_CachedTargetPosition - m_CachedCameraPosition);

        // To crop the boundary, adaptively moves the camera towards the target.
        var cachedCameraLeaningTargetPercetange = Mathf.SmoothStep(
            k_CachedCameraLeaningTargetPercetangeMin,
            k_CachedCameraLeaningTargetPercetangeMax,
            Mathf.Clamp(m_CachedTargetPosition.z / k_MaximumDistance, 0f, 1f));

        // Changes the camera rotation radius based on the distance.
        m_CameraRotationRadius = Mathf.SmoothStep(
            k_CameraRotationRadiusMin,
            k_CameraRotationRadiusMax,
            Mathf.Clamp(m_CachedTargetPosition.z / k_MaximumDistance, 0f, 1f));

        // Moves the camera position a little bit towards the target to crop the boundary.
        m_CachedCameraPosition += direction * cachedCameraLeaningTargetPercetange;

        // Computes an arbitrary non-zero basis of the camera plane orthognoal to the direction.
        if (Mathf.Abs(direction.z) > k_Epsilon)
        {
            m_CachedCameraPlaneAxisA =
              new Vector3(1f, 1f, (direction.x + direction.y) / -direction.z);
        }
        else
        if (Mathf.Abs(direction.x) > k_Epsilon)
        {
            m_CachedCameraPlaneAxisA =
              new Vector3((direction.y + direction.z) / -direction.x, 1f, 1f);
        }
        else
        if (Mathf.Abs(direction.y) > k_Epsilon)
        {
            m_CachedCameraPlaneAxisA =
              new Vector3(1f, (direction.x + direction.z) / -direction.y, 1f);
        }
        else
        {
            Debug.LogError("Error: Camera is too close to the target object.");
        }

        // Computes two orthognonal basis of the cached camera plane.
        m_CachedCameraPlaneAxisA = Vector3.Normalize(m_CachedCameraPlaneAxisA);
        m_CachedCameraPlaneAxisB = Vector3.Normalize(
          Vector3.Cross(m_CachedCameraPlaneAxisA, direction));

        // Binds the depth texture.
        m_StaticDepthTexture = DepthSource.GetDepthTextureSnapshot();
        Material material = GetComponent<Renderer>().material;
        material.SetTexture(k_DepthTexturePropertyName, m_StaticDepthTexture);

        // Initializes a static color texture.
        if (m_StaticColorTexture == null)
        {
            m_StaticColorTexture = new Texture2D(
                Screen.width, Screen.height,
                TextureFormat.RGBA32, /*mipChain=*/false)
            {
                filterMode = FilterMode.Point
            };

            m_StaticColorTexture.Apply();
        }

        // Grabs the static color texture from the background render texture.
        Rect rectReadPicture = new Rect(0, 0, Screen.width, Screen.height);
        RenderTexture.active = DemoRenderer.BackgroundRenderTexture;
        m_StaticColorTexture.ReadPixels(rectReadPicture, 0, 0);
        m_StaticColorTexture.Apply();
        material.SetTexture(k_CameraTexturePropertyName, m_StaticColorTexture);

        // Enables the secondary camera for the effect.
        StereoPhotoCamera.enabled = true;

        // Stores the current camera's up vector.
        m_CachedCameraUp = Camera.main.transform.TransformDirection(Vector3.up);
        StereoPhotoCamera.transform.SetPositionAndRotation(
          m_CachedCameraPosition, Camera.main.transform.rotation);

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
                Vector3 v = new Vector3(x * 0.01f, -y * 0.01f, 0) + k_DefaultMeshOffset;
                vertices.Add(v);
                normals.Add(Vector3.back);
            }
        }

        // Creates template triangle list.
        int[] triangles = GenerateTriangles(DepthSource.DepthWidth, DepthSource.DepthHeight);

        // Creates the mesh object and set all template data.
        m_Mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        m_Mesh.SetVertices(vertices);
        m_Mesh.SetNormals(normals);
        m_Mesh.SetTriangles(triangles, 0);
        m_Mesh.bounds = new Bounds(Vector3.zero, new Vector3(50, 50, 50));
        m_Mesh.UploadMeshData(true);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = m_Mesh;

        // Sets camera intrinsics for depth reprojection.
        Material material = GetComponent<Renderer>().material;
        material.SetTexture("_CurrentDepthTexture", DepthSource.DepthTexture);
        material.SetFloat("_FocalLengthX", DepthSource.FocalLength.x);
        material.SetFloat("_FocalLengthY", DepthSource.FocalLength.y);
        material.SetFloat("_PrincipalPointX", DepthSource.PrincipalPoint.x);
        material.SetFloat("_PrincipalPointY", DepthSource.PrincipalPoint.y);
        material.SetInt("_ImageDimensionsX", DepthSource.ImageDimensions.x);
        material.SetInt("_ImageDimensionsY", DepthSource.ImageDimensions.y);
        material.SetFloat("_TriangleConnectivityCutOff", k_TriangleConnectivityCutOff);

        m_Initialized = true;
    }

    private void Update()
    {
        var mesh_renderer = GetComponent<Renderer>();

        if (!m_FreezeMesh)
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
                  k_VertexModelTransformPropertyName, DepthSource.LocalToWorldMatrix);
                material.SetMatrix(
                  k_CameraViewMatrixPropertyName, Camera.main.worldToCameraMatrix);
                material.SetMatrix(
                  k_TextureProjectionMatrixPropertyName, Camera.main.projectionMatrix);
                mesh_renderer.enabled = true;
            }
        }

        if (!m_Initialized && DepthSource.Initialized)
        {
            InitializeMesh();
        }

        if (mesh_renderer.enabled)
        {
            // Computes the parametric equation of the camera path.
            float theta = Time.realtimeSinceStartup * k_CameraRotationSpeed;
            StereoPhotoCamera.transform.position = m_CachedCameraPosition
                + (m_CameraRotationRadius *
                ((Mathf.Cos(theta) * m_CachedCameraPlaneAxisA) +
                 (Mathf.Sin(theta) * m_CachedCameraPlaneAxisB)));
            StereoPhotoCamera.transform.LookAt(m_CachedTargetPosition, m_CachedCameraUp);

            // Updates the projection matrix for correct uv coordinates.
            Material material = mesh_renderer.material;
            var inverseModelViewMatrix = Matrix4x4.Inverse(
              StereoPhotoCamera.worldToCameraMatrix * m_CachedModeMatrix);
            var projectionMatrix = StereoPhotoCamera.projectionMatrix *
              m_CachedModelViewMatrix * inverseModelViewMatrix;
            material.SetMatrix(k_TextureProjectionMatrixPropertyName, projectionMatrix);
        }
    }
}
