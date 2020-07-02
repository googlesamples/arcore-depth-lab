//-----------------------------------------------------------------------
// <copyright file="OrientedReticle.cs" company="Google LLC">
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
using UnityEngine;

/// <summary>
/// Orients the game object to match the normal estimated from the depth map.
/// </summary>
public class OrientedReticle : MonoBehaviour
{
    /// <summary>
    /// Orientation interpolation amount.
    /// If set to 0.0 no interpolation will occur.
    /// </summary>
    public float Interpolation = 0.75f;

    /// <summary>
    /// Screen space position of the reticle.
    /// </summary>
    public Vector2 ScreenPosition = new Vector2(0.5f, 0.5f);

    /// <summary>
    /// DepthSource instance in scene.
    /// </summary>
    public DepthSource DepthSource;

    /// <summary>
    /// Distance from the camera to the reticle.
    /// </summary>
    public float Distance = 1.0f;

    // Each valid neighbor is checked whether it is an inlier in depth.  Inliers
    // are defined as within a distance of (outlier_depth_ratio * depth) of a
    // point at 'depth' depth.
    private const float k_OutlierDepthRatio = 0.2f;

    // For each valid depth pixel, the neighborhood in a square window of radius
    // window_radius_pixels is searched.
    private const int k_WindowRadiusPixels = 2;

    private void Start()
    {
        if (DepthSource == null)
        {
            DepthSource = GetComponent<DepthSource>();
        }
    }

    private void Update()
    {
        try
        {
            float distance = ComputeCenterScreenDistance();

            if (distance > 0)
            {
                Vector3 translation = Camera.main.ScreenToWorldPoint(new Vector3(
                    Screen.width * ScreenPosition.x,
                    Screen.height * ScreenPosition.y,
                    distance));

                transform.position = translation;
            }

            Quaternion? orientation = ComputeCenterScreenOrientation();
            if (orientation != null)
            {
                transform.rotation = Quaternion.Slerp(orientation.Value,
                    transform.rotation, Interpolation);
            }
        }
        catch (InvalidOperationException)
        {
            // Intentional pitfall, depth values were invalid.
        }
    }

    private float ComputeCenterScreenDistance()
    {
        Vector2 depthMapPoint = ScreenPosition;

        short[] depthMap = DepthSource.DepthArray;
        float depth_m = DepthSource.GetDepthFromUV(depthMapPoint, depthMap);

        if (depth_m == DepthSource.InvalidDepthValue)
        {
            throw new InvalidOperationException("Invalid depth value");
        }

        Vector3 viewspace_point = DepthSource.ComputeVertex(depthMapPoint, depth_m);

        return viewspace_point.magnitude;
    }

    private Quaternion? ComputeCenterScreenOrientation()
    {
        Vector3 normal = ComputeNormalMapFromDepthWeightedMeanGradient(ScreenPosition);

        // Transforms normal to the world space.
        normal = Camera.main.transform.TransformDirection(normal);

        Vector3 right = Vector3.right;
        if (normal != Vector3.up)
        {
            right = Vector3.Cross(normal, Vector3.up);
        }

        Vector3 forward = Vector3.Cross(normal, right);
        Quaternion orientation = Quaternion.identity;
        orientation.SetLookRotation(forward, normal);

        return orientation;
    }

    /// <summary>
    /// Estimates the normal vector for each point based on weighted mean gradients
    /// on neighborhood depth data.
    /// </summary>
    /// <param name="screenUV">The normalized screen uv coordinates.</param>
    /// <returns>The computed normal.</returns>
    private Vector3 ComputeNormalMapFromDepthWeightedMeanGradient(Vector2 screenUV)
    {
        short[] depthMap = DepthSource.DepthArray;
        Vector2 depthUV = screenUV;
        Vector2Int depthXY = DepthSource.DepthUVtoXY(depthUV);
        float depth_m = DepthSource.GetDepthFromUV(depthUV, depthMap);

        if (depth_m == DepthSource.InvalidDepthValue)
        {
            throw new InvalidOperationException("Invalid depth value");
        }

        // Iterates over neighbors to compute normal vector.
        float neighbor_corr_x = 0.0f;
        float neighbor_corr_y = 0.0f;
        float outlier_distance_m = k_OutlierDepthRatio * depth_m;
        int radius = k_WindowRadiusPixels;
        float neighbor_sum_confidences_x = 0.0f;
        float neighbor_sum_confidences_y = 0.0f;
        for (int dy = -radius; dy <= radius; ++dy)
        {
            for (int dx = -radius; dx <= radius; ++dx)
            {
                // Self isn't a neighbor.
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                Vector2Int offset = new Vector2Int(dx, dy);
                int currentX = depthXY.x + offset.x;
                int currentY = depthXY.y + offset.y;

                // Retrieves neighbor value.
                float neighbor_depth_m = DepthSource.GetDepthFromXY(currentX, currentY, depthMap);

                // Confidence is not currently being packed yet, so for now this hardcoded.
                float neighbor_confidence = 1.0f;
                if (neighbor_depth_m == 0.0)
                {
                    continue;  // Neighbor does not exist.
                }

                float neighbor_distance_m = neighbor_depth_m - depth_m;

                // Checks for outliers.
                if (neighbor_confidence == 0.0f ||
                    Mathf.Abs(neighbor_distance_m) > outlier_distance_m)
                {
                    continue;
                }

                // Updates correlations in each dimension.
                if (dx != 0)
                {
                    neighbor_sum_confidences_x += neighbor_confidence;
                    neighbor_corr_x += neighbor_confidence * neighbor_distance_m / dx;
                }

                if (dy != 0)
                {
                    neighbor_sum_confidences_y += neighbor_confidence;
                    neighbor_corr_y += neighbor_confidence * neighbor_distance_m / dy;
                }
            }
        }

        if (neighbor_sum_confidences_x == 0 && neighbor_sum_confidences_y == 0)
        {
            throw new InvalidOperationException("Invalid confidence value.");
        }

        // Computes estimate of normal vector by finding weighted averages of
        // the surface gradient in x and y.
        float pixel_width_m = depth_m / DepthSource.FocalLength.x;
        float slope_x = neighbor_corr_x / (pixel_width_m * neighbor_sum_confidences_x);
        float slope_y = neighbor_corr_y / (pixel_width_m * neighbor_sum_confidences_y);

        // Negatives convert the normal to Unity's coordinate system.
        Vector3 normal = new Vector3(-slope_y, -slope_x, -1.0f);
        normal.Normalize();

        return normal;
    }
}
