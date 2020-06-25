//-----------------------------------------------------------------------
// <copyright file="ToggleUIVisibility.cs" company="Google LLC">
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
/// A class containing functions for toggling the UI for the carousel.
/// and scene UI elements.
/// </summary>
public class ToggleUIVisibility : MonoBehaviour
{
    /// <summary>
    /// GameObjects for the carousel.
    /// </summary>
    public GameObject[] CarouselCanvases;

    /// <summary>
    /// Function to be attached to a toggle for enabling/disabling
    /// the carousel visibility.
    /// </summary>
    /// <param name="visible">Whether or not the carousel is visible.</param>
    public void CarouselVisible(bool visible)
    {
        foreach (GameObject canvas in CarouselCanvases)
        {
            canvas.SetActive(visible);
        }
    }

    /// <summary>
    /// Function to be attached to a toggle for enabling/disabling
    /// all UI elements in the app.
    /// </summary>
    /// <param name="visible">Whether or not the UI is visible.</param>
    public void AllUIVisible(bool visible)
    {
        Button[] Buttons = Resources.FindObjectsOfTypeAll(typeof(Button)) as Button[];
        Slider[] Sliders = Resources.FindObjectsOfTypeAll(typeof(Slider)) as Slider[];

        foreach (Button button in Buttons)
        {
            var origColor = button.gameObject.GetComponent<Image>().color;

            if (visible)
            {
                origColor.a = 1f;
            }
            else
            {
                origColor.a = 0f;
            }

            button.gameObject.GetComponent<Image>().color = origColor;

            foreach (Transform child in button.transform)
            {
                child.gameObject.SetActive(visible);
            }
        }

        foreach (Slider slider in Sliders)
        {
            slider.gameObject.SetActive(visible);
        }

        CarouselVisible(visible);
    }
}
