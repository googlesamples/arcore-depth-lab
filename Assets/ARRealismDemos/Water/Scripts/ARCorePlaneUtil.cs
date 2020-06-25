//-----------------------------------------------------------------------
// <copyright file="ARCorePlaneUtil.cs" company="Google LLC">
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using GoogleARCore;
using UnityEngine;

/// <summary>
/// Helper class for various high-level ARCore Plane features.
/// </summary>
public class ARCorePlaneUtil : Singleton<ARCorePlaneUtil>
{
    private List<DetectedPlane> m_AllPlanes = new List<DetectedPlane>();
    private List<DetectedPlane> m_UpdatedPlanes = new List<DetectedPlane>();
    private List<DetectedPlane> m_NewPlanes = new List<DetectedPlane>();
    private float m_LastTimestamp;

    /// <summary>
    /// Types of ARCore plane queries.
    /// </summary>
    public enum ARCorePlaneUtilQuery
    {
        /// <summary>
        /// Query all types of planes.
        /// </summary>
        All,

        /// <summary>
        /// Query new planes.
        /// </summary>
        New,

        /// <summary>
        /// Query updated planes.
        /// </summary>
        Updated
    }

    /// <summary>
    /// Gets the queried planes.
    /// </summary>
    /// <param name="whichQuery">Type of queried plane.</param>
    /// <returns>Returns a list of deteted planes.</returns>
    public List<DetectedPlane> GetPlanes(ARCorePlaneUtilQuery whichQuery)
    {
        // Conditionally collects new planes from ARCore.
        GetSessionPlanes();

        List<DetectedPlane> planes = new List<DetectedPlane>();

        switch (whichQuery)
        {
            case ARCorePlaneUtilQuery.All:
                planes = m_AllPlanes;
                break;
            case ARCorePlaneUtilQuery.New:
                planes = m_NewPlanes;
                break;
            case ARCorePlaneUtilQuery.Updated:
                planes = m_UpdatedPlanes;
                break;
        }

        return planes;
    }

    /// <summary>
    /// Gets the lowest plane.
    /// </summary>
    /// <param name="whichQuery">Type of queried plane.</param>
    /// <returns>Returns the lowest detected plane.</returns>
    public DetectedPlane GetLowestPlane(ARCorePlaneUtilQuery whichQuery)
    {
        List<DetectedPlane> planes = GetPlanes(whichQuery);

        var result = planes.OrderBy(a => a.CenterPose.position.y).ToArray();

        return result.Length > 0 ? result[0] : null;
    }

    /// <summary>
    /// Gets the Y value of the lowest plane.
    /// </summary>
    /// <returns>Returns the Y value of the lowest plane.</returns>
    public float GetLowestPlaneY()
    {
        return GetLowestPlaneY(ARCorePlaneUtilQuery.All);
    }

    /// <summary>
    /// Gets the Y value of the lowest plane.
    /// </summary>
    /// <param name="whichQuery">Type of queried plane.</param>
    /// <returns>Returns the Y value of the lowest plane.</returns>
    public float GetLowestPlaneY(ARCorePlaneUtilQuery whichQuery)
    {
        DetectedPlane lowestPlane = GetLowestPlane(whichQuery);

        float lowestY = lowestPlane != null ? lowestPlane.CenterPose.position.y : float.MaxValue;

        return lowestY;
    }

    private bool GetSessionPlanes()
    {
        bool foundPlanes = false;

        if (Mathf.Abs(m_LastTimestamp - Time.time) > float.Epsilon)
        {
            // Checks if the ARCore session is valid and running.
            if (Session.Status == SessionStatus.Tracking && Session.Status.IsValid())
            {
                // Gets new planes for this update.
                m_AllPlanes.Clear();
                m_UpdatedPlanes.Clear();
                m_NewPlanes.Clear();

                Session.GetTrackables<DetectedPlane>(m_AllPlanes);
                Session.GetTrackables<DetectedPlane>(m_NewPlanes, TrackableQueryFilter.New);
                Session.GetTrackables<DetectedPlane>(m_UpdatedPlanes, TrackableQueryFilter.Updated);
            }

            m_LastTimestamp = Time.time;
        }

        foundPlanes = m_UpdatedPlanes.Count > 0 ? true : false;
        return foundPlanes;
    }

    private void Awake()
    {
        this.name = "ARCorePlaneUtil";
        m_LastTimestamp = 0.0f;
    }

    private void Destroy()
    {
        m_AllPlanes.Clear();
        m_UpdatedPlanes.Clear();
        m_NewPlanes.Clear();
    }
}
