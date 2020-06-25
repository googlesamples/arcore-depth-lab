//-----------------------------------------------------------------------
// <copyright file="NoticeHelper.cs" company="Google LLC">
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

/// <summary>
/// A class responsible for opening notice URLs.
/// </summary>
public class NoticeHelper : MonoBehaviour
{
    /// <summary>
    /// Reference to the info panel.
    /// </summary>
    public GameObject NoticePanel;

    /// <summary>
    /// Reference to the depth support panel.
    /// </summary>
    public GameObject DepthWarningPanel;

    /// <summary>
    /// Opens the notice panel.
    /// </summary>
    public void OpenNoticePanel()
    {
        NoticePanel.SetActive(true);
    }

    /// <summary>
    /// Closes the notice panel.
    /// </summary>
    public void CloseNoticePanel()
    {
        NoticePanel.SetActive(false);
    }

    /// <summary>
    /// Opens the depth warning panel.
    /// </summary>
    public void OpenDepthWarningPanel()
    {
        DepthWarningPanel.SetActive(true);
    }

    /// <summary>
    /// Closes the depth warning panel.
    /// </summary>
    public void CloseDepthWarningPanel()
    {
        DepthWarningPanel.SetActive(false);
    }

    /// <summary>
    /// Toggles the notice panel.
    /// </summary>
    public void ToggleNoticePanel()
    {
        NoticePanel.SetActive(!NoticePanel.activeSelf);
    }

    /// <summary>
    /// Opens the provided URL.
    /// </summary>
    /// <param name="url">The URL of a website.</param>
    public void OpenURL(string url)
    {
        Application.OpenURL(url);
        NoticePanel.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(CheckDepthCoroutine());
    }

    private IEnumerator CheckDepthCoroutine()
    {
        yield return new WaitForSeconds(1);

        // Checks whether the user's device supports depth mode.
        if (!Session.IsDepthModeSupported(DepthMode.Automatic))
        {
            OpenDepthWarningPanel();
        }
    }
}
