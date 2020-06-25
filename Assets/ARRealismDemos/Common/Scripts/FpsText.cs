//-----------------------------------------------------------------------
// <copyright file="FpsText.cs" company="Google LLC">
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
/// Attach this script with any game object to show the frame rate in real time.
/// </summary>
public class FpsText : MonoBehaviour
{
    private float m_DeltaTime = 0.0f;

    private void Update()
    {
        m_DeltaTime += (Time.deltaTime - m_DeltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(0, 0, Screen.width, Screen.height / 50);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = Screen.height / 50;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        float msec = m_DeltaTime * 1000.0f;
        float fps = 1.0f / m_DeltaTime;
        string text = string.Format("{0:0.0} ms, {1:0.} fps", msec, fps);
        GUI.Label(rect, text, style);
    }
}
