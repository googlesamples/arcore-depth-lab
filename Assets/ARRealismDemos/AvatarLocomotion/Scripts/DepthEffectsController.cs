//-----------------------------------------------------------------------
// <copyright file="DepthEffectsController.cs" company="Google LLC">
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
/// Manages post-processing depth effects.
/// </summary>
public class DepthEffectsController : MonoBehaviour
{
    /// <summary>
    /// Depth snow particles controller.
    /// </summary>
    public DepthSnowParticlesController Snow;

    /// <summary>
    /// Depth motion lights controller.
    /// </summary>
    public DepthMotionLightsController Lights;

    /// <summary>
    /// Depth background renderer controller.
    /// </summary>
    public DepthBackgroundRendererController Background;

    private States m_State = States.Disabled;

    private enum States
    {
        Disabled,
        Snow,
        Lights
    }

    /// <summary>
    /// Switches the rendering effects among Disabled, Snow, and Lights.
    /// </summary>
    public void OnButtonPressed()
    {
        if (m_State == States.Disabled)
        {
            m_State = States.Snow;
            Snow.MeshRendererEnabled(true);
            Background.EnableGammaCorrection(true);
            Snow.EnableParticles(true);
            Lights.EnableLights(false);
        }
        else if (m_State == States.Snow)
        {
            m_State = States.Lights;
            Background.EnableGammaCorrection(false);
            Snow.EnableParticles(false);
            Lights.EnableLights(true);
        }
        else if (m_State == States.Lights)
        {
            m_State = States.Disabled;
            Background.EnableGammaCorrection(true);
            Snow.EnableParticles(false);
            Snow.MeshRendererEnabled(false);
            Lights.EnableLights(false);
        }
    }
}
