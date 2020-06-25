//-----------------------------------------------------------------------
// <copyright file="Utilities.cs" company="Google LLC">
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// General Utilities for ARRealism.
/// </summary>
public static class Utilities
{
    /// <summary>
    /// Vector2 * Vector2 - component-wise multiplication support for legacy Unity versions.
    /// </summary>
    /// <param name="v1">First vector factor.</param>
    /// <param name="v2">Second vector factor.</param>
    /// <returns>Contains component-wise multiplied vector.</returns>
    public static Vector2 MultiplyVector2(Vector2 v1, Vector2 v2)
    {
        return new Vector2(v1.x * v2.x, v1.y * v2.y);
    }

    /// <summary>
    /// Gets the first two components of the vector.
    /// </summary>
    /// <param name="v">Current vector.</param>
    /// <returns>The first two components of the vector.</returns>
    public static Vector2 GetXY(this Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }

    /// <summary>
    /// Gets all vertices of a mesh's bounding box in the local coordinate system.
    /// </summary>
    /// <param name="targetObject">The target object.</param>
    /// <returns>A list of vertices local positions in Vector3.</returns>
    public static Vector3[] GetBoundingBoxVertices(GameObject targetObject)
    {
        var meshFilters = targetObject.GetComponentsInChildren<MeshFilter>();
        var vertices = new List<Vector3>();
        Bounds box = new Bounds();
        foreach (var meshFilter in meshFilters)
        {
            var bounds = meshFilter.mesh.bounds;
            box.Expand(bounds.min);
            box.Expand(bounds.max);
        }

        // Adds the 8 bounding box vertices into the list.
        vertices.Add(box.min);
        vertices.Add(box.max);
        vertices.Add(new Vector3(box.min.x, box.min.y, box.max.z));
        vertices.Add(new Vector3(box.min.x, box.max.y, box.min.z));
        vertices.Add(new Vector3(box.max.x, box.min.y, box.min.z));
        vertices.Add(new Vector3(box.min.x, box.max.y, box.max.z));
        vertices.Add(new Vector3(box.max.x, box.min.y, box.max.z));
        vertices.Add(new Vector3(box.max.x, box.max.y, box.min.z));
        return vertices.ToArray();
    }

    /// <summary>
    /// Gets all vertices of a mesh's bounding box in the local coordinate system.
    /// </summary>
    /// <param name="origin">The origin point.</param>
    /// <param name="dimensions">The dimensions to expand in the world space.</param>
    /// <returns>A list of vertices local positions in Vector3.</returns>
    public static Vector3[] GetBoundingBoxVertices(Vector3 origin, Vector3 dimensions)
    {
        var box_min = origin - (dimensions * 0.5f);
        var box_max = origin + (dimensions * 0.5f);
        var vertices = new List<Vector3>();
        vertices.Add(box_min);
        vertices.Add(box_max);
        vertices.Add(new Vector3(box_min.x, box_min.y, box_max.z));
        vertices.Add(new Vector3(box_min.x, box_max.y, box_min.z));
        vertices.Add(new Vector3(box_max.x, box_min.y, box_min.z));
        vertices.Add(new Vector3(box_min.x, box_max.y, box_max.z));
        vertices.Add(new Vector3(box_max.x, box_min.y, box_max.z));
        vertices.Add(new Vector3(box_max.x, box_max.y, box_min.z));
        return vertices.ToArray();
    }

    /// <summary>
    /// Gets the screen point from a world point.
    /// </summary>
    /// <param name="worldPoint">The world position.</param>
    /// <returns>The screen position.</returns>
    public static Vector2Int WorldPointToScreenPoint(Vector3 worldPoint)
    {
        var screenPoint = Camera.main.WorldToScreenPoint(worldPoint);
        return new Vector2Int((int)screenPoint.x, Screen.height - (int)screenPoint.y);
    }

    /// <summary>
    /// Checks if a game object is in camera view.
    /// </summary>
    /// <param name="sourceObject">The object to be checked.</param>
    /// <returns>True if the object is in camera view.</returns>
    public static bool IsObjectInCameraView(GameObject sourceObject)
    {
        bool isObjectInView = false;
        if (sourceObject != null)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            Collider collider = sourceObject.GetComponent<Collider>();
            if (collider == null)
            {
                collider = sourceObject.transform.GetChild(0).GetComponent<Collider>();
            }

            isObjectInView = GeometryUtility.TestPlanesAABB(planes, collider.bounds);
        }

        return isObjectInView;
    }

    /// <summary>
    /// Gets the bounding rectangle of a game object.
    /// </summary>
    /// <param name="sourceObject">The object to be bounded.</param>
    /// <returns>The rectangle which contains the source object.</returns>
    public static RectInt GetScreenBoundingRectInt(GameObject sourceObject)
    {
        var renderer = sourceObject.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return new RectInt(0, 0, 0, 0);
        }

        var bounds = renderer.bounds;
        var origin = bounds.center;
        var dimension = bounds.size;
        var vertices = GetBoundingBoxVertices(origin, dimension);
        var screenPoints = new List<Vector2Int>();
        foreach (var vertex in vertices)
        {
            screenPoints.Add(WorldPointToScreenPoint(vertex));
        }

        var rectMin = screenPoints[0];
        var rectMax = screenPoints[0];
        foreach (var point in screenPoints)
        {
            rectMin = Vector2Int.Min(rectMin, point);
            rectMax = Vector2Int.Max(rectMax, point);
        }

        // Clamps to the valid screen coordinate system.
        var xMin = Math.Max(Math.Min(rectMin.x, Screen.width), 0);
        var yMin = Math.Max(Math.Min(rectMin.y, Screen.height), 0);
        var xMax = Math.Max(Math.Min(rectMax.x, Screen.width), 0);
        var yMax = Math.Max(Math.Min(rectMax.y, Screen.height), 0);

        var width = xMax - xMin;
        var height = yMax - yMin;

        return new RectInt(xMin, yMin, width, height);
    }

    /// <summary>
    /// Component-wise rounds up Vector3.
    /// </summary>
    /// <param name="vector">Vector3 input.</param>
    /// <param name="numDecimals">Number of decimals to preserve.</param>
    /// <returns>A new Vector3.</returns>
    public static Vector3 Round(this Vector3 vector, int numDecimals = 0)
    {
        float multiplier = Mathf.Pow(10f, (float)numDecimals);

        return new Vector3(
            Mathf.Round(vector.x * multiplier) / multiplier,
            Mathf.Round(vector.y * multiplier) / multiplier,
            Mathf.Round(vector.z * multiplier) / multiplier);
    }
}
