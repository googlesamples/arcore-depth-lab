//-----------------------------------------------------------------------
// <copyright file="HintPanel.cs" company="Google LLC">
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
/// Shows a hint panel to notify the user that one can touch anywhere on the screen to trigger
/// the laser beam.
/// </summary>
public class HintPanel : MonoBehaviour
{
    /// <summary>
    /// The GameObject that contains the panel UI elements.
    /// </summary>
    public GameObject Panel;

    private const float _waitTimeS = 10;
    private const float _showHintTimeS = 5;

    private UnityEngine.UI.Image _panelImage;
    private UnityEngine.UI.Text _panelText;

    private float _hintTimer = _showHintTimeS - 1.0f;
    private LaserBeam _laserBeam;

    private HintState _currentState = HintState.Show;
    private HintState _nextState = HintState.Show;
    private bool _stopShowing = false;

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
            _laserBeam = laserBeams[0];
        }

        _panelImage = Panel.GetComponent<Image>();
        _panelText = Panel.GetComponentInChildren<Text>();
        SetPanelAlpha(1.0f);
    }

    private void Update()
    {
        _stopShowing = _laserBeam.HasLaserBeenTriggered();
        if (_stopShowing)
        {
            SetPanelAlpha(0.0f);
            _currentState = HintState.Hide;
            _nextState = HintState.Hide;
            _hintTimer = _waitTimeS - 1.0f;
            return;
        }

        _hintTimer -= Time.deltaTime;
        _hintTimer = _hintTimer < 0 ? 0 : _hintTimer;

        if (_currentState != _nextState)
        {
            switch (_nextState)
            {
                case HintState.Show:
                _hintTimer = _showHintTimeS;
                break;
                case HintState.Hide:
                _hintTimer = _waitTimeS;
                break;
            }

            _currentState = _nextState;
        }

        switch (_currentState)
        {
            case HintState.Show:
            {
                float panelAlpha = _showHintTimeS - _hintTimer;
                panelAlpha = Mathf.Clamp(panelAlpha, 0.0f, 1.0f);
                SetPanelAlpha(panelAlpha);

                if (_hintTimer == 0)
                {
                    _nextState = HintState.Hide;
                }

                break;
            }

            case HintState.Hide:
            {
                float panelAlpha = _waitTimeS - _hintTimer;
                panelAlpha = 1.0f - Mathf.Clamp(panelAlpha, 0.0f, 1.0f);
                SetPanelAlpha(panelAlpha);

                if (_hintTimer == 0)
                {
                    _nextState = HintState.Show;
                }

                break;
            }
        }
    }

    private void SetPanelAlpha(float alpha)
    {
        Color imageColor = _panelImage.color;
        imageColor.a = alpha;
        _panelImage.color = imageColor;

        Color textColor = _panelText.color;
        textColor.a = alpha;
        _panelText.color = textColor;
    }
}
