//-----------------------------------------------------------------------
// <copyright file="RotateCanvas.cs" company="Google LLC">
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
using UnityEngine.UI;

/// <summary>
/// A class responsible for rotating the labels of UI buttons
/// based on the initial device orientation.
/// </summary>
public class RotateCanvas : MonoBehaviour
{
    private const float _globalButtonOpacity = 0.2f;

    private DeviceOrientation _deviceOrientationInit;
    private RectTransform _rectTransform;

    private void Start()
    {
        _deviceOrientationInit = DeviceOrientationInit.DeviceOrientationInstance;

        if (GetComponent<Text>() != null)
        {
            _rectTransform = GetComponent<Text>().rectTransform;
        }
        else if (GetComponent<Image>() != null)
        {
            _rectTransform = GetComponent<Image>().rectTransform;
        }

        switch (_deviceOrientationInit)
        {
            case DeviceOrientation.LandscapeLeft:
                _rectTransform.rotation = Quaternion.Euler(
                    _rectTransform.eulerAngles.x, _rectTransform.eulerAngles.y, -90f);
                break;
            case DeviceOrientation.LandscapeRight:
                _rectTransform.rotation = Quaternion.Euler(
                    _rectTransform.eulerAngles.x, _rectTransform.eulerAngles.y, 90f);
                break;
            default:
                break;
        }
    }
}
