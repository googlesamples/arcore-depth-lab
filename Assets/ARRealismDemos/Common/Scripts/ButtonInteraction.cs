//-----------------------------------------------------------------------
// <copyright file="ButtonInteraction.cs" company="Google LLC">
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
/// Handles button text color while enabling/disabling the button.
/// </summary>
public class ButtonInteraction : MonoBehaviour
{
    /// <summary>
    /// Enabled button text color.
    /// </summary>
    public Color EnabledTextColor = Color.black;

    /// <summary>
    /// Disabled button text color.
    /// </summary>
    public Color DisabledTextColor = new Color32(150, 150, 150, 255);

    /// <summary>
    /// Enables button and sets color to EnabledTextColor.
    /// </summary>
    public void EnableButton()
    {
        GetComponentInChildren<UnityEngine.UI.Text>().color = EnabledTextColor;
        GetComponent<UnityEngine.UI.Button>().enabled = true;
    }

    /// <summary>
    /// Disables button and sets color to DisabledTextColor.
    /// </summary>
    public void DisableButton()
    {
        GetComponentInChildren<UnityEngine.UI.Text>().color = DisabledTextColor;
        GetComponent<UnityEngine.UI.Button>().enabled = false;
    }
}
