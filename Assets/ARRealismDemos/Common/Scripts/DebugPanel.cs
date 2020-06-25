//-----------------------------------------------------------------------
// <copyright file="DebugPanel.cs" company="Google LLC">
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
/// An easy-to-use generic debug panel in Singleton pattern to print real-time outputs
/// directly on the phone.
/// ## Usage
///     * DebugPanel.Instance.Print("This is a message.");
///     * DebugPanel.Instance.SetGreen();
/// The DebugPanel will be initialized when first called.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    /// <summary>
    /// Shows the debug console and prints out the collision-related variables.
    /// </summary>
    public bool ShowDebugOverlay = true;

    // A red color to indicate collision in the bottom debug panel.
    private static Color s_ColorRed = new Color(252.0f / 255, 141.0f / 255, 89.0f / 255);

    // A green color to indicate no collision in the bottom debug panel.
    private static Color s_ColorGreen = new Color(145.0f / 255, 207.0f / 255, 96.0f / 255);

    private static DebugPanel s_Instance;
    private Image m_DebugButton;
    private Text m_DebugConsole;

    /// <summary>
    /// Gets the Singleton class of the DebugPanel.
    /// </summary>
    public static DebugPanel Instance
    {
        get
        {
            return s_Instance;
        }
    }

    /// <summary>
    /// Prints a message on the debug panel.
    /// </summary>
    /// <param name="message">The string to print.</param>
    public void Print(string message)
    {
        if (ShowDebugOverlay)
        {
            m_DebugConsole.text = message;
        }
    }

    /// <summary>
    /// Sets the background color of the debug panel to green.
    /// </summary>
    public void SetGreen()
    {
        SetColor(s_ColorGreen);
    }

    /// <summary>
    /// Sets the background color of the debug panel to red.
    /// </summary>
    public void SetRed()
    {
        SetColor(s_ColorRed);
    }

    /// <summary>
    /// Sets the background color of the debug panel to a specific color.
    /// </summary>
    /// <param name="color">The background color to set.</param>
    public void SetColor(Color color)
    {
        if (ShowDebugOverlay)
        {
            m_DebugButton.color = color;
        }
    }

    /// <summary>
    /// Updates the collision animiation every frame: rotates Andy when collision occurs.
    /// </summary>
    protected void Start()
    {
        if (GameObject.Find("DebugButton") == null || GameObject.Find("DebugConsole") == null)
        {
            Debug.LogError("Cannot find the debug panel in the scene. \n" +
                "Please copy DebugButton and DebugConsole from other scenes.");
            ShowDebugOverlay = false;
            return;
        }

        if (ShowDebugOverlay)
        {
            m_DebugButton = GameObject.Find("DebugButton").GetComponent<Image>();
            m_DebugConsole = GameObject.Find("DebugConsole").GetComponent<Text>();
        }
        else
        {
            GameObject.Find("DebugButton").SetActive(false);
            GameObject.Find("DebugConsole").SetActive(false);
        }
    }

    /// <summary>
    /// Checks if there is a different instance and destroys it when necesssary.
    /// </summary>
    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            s_Instance = this;
        }
    }
}
