//-----------------------------------------------------------------------
// <copyright file="WaterGridManager.cs" company="Google LLC">
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
using GoogleARCoreInternal;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the depth-aware water effect layer.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class WaterGridManager : MonoBehaviour
{
    /// <summary>
    /// Water depth in meters.
    /// </summary>
    public float WaterDepthInM = 0.4f;

    /// <summary>
    /// Plane X dimension.
    /// </summary>
    public int Xdim = 250;

    /// <summary>
    /// Plane Y dimension.
    /// </summary>
    public int Ydim = 250;

    /// <summary>
    /// Plane cell size in m.
    /// </summary>
    public float CellSizeInM = 0.1f;

    /// <summary>
    /// Wave speed.
    /// </summary>
    public float WaveSpeed = 0.00025f;

    /// <summary>
    /// Wave height intensity.
    /// </summary>
    public float WaveIntensity = 0.025f;

    /// <summary>
    /// Material for rendering the flooding effects.
    /// </summary>
    public Material WaterMaterial = null;

    private float _waterLevel;
    private Vector3[] _vertices;
    private Vector3[] _normals;
    private Color[] _colors;
    private MeshFilter _meshFilter;
    private Mesh _mesh;

    private void GenerateWater()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshFilter.sharedMesh = null;

        _mesh = new Mesh();
        _mesh.name = "Water";

        _vertices = new Vector3[(Xdim + 1) * (Ydim + 1)];
        _normals = new Vector3[_vertices.Length];
        Vector2[] uv = new Vector2[_vertices.Length];

        UpdateWater();
        for (int i = 0, y = 0; y <= Ydim; y++)
        {
            for (int x = 0; x <= Xdim; x++, i++)
            {
                uv[i] = new Vector2((float)x / Xdim, (float)y / Ydim);
            }
        }

        int[] triangles = new int[Xdim * Ydim * 6];
        for (int ti = 0, vi = 0, y = 0; y < Ydim; y++, vi++)
        {
            for (int x = 0; x < Xdim; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + Xdim + 1;
                triangles[ti + 5] = vi + Xdim + 2;
            }
        }

        _mesh.uv = uv;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
        _mesh.RecalculateTangents();

        _meshFilter.sharedMesh = _mesh;

        if (WaterMaterial != null)
        {
            WaterMaterial.SetFloat("_ShowColorOnly", 0);
        }
    }

    private void UpdateWater()
    {
        Vector3 center = new Vector3(Xdim * CellSizeInM * 0.5f, 0.0f, Ydim * CellSizeInM * 0.5f);
        _colors = new Color[_vertices.Length];

        for (int i = 0, y = 0; y <= Ydim; y++)
        {
            for (int x = 0; x <= Xdim; x++, i++)
            {
                Random.InitState(i + 123);
                float rndWaveSpeed = Random.Range(0.0001f, WaveSpeed);
                float rndWaveIntensity = Random.Range(0.0001f, WaveIntensity);
                float up = (Mathf.Sin(Time.time * rndWaveSpeed * i) * rndWaveIntensity) +
                    (WaveIntensity * 0.5f);
                _vertices[i] = new Vector3(x * CellSizeInM, up, y * CellSizeInM) - center;
                _normals[i] = Vector3.up;
                _colors[i] = new Color(up / WaveIntensity, up / WaveIntensity, up / WaveIntensity);
            }
        }

        _mesh.vertices = _vertices;
        _mesh.colors = _colors;
    }

    private void Start()
    {
        _waterLevel = -0.5f;
        GenerateWater();
        UpdateWater();
    }

    private void Update()
    {
        UpdateWater();

        if (InstantPreviewManager.IsProvidingPlatform == false)
        {
            _waterLevel = ARCorePlaneUtil.Instance.GetLowestPlaneY() + WaterDepthInM;
            transform.position = new Vector3(0.0f, _waterLevel, 0.0f);
        }
    }

    private void OnDestroy()
    {
        if (WaterMaterial != null)
        {
            WaterMaterial.SetFloat("_ShowColorOnly", 1f);
        }
    }
}
