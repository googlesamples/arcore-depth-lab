//-----------------------------------------------------------------------
// <copyright file="AvatarPathController.cs" company="Google LLC">
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
/// Controls the demo flow of hovering Andy.
/// </summary>
public class AvatarPathController : MonoBehaviour
{
    /// <summary>
    /// References the 3D cursor.
    /// </summary>
    public GameObject DepthCursor;

    /// <summary>
    /// References the Andy avatar.
    /// </summary>
    public GameObject Andy;

    /// <summary>
    /// References the waypoint.
    /// </summary>
    public GameObject Waypoint;

    private const float k_AvatarOffset = 0.9f;

    private const float k_WaypointYOffset = 0.05f;

    private GameObject m_Root;

    private AvatarController m_AvatarController;

    private bool m_FirstWaypointPlaced;

    /// <summary>
    /// Sets a waypoint for the avatar.
    /// </summary>
    public void DropWaypoint()
    {
        if (m_Root == null)
        {
            m_Root = new GameObject("Waypoints");
        }

        Vector3 pos = DepthCursor.transform.position;
        pos.y += k_WaypointYOffset;

        GameObject marker = Instantiate(Waypoint, pos, Quaternion.identity);
        marker.transform.parent = m_Root.transform;

        if (m_AvatarController != null)
        {
            m_AvatarController.AddNewCubeObject(marker);
        }

        m_FirstWaypointPlaced = true;
    }

    /// <summary>
    /// Clears all the instantiated waypoints.
    /// </summary>
    public void Clear()
    {
        if (m_Root != null)
        {
            foreach (Transform child in m_Root.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        Destroy(m_Root);
        m_Root = null;
    }

    private void Start()
    {
        m_AvatarController = Andy.GetComponent<AvatarController>();
    }

    private void Update()
    {
        if (!m_FirstWaypointPlaced)
        {
            Vector3 toCamera = Camera.main.transform.position - DepthCursor.transform.position;

            Andy.transform.position = DepthCursor.transform.position +
                (toCamera.normalized * k_AvatarOffset);
        }
    }
}
