//-----------------------------------------------------------------------
// <copyright file="CarouselLabelOrientation.cs" company="Google LLC">
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
using UnityEngine.UI;

/// <summary>
/// A class responsible for enabling the initial carousel active
/// scene label orientation.
/// </summary>
public class CarouselLabelOrientation : MonoBehaviour
{
    /// <summary>
    /// GameObject for the scene label in portrait mode.
    /// </summary>
    public GameObject SceneLabelPortrait;

    /// <summary>
    /// GameObject for the scene label in landscape left mode.
    /// </summary>
    public GameObject SceneLabelLandscapeLeft;

    /// <summary>
    /// GameObject for the scene label in landscape right mode.
    /// </summary>
    public GameObject SceneLabelLandscapeRight;

    /// <summary>
    /// CarouselMovement class.
    /// </summary>
    public CarouselMovement CarouselMovement;

    private DeviceOrientation m_DeviceOrientationInit;

    // Start is called before the first frame update
    private void Start()
    {
        m_DeviceOrientationInit = DeviceOrientationInit.DeviceOrientationInstance;

        if (m_DeviceOrientationInit == DeviceOrientation.Unknown)
        {
            StartCoroutine(GetValidDeviceOrientation());
        }
        else
        {
            RotateCarouselLabel();
        }
    }

    private IEnumerator GetValidDeviceOrientation()
    {
        if (Input.deviceOrientation == DeviceOrientation.Unknown)
        {
            yield return 0;
        }
        else
        {
            DeviceOrientationInit.DeviceOrientationInstance = Input.deviceOrientation;
            m_DeviceOrientationInit = DeviceOrientationInit.DeviceOrientationInstance;
            RotateCarouselLabel();
        }
    }

    private void RotateCarouselLabel()
    {
        switch (m_DeviceOrientationInit)
        {
            case DeviceOrientation.LandscapeLeft:
                CarouselMovement.ButtonLabel = SceneLabelLandscapeLeft.GetComponent<Text>();
                SceneLabelPortrait.SetActive(false);
                SceneLabelLandscapeLeft.SetActive(true);
                break;
            case DeviceOrientation.LandscapeRight:
                CarouselMovement.ButtonLabel = SceneLabelLandscapeRight.GetComponent<Text>();
                SceneLabelPortrait.SetActive(false);
                SceneLabelLandscapeRight.SetActive(true);
                break;
            default:
                break;
        }
    }
}
