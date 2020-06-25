//-----------------------------------------------------------------------
// <copyright file="CheckReticleOrientation.cs" company="Google LLC">
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
/// Determines whether the reticle is tilted and therefore unsuitable to place an object.
/// </summary>
public class CheckReticleOrientation : MonoBehaviour
{
    private const float k_MaxReticleTiltDegrees = 90;
    private const float k_Tolerance = 10;
    private bool m_ReticleTilted = false;

    /// <summary>
    /// Gets a value indicating whether the reticle is tilted.
    /// </summary>
    public bool ReticleTilted
    {
        get
        {
            return m_ReticleTilted;
        }
    }

    private void Update()
    {
        float angle = Vector3.Angle(transform.up, Vector3.up);
        if (angle > k_MaxReticleTiltDegrees)
        {
            m_ReticleTilted = true;
        }
        else if (angle < k_MaxReticleTiltDegrees - k_Tolerance)
        {
            m_ReticleTilted = false;
        }
    }
}
