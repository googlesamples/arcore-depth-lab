//-----------------------------------------------------------------------
// <copyright file="AvatarPathController.cs" company="Google LLC">
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

    private const float _avatarOffset = 0.9f;

    private const float _waypointYOffset = 0.05f;

    private GameObject _root;

    private AvatarController _avatarController;

    private bool _firstWaypointPlaced;

    /// <summary>
    /// Sets a waypoint for the avatar.
    /// </summary>
    public void DropWaypoint()
    {
        if (_root == null)
        {
            _root = new GameObject("Waypoints");
        }

        Vector3 pos = DepthCursor.transform.position;
        pos.y += _waypointYOffset;

        GameObject marker = Instantiate(Waypoint, pos, Quaternion.identity);
        marker.transform.parent = _root.transform;

        if (_avatarController != null)
        {
            _avatarController.AddNewCubeObject(marker);
        }

        _firstWaypointPlaced = true;
    }

    /// <summary>
    /// Clears all the instantiated waypoints.
    /// </summary>
    public void Clear()
    {
        if (_root != null)
        {
            foreach (Transform child in _root.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        Destroy(_root);
        _root = null;
    }

    private void Start()
    {
        _avatarController = Andy.GetComponent<AvatarController>();
    }

    private void Update()
    {
        if (!_firstWaypointPlaced)
        {
            Vector3 toCamera =
                DepthSource.ARCamera.transform.position - DepthCursor.transform.position;

            Andy.transform.position = DepthCursor.transform.position +
                (toCamera.normalized * _avatarOffset);
        }
    }
}
