//-----------------------------------------------------------------------
// <copyright file="Particles.cs" company="Google LLC">
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
/// Part of a GPU particle implementation.
/// This class generates the particle's geometry, represented by a bunch of quads, one for each
/// pixel in the textures that represent the particles state.
/// </summary>
public class Particles : MonoBehaviour
{
    /// <summary>
    /// Particle size in world space.
    /// </summary>
    public float ParticleSize = 0.5f;

    /// <summary>
    /// Dimension of the particle texture (must be a square texture).
    /// </summary>
    public int ParticleTextureSize = 256;

    private void Start()
    {
        int particleCount = ParticleTextureSize * ParticleTextureSize;

        // Initializes the particle triangles.
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        Vector3[] vertices = new Vector3[particleCount * 4];
        Vector3[] normals = new Vector3[particleCount * 4];
        Vector2[] uv = new Vector2[particleCount * 4];
        int[] triangleIndices = new int[particleCount * 6];

        float vertexXPos = -(ParticleSize * 0.5f);
        float vertexYPos = vertexXPos;
        float uvScale = ParticleTextureSize;

        int xIndex = 0;
        int yIndex = 0;

        for (int i = 0; i < particleCount; ++i)
        {
            int bufferIndex = i * 4;
            int triIndex = i * 6;

            vertices[bufferIndex + 0] = new Vector3(vertexXPos, vertexYPos, 0);
            vertices[bufferIndex + 1] = new Vector3(vertexXPos + ParticleSize, vertexYPos, 0);
            vertices[bufferIndex + 2] = new Vector3(vertexXPos, vertexYPos + ParticleSize, 0);
            vertices[bufferIndex + 3] = new Vector3(vertexXPos + ParticleSize,
              vertexYPos + ParticleSize, 0);

            normals[bufferIndex + 0] = -Vector3.forward;
            normals[bufferIndex + 1] = -Vector3.forward;
            normals[bufferIndex + 2] = -Vector3.forward;
            normals[bufferIndex + 3] = -Vector3.forward;

            Vector2 uvCoord = new Vector2(xIndex / uvScale, yIndex / uvScale);

            uv[bufferIndex + 0] = uvCoord;
            uv[bufferIndex + 1] = uvCoord;
            uv[bufferIndex + 2] = uvCoord;
            uv[bufferIndex + 3] = uvCoord;

            triangleIndices[triIndex + 0] = bufferIndex + 0;
            triangleIndices[triIndex + 1] = bufferIndex + 2;
            triangleIndices[triIndex + 2] = bufferIndex + 1;

            triangleIndices[triIndex + 3] = bufferIndex + 2;
            triangleIndices[triIndex + 4] = bufferIndex + 3;
            triangleIndices[triIndex + 5] = bufferIndex + 1;

            ++xIndex;
            if ((i + 1) % ParticleTextureSize == 0)
            {
                xIndex = 0;
                ++yIndex;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangleIndices;
        mesh.normals = normals;
        mesh.uv = uv;

        // Sets large mesh bounds so particles don't get culled.
        Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(10000, 10000, 10000));
        mesh.bounds = bounds;
    }

    private void Update()
    {
        Camera ParticleRenderingCamera = Camera.main;
        Matrix4x4 viewMatrix = ParticleRenderingCamera.worldToCameraMatrix;
        Matrix4x4 viewProjectionMatrix = ParticleRenderingCamera.projectionMatrix *
          ParticleRenderingCamera.worldToCameraMatrix;
        Shader.SetGlobalMatrix("_ParticleCameraViewMatrix", viewMatrix);
        Shader.SetGlobalMatrix("_ParticleCameraViewProjectionMatrix", viewProjectionMatrix);
        Shader.SetGlobalFloat("_ParticleCameraNear", ParticleRenderingCamera.nearClipPlane);
        Shader.SetGlobalFloat("_ParticleCameraFar", ParticleRenderingCamera.farClipPlane);

        Vector4 depthIntrinsics = new Vector4(DepthSource.ImageDimensions.x,
                                DepthSource.ImageDimensions.y,
                                DepthSource.FocalLength.x,
                                DepthSource.FocalLength.y);
        Shader.SetGlobalVector("_DepthIntrinsics", depthIntrinsics);
    }
}
