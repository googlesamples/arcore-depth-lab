//-----------------------------------------------------------------------
// <copyright file="UpdateParticles.cs" company="Google LLC">
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
/// Part of a GPU particle implementation.
/// The Particle's state is kept in two textures each channel storing particle attributes,
/// or a component of an attribute, such as position, direction and lifetime.
/// This class manages two render textures in order to keep and update the current particle's state.
/// </summary>
public class UpdateParticles : MonoBehaviour
{
    /// <summary>
    /// Texture that represents the particles initial state.
    /// </summary>
    public Texture InitialState;

    /// <summary>
    /// Represents the current particle's state.
    /// </summary>
    public RenderTexture Target0;

    /// <summary>
    /// The render target where the new particle's state gets rendered(updated) to.
    /// </summary>
    public RenderTexture Target1;

    /// <summary>
    /// Resets the particle's state.
    /// </summary>
    public void Reset()
    {
        if (InitialState != null)
        {
            Graphics.Blit(InitialState, Target0);
        }
        else
        {
            Graphics.Blit(Texture2D.blackTexture, Target0);
        }
    }

    private void Start()
    {
        Reset();
    }

    private void OnPostRender()
    {
        Graphics.Blit(Target1, Target0);
    }
}
