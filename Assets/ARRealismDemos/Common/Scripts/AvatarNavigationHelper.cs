//-----------------------------------------------------------------------
// <copyright file="AvatarNavigationHelper.cs" company="Google LLC">
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
/// This class contains a collection of helper functions for simple avatar navigation using depth.
/// </summary>
public class AvatarNavigationHelper
{
    /// <summary>
    /// Raycasts into the depth map from a start point to an end point.
    /// </summary>
    /// <param name="depthArray">CPU depth array to raycast into.</param>
    /// <param name="start">Start point of the raycast operation.</param>
    /// <param name="end">End point of the raycast operation.</param>
    /// <param name="step">The step size of the raycast operation.</param>
    /// <returns>Returns the hit point of raycast, negative inifinity for no hit.</returns>
    public static Vector3 RaycastDepth(short[] depthArray, Vector3 start, Vector3 end, float step)
    {
        Vector3 vector = end - start;
        Vector3 direction = vector.normalized;
        float length = vector.magnitude;
        float squareLength = vector.sqrMagnitude;

        Vector3 intermediatePoint = start;
        Vector3 stepVector = step * direction;

        Vector3 hitPoint = Vector3.negativeInfinity;

        int stepCount = 0;
        while ((intermediatePoint - start).sqrMagnitude <= squareLength)
        {
            intermediatePoint = start + (stepCount * stepVector);
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(intermediatePoint);
            Vector2Int depthXY = DepthSource.ScreenToDepthXY(
                (int)screenPoint.x, (int)screenPoint.y);

            float realDepth = DepthSource.GetDepthFromXY(depthXY.x, depthXY.y, depthArray);

            // Detects a hit if the ray lands farther than the depth from the depth map.
            if ((realDepth != 0) && (screenPoint.z >= realDepth))
            {
                hitPoint = intermediatePoint;
                break;
            }

            stepCount++;
        }

        return hitPoint;
    }
}
