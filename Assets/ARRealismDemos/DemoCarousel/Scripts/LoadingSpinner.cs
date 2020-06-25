//-----------------------------------------------------------------------
// <copyright file="LoadingSpinner.cs" company="Google LLC">
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
/// Shows a spinner while loading a sccne in the background.
/// </summary>
public class LoadingSpinner : MonoBehaviour
{
    /// <summary>
    /// Instance of the LoadingSpinner class.
    /// </summary>
    public static LoadingSpinner Instance;

    private readonly object k_CurrentLoadingOperationLock = new object();

    // The reference to the current loading operation running in the background
    private AsyncOperation m_CurrentLoadingOperation;

    // A flag to tell whether a scene is being loaded or not
    private bool m_IsLoading;

    // Canvas group used to fade in/out the spinner
    private CanvasGroup m_CanvasGroup;

    /// <summary>
    /// Shows the loading spinner.
    /// </summary>
    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            // Locks the interactions.
            Handheld.StartActivityIndicator();

            // Enables the spinner.
            FadeIn();
        }
    }

    /// <summary>
    /// Sets the loading AsyncOperation.
    /// </summary>
    /// <param name="loadingOperation">AsynOperation to monitor.</param>
    public void SetLoadingOperation(AsyncOperation loadingOperation)
    {
        lock (k_CurrentLoadingOperationLock)
        {
            // Stores the reference.
            m_CurrentLoadingOperation = loadingOperation;
            m_IsLoading = true;
        }
    }

    /// <summary>
    /// Hides the loading spinner.
    /// </summary>
    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            // Disables the spinner
            FadeOut();
            lock (k_CurrentLoadingOperationLock)
            {
                m_CurrentLoadingOperation = null;
                m_IsLoading = false;
            }
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Don't destroy the loading spinner while switching scenes.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        m_CanvasGroup = gameObject.GetComponent(typeof(CanvasGroup)) as CanvasGroup;

        #if UNITY_ANDROID
        Handheld.SetActivityIndicatorStyle(AndroidActivityIndicatorStyle.DontShow);
        #endif
        Hide();
    }

    private void Update()
    {
        if (m_IsLoading)
        {
            lock (k_CurrentLoadingOperationLock)
            {
                // Hides the spinner when completed.
                if (m_CurrentLoadingOperation.isDone)
                {
                    Hide();
                    Handheld.StopActivityIndicator();
                }
            }
        }
    }

    private void FadeIn()
    {
        gameObject.SetActive(true);
        StartCoroutine(FadeSpinner(m_CanvasGroup, m_CanvasGroup.alpha, 1, .5f));
    }

    private void FadeOut()
    {
        StartCoroutine(FadeSpinner(m_CanvasGroup, m_CanvasGroup.alpha, 0, .5f));
    }

    private IEnumerator FadeSpinner(CanvasGroup canvasGroup, float start,
                                    float end, float lerpTime = 1)
    {
        float timeStartedLerping = Time.time;
        float timeSinceStarted = Time.time - timeStartedLerping;
        float percentageComplete = timeSinceStarted / lerpTime;

        while (true)
        {
            timeSinceStarted = Time.time - timeStartedLerping;
            percentageComplete = timeSinceStarted / lerpTime;

            float currentAlphaValue = Mathf.Lerp(start, end, percentageComplete);
            canvasGroup.alpha = currentAlphaValue;

            if (percentageComplete >= 1)
            {
                canvasGroup.alpha = end;
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        // Deactivates the object when fading out.
        if (end == 0)
        {
            gameObject.SetActive(false);
        }
    }
}
