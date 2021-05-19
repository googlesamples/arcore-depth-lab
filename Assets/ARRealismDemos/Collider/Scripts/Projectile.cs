//-----------------------------------------------------------------------
// <copyright file="Projectile.cs" company="Google LLC">
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// A projected 3D cursor to guide the user in augmented reality.
/// </summary>
public class Projectile : MonoBehaviour
{
    private Rigidbody _rigidbody;

    private ARRaycastManager _raycastManager;

    private ARAnchorManager _anchorManager;

    // Start is called before the first frame update
    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _raycastManager = FindObjectOfType<ARRaycastManager>();
        Debug.Assert(_raycastManager);

        _anchorManager = FindObjectOfType<ARAnchorManager>();
        Debug.Assert(_anchorManager);
    }

    // Update is called once per frame
    private void Update()
    {
        if (_rigidbody.IsSleeping() && !_rigidbody.isKinematic)
        {
            _rigidbody.isKinematic = true;
            CreateAnchor();
        }
    }

    private void CreateAnchor()
    {
        Vector3 screenPoint = DepthSource.ARCamera.WorldToScreenPoint(transform.position);

        // Raycasts against the location the object stopped.
        TrackableType trackableTypes = TrackableType.Planes | TrackableType.FeaturePoint;
        List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
        if (_raycastManager.Raycast(screenPoint, raycastHits, trackableTypes))
        {
            ARRaycastHit raycastHit = raycastHits[0];
            ARAnchor anchor = null;
            if ((raycastHit.trackable is ARPlane) &&
                Vector3.Dot(DepthSource.ARCamera.transform.position - raycastHit.pose.position,
                raycastHit.pose.rotation * Vector3.up) < 0)
            {
                Debug.Log("Hit at back of the current ARPlane.");
            }
            else if (raycastHit.trackable is ARPlane plane)
            {
                Debug.Log("Create ARAnchor attached to ARPlane.");
                anchor = _anchorManager.AttachAnchor(plane, raycastHit.pose);
            }
            else
            {
                Debug.Log("Create a regular ARAnchor.");
                anchor = new GameObject().AddComponent<ARAnchor>();
                anchor.gameObject.name = "ARAnchor";
                anchor.transform.position = raycastHit.pose.position;
                anchor.transform.rotation = raycastHit.pose.rotation;
            }

            if (anchor != null)
            {
                transform.SetParent(anchor.transform, true);
            }
        }
    }
}
