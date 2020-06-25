//-----------------------------------------------------------------------
// <copyright file="DepthPulseEffect.cs" company="Google LLC">
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
/// Add comment here.
/// </summary>
public class DepthPulseEffect : MonoBehaviour
{
    /// <summary>
    /// Add comment here.
    /// </summary>
    public float StartFadingDepth = k_StartFadingDepth;

    /// <summary>
    /// Add comment here.
    /// </summary>
    public float MaximumPulseDepth = k_MaximumPulseDepth;

    /// <summary>
    /// Add comment here.
    /// </summary>
    public float PulseDurationS = k_PulseDurationS;

    /// <summary>
    /// Add comment here.
    /// </summary>
    public float PulseWidthM = k_PulseWidthM;

    /// <summary>
    /// Add comment here.
    /// </summary>
    public ShadowReceiverMesh ShadowReceiver;

    // A small default texture size to create a texture of unknown size.
    private const int k_DefaultTextureSize = 2;
    private const float k_MaximumPulseDepth = 7;
    private const float k_StartFadingDepth = 6;
    private const float k_PulseDurationS = 5;
    private const float k_PulseWidthM = 0.4f;

    private static readonly Vector3 k_DefaultMeshOffset = new Vector3(-100, -100, -100);

    // Holds the vertex and index data of the depth template mesh.
    private Mesh m_Mesh;

    // Holds the calibrated camera's intrinsic parameters.
    private CameraIntrinsics m_Intrinsics;

    // This is the scale vector to appropriately scale the camera intrinsics to the depth texture.
    private Vector2 m_IntrinsicsScale;
    private Texture2D m_DepthTexture;
    private bool m_Initialized = false;

    private Material m_Material;
    private float m_PulseDepth = 0;
    private Coroutine m_CurrentCoroutine;
    private Matrix4x4 m_ScreenRotation = Matrix4x4.Rotate(Quaternion.identity);

    /// <summary>
    /// Add comment here.
    /// </summary>
    public void StartPulseToCamera()
    {
        if (m_CurrentCoroutine != null)
        {
            StopCoroutine(m_CurrentCoroutine);
        }

        m_CurrentCoroutine = StartCoroutine(AnimateValue(0, PulseDurationS));
    }

    /// <summary>
    /// Add comment here.
    /// </summary>
    public void StartPulseToHorizon()
    {
        if (m_CurrentCoroutine != null)
        {
            StopCoroutine(m_CurrentCoroutine);
        }

        m_CurrentCoroutine = StartCoroutine(
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
        m_Intrinsics = Frame.CameraImage.TextureIntrinsics;

        // Scale camera intrinsics to the depth map size.
        m_IntrinsicsScale.x = m_DepthTexture.width / (float)m_Intrinsics.ImageDimensions.x;
        m_IntrinsicsScale.y = m_DepthTexture.height / (float)m_Intrinsics.ImageDimensions.y;

        // Create template vertices.
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        // Create template vertices for the mesh object.
        for (int y = 0; y < m_DepthTexture.height; y++)
        {
            for (int x = 0; x < m_DepthTexture.width; x++)
            {
                Vector3 v = new Vector3(x * 0.01f, -y * 0.01f, 0) + k_DefaultMeshOffset;
                vertices.Add(v);
                normals.Add(Vector3.back);
            }
        }

        // Create template triangle list.
        int[] triangles = GenerateTriangles(m_DepthTexture.width, m_DepthTexture.height);

        // Create the mesh object and set all template data.
        m_Mesh = new Mesh();
        m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_Mesh.SetVertices(vertices);
        m_Mesh.SetNormals(normals);
        m_Mesh.SetTriangles(triangles, 0);
        m_Mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000));
        m_Mesh.UploadMeshData(true);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = m_Mesh;

        float principalPointX = m_Intrinsics.PrincipalPoint.x * m_IntrinsicsScale.x;
        float principalPointY = m_Intrinsics.PrincipalPoint.y * m_IntrinsicsScale.y;

        // Set camera intrinsics for depth reprojection.
        m_Material.SetFloat("_FocalLengthX", m_Intrinsics.FocalLength.x * m_IntrinsicsScale.x);
        m_Material.SetFloat("_FocalLengthY", m_Intrinsics.FocalLength.y * m_IntrinsicsScale.y);
        m_Material.SetFloat("_PrincipalPointX", principalPointX);
        m_Material.SetFloat("_PrincipalPointY", principalPointY);
        m_Material.SetInt("_ImageDimensionsX", m_DepthTexture.width);
        m_Material.SetInt("_ImageDimensionsY", m_DepthTexture.height);

        m_Initialized = true;
    }

    private void Start()
    {
        // Default texture, will be updated each frame.
        m_DepthTexture = new Texture2D(k_DefaultTextureSize, k_DefaultTextureSize);

        // Assign the texture to the material.
        m_Material = GetComponent<Renderer>().material;
        m_Material.SetTexture("_CurrentDepthTexture", m_DepthTexture);
        UpdateScreenOrientation();
    }

    private void Update()
    {
        // Get the latest depth data from ARCore.
        Frame.CameraImage.UpdateDepthTexture(ref m_DepthTexture);
        UpdateShaderVariables();
        UpdateScreenOrientation();

        if (!m_Initialized && m_DepthTexture.width != k_DefaultTextureSize
            && m_DepthTexture.height != k_DefaultTextureSize)
        {
            InitializeMesh();
        }
    }

    private void UpdateShaderVariables()
    {
        m_Material.SetFloat("_PulseWidth", PulseWidthM);
        m_Material.SetFloat("_PulseDepth", m_PulseDepth);
        m_Material.SetFloat("_MaximumPulseDepth", MaximumPulseDepth);
        m_Material.SetFloat("_StartFadingDepth", StartFadingDepth);
        ShadowReceiver.MaximumMeshDistance = m_PulseDepth;
    }

    private IEnumerator AnimateValue(float targetValue, float animationTime)
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = true;

        float originalPulseDepth = m_PulseDepth;
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / animationTime)
        {
            m_PulseDepth = Mathf.Lerp(originalPulseDepth, targetValue, t);
            yield return null;
        }

        m_PulseDepth = targetValue;

        meshRenderer.enabled = false;
    }

    private void UpdateScreenOrientation()
    {
        switch (Screen.orientation)
        {
            case ScreenOrientation.Portrait:
                m_ScreenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90));
                break;
            case ScreenOrientation.LandscapeLeft:
                m_ScreenRotation = Matrix4x4.Rotate(Quaternion.identity);
                break;
            case ScreenOrientation.PortraitUpsideDown:
                m_ScreenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, -90));
                break;
            case ScreenOrientation.LandscapeRight:
                m_ScreenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 180));
                break;
        }

        m_Material.SetMatrix("_ScreenRotation", m_ScreenRotation);
    }
}
