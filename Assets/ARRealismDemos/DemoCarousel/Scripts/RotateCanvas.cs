//-----------------------------------------------------------------------
// <copyright file="RotateCanvas.cs" company="Google LLC">
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
/// A class responsible for rotating the labels of UI buttons
/// based on the initial device orientation.
/// </summary>
public class RotateCanvas : MonoBehaviour
{
    private const float k_GlobalButtonOpacity = 0.2f;

    private DeviceOrientation m_DeviceOrientationInit;
    private RectTransform m_RectTransform;

    private void Start()
    {
        m_DeviceOrientationInit = DeviceOrientationInit.DeviceOrientationInstance;

        if (GetComponent<Text>() != null)
        {
            m_RectTransform = GetComponent<Text>().rectTransform;
        }
        else if (GetComponent<Image>() != null)
        {
            m_RectTransform = GetComponent<Image>().rectTransform;
        }

        switch (m_DeviceOrientationInit)
        {
            case DeviceOrientation.LandscapeLeft:
                m_RectTransform.rotation = Quaternion.Euler(
                    m_RectTransform.eulerAngles.x, m_RectTransform.eulerAngles.y, -90f);
                break;
            case DeviceOrientation.LandscapeRight:
                m_RectTransform.rotation = Quaternion.Euler(
                    m_RectTransform.eulerAngles.x, m_RectTransform.eulerAngles.y, 90f);
                break;
            default:
                break;
        }

        // UpdateGlobalButtonOpacity();
    }

    // Updates the opacity of UI buttons globally.
    private void UpdateGlobalButtonOpacity()
    {
        Button button = transform.GetComponentInParent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;

            Color normalColor = colors.normalColor;
            normalColor.a = k_GlobalButtonOpacity;

            Color highlightedColor = colors.highlightedColor;
            highlightedColor.a = k_GlobalButtonOpacity;

            Color pressedColor = colors.pressedColor;
            pressedColor.a = k_GlobalButtonOpacity;

            Color disabledColor = colors.disabledColor;
            disabledColor.a = k_GlobalButtonOpacity;

            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.disabledColor = disabledColor;

            button.colors = colors;
        }
    }
}
