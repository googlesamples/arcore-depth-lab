//-----------------------------------------------------------------------
// <copyright file="PlaneDetectionIcon.cs" company="Google LLC">
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
using UnityEngine.UI;

/// <summary>
/// Manages the detected plane.
/// </summary>
public class PlaneDetectionIcon : MonoBehaviour
{
    /// <summary>
    /// The sprite that shows we have detected an image.
    /// </summary>
    public Sprite PlaneDetectedImage;

    /// <summary>
    /// The sprite that shows we have not detected an image.
    /// </summary>
    public Sprite NoPlaneDetectedImage;

    /// <summary>
    /// Just copying this functionality for the PlaneDiscoveryGuide.cs
    /// This is a quick check to see if planes have been detected.
    /// This variable will store a list of detected planes.
    /// </summary>
    private List<DetectedPlane> m_DetectedPlanes = new List<DetectedPlane>();

    /// <summary>
    /// Add comment here.
    /// </summary>
    public void ShowMe()
    {
        transform.localScale = Vector3.one;
        GetComponent<Image>().sprite = NoPlaneDetectedImage;
        GetComponent<Image>().enabled = true;
    }

    /// <summary>
    /// Add comment here.
    /// </summary>
    private void Update()
    {
        if (Session.Status != SessionStatus.Tracking)
        {
            return;
        }

        Session.GetTrackables<DetectedPlane>(m_DetectedPlanes, TrackableQueryFilter.All);
        if (m_DetectedPlanes.Count > 0)
        {
            // GetComponent<Image>().color = Color.green;
            GetComponent<Image>().sprite = PlaneDetectedImage;
            StartCoroutine("HideMe");
        }
        else
        {
            // GetComponent<Image>().color = Color.red;
            GetComponent<Image>().sprite = NoPlaneDetectedImage;
        }
    }

    /// <summary>
    /// Destroys this gameobject after a given time.
    /// </summary>
    /// <returns>IEnumerator for HideMe method.</returns>
    private IEnumerator HideMe()
    {
        yield return new WaitForSeconds(3f);
        GetComponent<Image>().enabled = false;
    }
}
