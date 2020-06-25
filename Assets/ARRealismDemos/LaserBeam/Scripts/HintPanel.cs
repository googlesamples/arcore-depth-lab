//-----------------------------------------------------------------------
// <copyright file="HintPanel.cs" company="Google LLC">
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
/// Shows a hint panel to notify the user that one can touch anywhere on the screen to trigger
/// the laser beam.
/// </summary>
public class HintPanel : MonoBehaviour
{
    /// <summary>
    /// The GameObject that contains the panel UI elements.
    /// </summary>
    public GameObject Panel;

    private const float k_WaitTimeS = 10;
    private const float k_ShowHintTimeS = 5;

    private UnityEngine.UI.Image m_PanelImage;
    private UnityEngine.UI.Text m_PanelText;

    private float m_HintTimer = k_ShowHintTimeS - 1.0f;
    private LaserBeam m_LaserBeam;

    private HintState m_CurrentState = HintState.Show;
    private HintState m_NextState = HintState.Show;
    private bool m_StopShowing = false;

    private enum HintState
    {
        Show,
        Hide,
    }

    private void Start()
    {
         LaserBeam[] laserBeams = Object.FindObjectsOfType<LaserBeam>();
         if (laserBeams.Length > 0)
         {
            m_LaserBeam = laserBeams[0];
         }

        m_PanelImage = Panel.GetComponent<Image>();
        m_PanelText = Panel.GetComponentInChildren<Text>();
        SetPanelAlpha(1.0f);
    }

    private void Update()
    {
        m_StopShowing = m_LaserBeam.HasLaserBeenTriggered();
        if (m_StopShowing)
        {
            SetPanelAlpha(0.0f);
            m_CurrentState = HintState.Hide;
            m_NextState = HintState.Hide;
            m_HintTimer = k_WaitTimeS - 1.0f;
            return;
        }

        m_HintTimer -= Time.deltaTime;
        m_HintTimer = m_HintTimer < 0 ? 0 : m_HintTimer;

        if (m_CurrentState != m_NextState)
        {
            switch (m_NextState)
            {
                case HintState.Show:
                m_HintTimer = k_ShowHintTimeS;
                break;
                case HintState.Hide:
                m_HintTimer = k_WaitTimeS;
                break;
            }

            m_CurrentState = m_NextState;
        }

        switch (m_CurrentState)
        {
            case HintState.Show:
            {
                float panelAlpha = k_ShowHintTimeS - m_HintTimer;
                panelAlpha = Mathf.Clamp(panelAlpha, 0.0f, 1.0f);
                SetPanelAlpha(panelAlpha);

                if (m_HintTimer == 0)
                {
                    m_NextState = HintState.Hide;
                }

                break;
            }

            case HintState.Hide:
            {
                float panelAlpha = k_WaitTimeS - m_HintTimer;
                panelAlpha = 1.0f - Mathf.Clamp(panelAlpha, 0.0f, 1.0f);
                SetPanelAlpha(panelAlpha);

                if (m_HintTimer == 0)
                {
                    m_NextState = HintState.Show;
                }

                break;
            }
        }
    }

    private void SetPanelAlpha(float alpha)
    {
        Color imageColor = m_PanelImage.color;
        imageColor.a = alpha;
        m_PanelImage.color = imageColor;

        Color textColor = m_PanelText.color;
        textColor.a = alpha;
        m_PanelText.color = textColor;
    }
}
