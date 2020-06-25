//-----------------------------------------------------------------------
// <copyright file="SimpleCollisionHelper.cs" company="Google LLC">
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
using UnityEngine.Serialization;

/// <summary>
/// Simple class that allows for lightweight querying of wether a gameObject will collide
/// at a given transform.
/// </summary>
public class SimpleCollisionHelper
{
    private const float k_VertexCollisionThresholdInMeters = 0.12f;

    private static Vector3[] s_Offsets =
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(-1.0f, -1.0f, -1.0f),
        new Vector3(1.0f, -1.0f, -1.0f),
        new Vector3(1.0f, -1.0f, 1.0f),
        new Vector3(-1.0f, -1.0f, 1.0f),
        new Vector3(-1.0f, 0.0f, -1.0f),
        new Vector3(1.0f, 0.0f, -1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(-1.0f, 0.0f, 1.0f),
        new Vector3(-1.0f, 1.0f, -1.0f),
        new Vector3(1.0f, 1.0f, -1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(-1.0f, 1.0f, 1.0f),
    };

    /// <summary>
    /// Tests if a gameObject with a given transform is behind the physical environment.
    /// </summary>
    /// <param name="gameObject">The gameObject which we want to use to test collisions.</param>
    /// <param name="transform">The gameObject's transform where we want to check
    /// the collisions at.</param>
    /// <returns>Percentage of points colliding with the environment.</returns>
    public static float TestCollision(GameObject gameObject, Matrix4x4 transform)
    {
        Vector3[] vertexList = GetVertices(gameObject, transform);

        int numCollision = 0;
        float collisionPercentage = 0;
        int totalTests = vertexList.Length;

        // Tests every single vertex of the mesh and gets the statistics of the results.
        foreach (var vertex in vertexList)
        {
            var result = TestCollisionOnVertex(vertex);
            switch (result)
            {
                case CollisionResults.Collided:
                    ++numCollision;
                    break;
                case CollisionResults.InvalidDepth:
                    --totalTests;
                    break;
            }
        }

        // Reports no collision if the depth map is empty.
        if (totalTests <= 0)
        {
            return 0;
        }

        collisionPercentage = (float)numCollision / totalTests;
        return collisionPercentage;
    }

    private static Vector3[] GetVertices(GameObject gameObject, Matrix4x4 transform)
    {
        var collider = gameObject.GetComponent<BoxCollider>();

        Bounds bounds = new Bounds();
        bounds.Encapsulate(collider.center + (collider.size * 0.5f));
        bounds.Encapsulate(collider.center - (collider.size * 0.5f));

        Vector3[] box_vertices = new Vector3[s_Offsets.Length];
        for (int i = 0; i < s_Offsets.Length; ++i)
        {
            box_vertices[i] = transform.MultiplyPoint(
              bounds.center + Vector3.Scale(s_Offsets[i], bounds.extents));
        }

        return box_vertices;
    }

    private static CollisionResults TestCollisionOnVertex(Vector3 targetWorldPosition)
    {
        // Computes the environment's depth.
        var screenPosition = Camera.main.WorldToScreenPoint(targetWorldPosition);
        var screenUv = new Vector2(screenPosition.x / Screen.width,
                                   screenPosition.y / Screen.height);
        var depthUv = DepthSource.ScreenToDepthUV(screenUv);
        var environmentDepth = DepthSource.GetDepthFromUV(depthUv, DepthSource.DepthArray);

        if (environmentDepth == DepthSource.InvalidDepthValue)
        {
            return CollisionResults.InvalidDepth;
        }

        // Computes the virtual object's depth.
        var targetDepth = screenPosition.z;

        return targetDepth >
               environmentDepth + k_VertexCollisionThresholdInMeters
               ? CollisionResults.Collided : CollisionResults.NoCollision;
    }
}
