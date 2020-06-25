//-----------------------------------------------------------------------
// <copyright file="AvatarHover.cs" company="Google LLC">
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
/// Animates Andy along the y axis with a sine function of time.
/// </summary>
public class AvatarHover : MonoBehaviour
{
    private const float k_HoverRange = 0.08f;
    private float m_InitialYPosition;

    private void Start()
    {
        m_InitialYPosition = transform.localPosition.y;
    }

    private void Update()
    {
        Vector3 localPos = transform.localPosition;
        localPos.y = m_InitialYPosition + (k_HoverRange * Mathf.Sin(Time.time));
        transform.localPosition = localPos;
    }
}
