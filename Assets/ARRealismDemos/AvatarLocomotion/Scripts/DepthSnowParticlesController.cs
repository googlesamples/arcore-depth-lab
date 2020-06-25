//-----------------------------------------------------------------------
// <copyright file="DepthSnowParticlesController.cs" company="Google LLC">
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
/// Manages the snow particles in the scene.
/// </summary>
public class DepthSnowParticlesController : MonoBehaviour
{
    /// <summary>
    /// Stores particle speed.
    /// </summary>
    public Material ParticleVelocityMaterial;

    /// <summary>
    /// Stores particle states.
    /// </summary>
    public UpdateParticles ParticlesVelocityState;

    /// <summary>
    /// Stores particle positions.
    /// </summary>
    public UpdateParticles ParticlesPositionState;

    /// <summary>
    /// Renderer of the particle.
    /// </summary>
    public MeshRenderer ParticlesRenderer;

    private bool m_ParticlesEnabled = true;
    private bool m_ParticleStateChanged = true;

    /// <summary>
    /// Activates or deactivates the snow particle system.
    /// </summary>
    /// <param name="particlesEnabled">Enable particles.</param>
    public void EnableParticles(bool particlesEnabled)
    {
        m_ParticleStateChanged = true;
        m_ParticlesEnabled = particlesEnabled;
    }

    /// <summary>
    /// Activates or deactivates the particle renderer.
    /// </summary>
    /// <param name="enable">Enable mesh renderer.</param>
    public void MeshRendererEnabled(bool enable)
    {
        ParticlesRenderer.enabled = enable;
    }

    private void Start()
    {
        EnableParticles(false);
    }

    private void Update()
    {
        if (ParticleVelocityMaterial != null && m_ParticleStateChanged)
        {
            ParticleVelocityMaterial.SetFloat("_EmitParticles", m_ParticlesEnabled ? 1 : 0);
            if (m_ParticlesEnabled)
            {
                ParticlesVelocityState.Reset();
                ParticlesPositionState.Reset();
            }

            m_ParticleStateChanged = false;
        }
    }
}
