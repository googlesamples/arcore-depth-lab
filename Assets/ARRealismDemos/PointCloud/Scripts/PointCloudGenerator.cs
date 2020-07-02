//-----------------------------------------------------------------------
// <copyright file="PointCloudGenerator.cs" company="Google LLC">
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
using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;

/// <summary>
/// Computes a point cloud from the depth map on the CPU.
/// </summary>
public class PointCloudGenerator : MonoBehaviour
{
    private const float k_MaxVisualizationDistanceM = 7;
    private const float k_MinVisualizationDistanceM = 0.4f;
    private bool m_Initialized;
    private CameraIntrinsics m_CameraIntrinsics;
    private Mesh m_Mesh;

    /// <summary>
    /// Computes 3D vertices from the depth map and creates a Mesh() object with the Point primitive
    /// type. Each point differently colored based on a depth color ramp.
    /// </summary>
    public void ComputePointCloud()
    {
        if (!m_Initialized)
        {
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        List<Color> colors = new List<Color>();
        int vertexCounter = 0;

        for (int y = 0; y < DepthSource.DepthHeight; y++)
        {
            for (int x = 0; x < DepthSource.DepthWidth; x++)
            {
                int depthIndex = (y * DepthSource.DepthWidth) + x;
                float depthInM = DepthSource.DepthArray[depthIndex] * DepthSource.MillimeterToMeter;

                Vector3 vertex = DepthSource.ComputeVertex(x, y, depthInM);
                if (vertex == Vector3.negativeInfinity)
                {
                    continue;
                }

                vertex = DepthSource.TransformVertexToWorldSpace(vertex);

                float depthRange = k_MaxVisualizationDistanceM - k_MinVisualizationDistanceM;
                float normalizedDepth = (depthInM - k_MinVisualizationDistanceM) / depthRange;
                Color color = ColorRampGenerator.Turbo(normalizedDepth);
                vertices.Add(vertex);
                indices.Add(vertexCounter++);
                colors.Add(color);
            }
        }

        m_Mesh.SetVertices(vertices);
        m_Mesh.SetIndices(indices.ToArray(), MeshTopology.Points, 0);
        m_Mesh.SetColors(colors);
        m_Mesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = m_Mesh;
    }

    private void Start()
    {
        m_Mesh = new Mesh();
        m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    private void Update()
    {
        // Waits until MotionStereo provides real data.
        if (!m_Initialized && DepthSource.Initialized)
        {
            m_Initialized = true;
        }
    }
}
