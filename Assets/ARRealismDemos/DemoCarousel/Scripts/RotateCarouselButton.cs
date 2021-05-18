//-----------------------------------------------------------------------
// <copyright file="RotateCarouselButton.cs" company="Google LLC">
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
/// A class responsible for rotating the quad displaying an icon on the carousel.
/// Attach this to the quad within one of the UI_carousel items.
/// </summary>
public class RotateCarouselButton : MonoBehaviour
{
    private DeviceOrientation _deviceOrientationInit;

    // Start is called before the first frame update
    private void Start()
    {
        _deviceOrientationInit = DeviceOrientationInit.DeviceOrientationInstance;

        if (_deviceOrientationInit == DeviceOrientation.Unknown)
        {
            StartCoroutine(GetValidDeviceOrientation());
        }
        else
        {
            RotateCarouselItem();
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
            _deviceOrientationInit = DeviceOrientationInit.DeviceOrientationInstance;
            RotateCarouselItem();
        }
    }

    private void RotateCarouselItem()
    {
        switch (_deviceOrientationInit)
        {
            case DeviceOrientation.LandscapeLeft:
                transform.Rotate(0, 0, -90f);
                break;
            case DeviceOrientation.LandscapeRight:
                transform.Rotate(0, 0, 90f);
                break;
            default:
                break;
        }
    }
}
