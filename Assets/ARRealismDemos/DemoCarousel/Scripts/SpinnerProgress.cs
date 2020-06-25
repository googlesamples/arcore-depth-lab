//-----------------------------------------------------------------------
// <copyright file="SpinnerProgress.cs" company="Google LLC">
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
/// Creates a spinner effect by rotating the source image.
/// </summary>
public class SpinnerProgress : MonoBehaviour
{
    private RectTransform m_SpinnerComponent;
    private float m_RotateSpeed = 400f;

    private void Start()
    {
        m_SpinnerComponent = GetComponent<RectTransform>();
    }

    private void Update()
    {
        m_SpinnerComponent.Rotate(0f, 0f, m_RotateSpeed * Time.deltaTime);
    }
}
