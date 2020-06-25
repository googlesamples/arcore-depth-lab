//-----------------------------------------------------------------------
// <copyright file="SplatMeshFromDepth.cs" company="Google LLC">
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
using UnityEngine;

/// <summary>
/// Generate a small mesh from the depth map on the current location.
/// Uv's will be planar projected from the current camera observation point.
/// </summary>
public class SplatMeshFromDepth : MonoBehaviour
{
    /// <summary>
    /// The number of vertices in width.
    /// </summary>
    public int Width = 10;

    /// <summary>
    /// The number of vertices in height.
    /// </summary>
    public int Height = 10;

    private void Start()
    {
#if UNITY_EDITOR
#else
        if (!DepthSource.Initialized)
        {
            return;
        }
#endif
        Rect bb = GetViewAlignedProjectedBoundingBox();
        CreateMesh(bb);
    }

    private Rect GetViewAlignedProjectedBoundingBox()
    {
        Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;

        Vector3 world_bounds = new Vector3(0.1f, 0.1f, 0.1f);

        Vector3 FrontBottomLeft = new Vector3(-world_bounds.x, 0.0f, -world_bounds.z);
        Vector3 FrontBottomRight = new Vector3(+world_bounds.x, 0.0f, -world_bounds.z);
        Vector3 BackBottomLeft = new Vector3(-world_bounds.x, 0.0f, +world_bounds.z);
        Vector3 BackBottomRight = new Vector3(+world_bounds.x, 0.0f, +world_bounds.z);

        FrontBottomLeft = transform.TransformPoint(FrontBottomLeft);
        FrontBottomRight = transform.TransformPoint(FrontBottomRight);
        BackBottomLeft = transform.TransformPoint(BackBottomLeft);
        BackBottomRight = transform.TransformPoint(BackBottomRight);

        Vector3 max = Vector3.negativeInfinity;
        Vector3 min = Vector3.positiveInfinity;

        Vector3 screen = Camera.main.WorldToScreenPoint(FrontBottomLeft);
        max = new Vector3(Mathf.Max(max.x, screen.x),
                    Mathf.Max(max.y, screen.y), Mathf.Max(max.z, screen.z));
        min = new Vector3(Mathf.Min(min.x, screen.x),
                    Mathf.Min(min.y, screen.y), Mathf.Min(min.z, screen.z));

        screen = Camera.main.WorldToScreenPoint(FrontBottomRight);
        max = new Vector3(Mathf.Max(max.x, screen.x),
                    Mathf.Max(max.y, screen.y), Mathf.Max(max.z, screen.z));
        min = new Vector3(Mathf.Min(min.x, screen.x),
                    Mathf.Min(min.y, screen.y), Mathf.Min(min.z, screen.z));

        screen = Camera.main.WorldToScreenPoint(BackBottomLeft);
        max = new Vector3(Mathf.Max(max.x, screen.x),
                    Mathf.Max(max.y, screen.y), Mathf.Max(max.z, screen.z));
        min = new Vector3(Mathf.Min(min.x, screen.x),
                    Mathf.Min(min.y, screen.y), Mathf.Min(min.z, screen.z));

        screen = Camera.main.WorldToScreenPoint(BackBottomRight);
        max = new Vector3(Mathf.Max(max.x, screen.x),
                    Mathf.Max(max.y, screen.y), Mathf.Max(max.z, screen.z));
        min = new Vector3(Mathf.Min(min.x, screen.x),
                    Mathf.Min(min.y, screen.y), Mathf.Min(min.z, screen.z));

        //// Round to int, since we are using this as pixel indices.
        max = new Vector3((int)max.x, (int)max.y, (int)max.z);
        min = new Vector3((int)min.x, (int)min.y, (int)min.z);
        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    private void CreateMesh(Rect ScreenSpaceWindow)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        int step_x = (int)(ScreenSpaceWindow.width / Width);
        int step_y = (int)(ScreenSpaceWindow.height / Height);

        int start_point_x = (int)ScreenSpaceWindow.x;
        int start_point_y = (int)ScreenSpaceWindow.y;

        float inverseWidth = 1.0f / (Width - 1);
        float inverseHeight = 1.0f / (Height - 1);

        for (int y = Height - 1; y >= 0; --y)
        {
            for (int x = 0; x < Width; ++x)
            {
                Vector3 vertex = Vector3.zero;
                Vector2Int screenPoint = new Vector2Int(start_point_x + (x * step_x),
                                                        start_point_y + (y * step_y));
#if UNITY_EDITOR
                Ray ray = Camera.main.ScreenPointToRay(
                      new Vector3(screenPoint.x, screenPoint.y, 1.0f));
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    float depthM = hit.distance;
                    vertex = Camera.main.ScreenToWorldPoint(
                                              new Vector3(screenPoint.x, screenPoint.y, depthM));
                }
#else
                short[] depthMap = DepthSource.DepthArray;
                Vector2Int depthPoint = DepthSource.ScreenToDepthXY(screenPoint.x, screenPoint.y);
                float depth_m = DepthSource.GetDepthFromXY(depthPoint.x, depthPoint.y, depthMap);
                vertex = DepthSource.ComputeVertex(depthPoint.x, depthPoint.y, depth_m);
                vertex = DepthSource.TransformVertexToWorldSpace(vertex);
#endif
                Vector2 uv = new Vector2(x * inverseWidth, y * inverseHeight);
                uvs.Add(uv);
                vertices.Add(vertex - transform.position);
            }
        }

        if (vertices.Count > 0)
        {
            int[] triangles = GenerateTriangles(Width, Height);

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.UploadMeshData(false);

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;

            transform.rotation = Quaternion.identity;
        }
    }

    private int[] GenerateTriangles(int width, int height)
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
}
