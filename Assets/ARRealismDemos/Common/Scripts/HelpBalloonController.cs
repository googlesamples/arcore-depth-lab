//-----------------------------------------------------------------------
// <copyright file="HelpBalloonController.cs" company="Google LLC">
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
/// Hides the help balloon.
/// </summary>
public class HelpBalloonController : MonoBehaviour
{
    /// <summary>
    /// Hides the help balloon on screen tap if true.
    /// </summary>
    public bool HideOnTap = false;

    /// <summary>
    /// Starts with fading in.
    /// </summary>
    public bool FadeInOnStart = false;

    /// <summary>
    /// Position balloon transform.
    /// </summary>
    public Transform BalloonPosition;

    /// <summary>
    /// Position balloon offset.
    /// </summary>
    public Vector2 PositionOffset;
    private bool m_HelpBalloonShowing = true;

    /// <summary>
    /// Starts with fading out.
    /// </summary>
    public void FadeOut()
    {
        StartCoroutine(ShowHide(false));
    }

    /// <summary>
    /// Starts with fading out.
    /// </summary>
    public void FadeIn()
    {
        StartCoroutine(ShowHide(true));
    }

    private void Awake()
    {
        if (FadeInOnStart)
        {
            GetComponent<CanvasGroup>().alpha = 0;
            StartCoroutine(ShowHide(true));
        }
    }

    private void Update()
    {
        if (HideOnTap && Input.touchCount > 0 && m_HelpBalloonShowing)
        {
            StartCoroutine(ShowHide(false));
        }

        if (BalloonPosition != null)
        {
            Transform t = transform.GetChild(0).transform;
            t.position = BalloonPosition.position;
            t.position += new Vector3(PositionOffset.x * transform.localScale.x,
                PositionOffset.y * transform.localScale.y, 0);
        }
    }

    /// <summary>
    /// Fade the balloon canvas.
    /// </summary>
    /// <returns>Returns null.</returns>
    /// <param name="Show">Show(true) or hide(false) the help balloon.</param>
    private IEnumerator ShowHide(bool Show)
    {
        if (!Show)
        {
            m_HelpBalloonShowing = false;
            while (GetComponent<CanvasGroup>().alpha > 0.02f)
            {
                GetComponent<CanvasGroup>().alpha -= Time.deltaTime * 2;
                yield return null;
            }

            GetComponent<CanvasGroup>().alpha = 0;
        }
        else
        {
            while (GetComponent<CanvasGroup>().alpha <= .98f)
            {
                GetComponent<CanvasGroup>().alpha += Time.deltaTime * 2;
                yield return null;
            }

            GetComponent<CanvasGroup>().alpha = 1;
            m_HelpBalloonShowing = true;
        }
    }
}
