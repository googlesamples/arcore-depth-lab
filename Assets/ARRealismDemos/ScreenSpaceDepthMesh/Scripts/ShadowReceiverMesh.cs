//-----------------------------------------------------------------------
// <copyright file="ShadowReceiverMesh.cs" company="Google LLC">
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

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Casts shadow of the attached mesh.
/// </summary>
public class ShadowReceiverMesh : MonoBehaviour
{
    /// <summary>
    /// Lower bound of mesh distance to cast shadow.
    /// </summary>
    public float MinimumMeshDistance = _minimumMeshDistance;

    /// <summary>
    /// Higher bound of mesh distance to cast shadow.
    /// </summary>
    public float MaximumMeshDistance = _maximumMeshDistance;

    private const float _minimumMeshDistance = 0;
    private const float _maximumMeshDistance = 1000;

    private static readonly Vector3 _defaultMeshOffset = new Vector3(-100, -100, -100);
    private static readonly string _vertexModelTransformPropertyName = "_VertexModelTransform";

    // Holds the vertex and index data of the depth template mesh.
    private Mesh _mesh;

    private bool _initialized = false;

    private Material _material;

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
        _mesh = new Mesh();
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _mesh.SetVertices(vertices);
        _mesh.SetNormals(normals);
        _mesh.SetTriangles(triangles, 0);
        _mesh.bounds = new Bounds(Vector3.zero, new Vector3(50, 50, 50));
        _mesh.UploadMeshData(true);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = _mesh;

        // Set camera intrinsics for depth reprojection.
        _material.SetFloat("_FocalLengthX", DepthSource.FocalLength.x);
        _material.SetFloat("_FocalLengthY", DepthSource.FocalLength.y);
        _material.SetFloat("_PrincipalPointX", DepthSource.PrincipalPoint.x);
        _material.SetFloat("_PrincipalPointY", DepthSource.PrincipalPoint.y);
        _material.SetInt("_ImageDimensionsX", DepthSource.ImageDimensions.x);
        _material.SetInt("_ImageDimensionsY", DepthSource.ImageDimensions.y);

        _initialized = true;
    }

    private void Start()
    {
        // Removes any legacy manual mesh rotation to work for portrait mode phone.
        transform.localRotation = Quaternion.identity;

        // Assigns the texture to the material.
        _material = GetComponent<Renderer>().material;
        UpdateShaderVariables();
    }

    private void Update()
    {
        UpdateShaderVariables();

        if (!_initialized && DepthSource.Initialized)
        {
            InitializeMesh();
        }
    }

    private void UpdateShaderVariables()
    {
        _material.SetFloat("_MinimumMeshDistance", MinimumMeshDistance);
        _material.SetFloat("_MaximumMeshDistance", MaximumMeshDistance);
        _material.SetMatrix(_vertexModelTransformPropertyName, DepthSource.LocalToWorldMatrix);
        _material.SetTexture("_CurrentDepthTexture", DepthSource.DepthTexture);
    }
}
