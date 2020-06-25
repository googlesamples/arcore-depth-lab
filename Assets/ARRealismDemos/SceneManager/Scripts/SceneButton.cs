//-----------------------------------------------------------------------
// <copyright file="SceneButton.cs" company="Google LLC">
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
using UnityEngine.Serialization;

/// <summary>
/// Script for triggering scenes. Attach this to a quad.
/// </summary>
public class SceneButton : MonoBehaviour
{
    /// <summary>
    /// Name of the scene file that the button will trigger.
    /// </summary>
    public string SceneName = null;

    /// <summary>
    /// Title of the scene to populate in the carousel label.
    /// </summary>
    public string SceneLabel;

    private SceneSwitcher m_SceneSwitcher;

    /// <summary>
    /// Triggers the scene switch.
    /// </summary>
    public void Press()
    {
        if (SceneName != string.Empty)
        {
            Debug.Log("Switching to scene: " + SceneName);

            // Load scene by name -> sceneName
            m_SceneSwitcher.LoadScene(SceneName);
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        m_SceneSwitcher = SceneSwitcher.Instance;
    }
}
