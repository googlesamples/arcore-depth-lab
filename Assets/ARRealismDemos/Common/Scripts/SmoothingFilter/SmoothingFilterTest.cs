//-----------------------------------------------------------------------
// <copyright file="SmoothingFilterTest.cs" company="Google LLC">
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
/// This script helps testing the filter parameters.
/// </summary>
public class SmoothingFilterTest : MonoBehaviour
{
    /// <summary>
    /// Attach a transform that represents the unfiltered noisy data.
    /// </summary>
    public Transform NoisyObject;

    /// <summary>
    /// Attach a transform that represents the filtered data.
    /// </summary>
    public Transform FilteredObject;

    /// <summary>
    /// Attach a transform that represents the unmodified clean input.
    /// </summary>
    public Transform NeutralObject;

    /// <summary>
    /// Amount of maximum noise in meters.
    /// </summary>
    public float PositionalNoiseM;

    /// <summary>
    /// Ratio of blending between input and random rotation. 0 is input, 1 is random rotation.
    /// </summary>
    [Range(0, 1)]
    public float RotationalNoiseRatio;

    /// <summary>
    /// The Z-offset of the test objects from the camera.
    /// </summary>
    public float TestObjectZOffsetM = k_TestObjectZOffsetM;

    private const float k_TestObjectZOffsetM = 2;
    private const float k_TestObjectXOffsetM = 1;
    private float m_MouseScroll;
    private bool m_DetachTestObjects;
    private Vector3 m_PrevPos;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            m_DetachTestObjects = !m_DetachTestObjects;
        }

        Vector2 v2 = Input.mousePosition;
        Vector3 v3 = m_DetachTestObjects ?
            m_PrevPos :
            Camera.main.ScreenToWorldPoint(new Vector3(v2.x, v2.y, TestObjectZOffsetM));

        Vector3 newPosition = v3 + (Random.onUnitSphere * PositionalNoiseM * 0.5f);
        m_MouseScroll += Input.mouseScrollDelta.y;

        Quaternion rotation = Quaternion.Euler(0, 0, m_MouseScroll);
        Quaternion newRotation = Quaternion.Slerp(
            rotation,
            Random.rotationUniform,
            RotationalNoiseRatio);

        if (NeutralObject != null)
        {
            NeutralObject.localPosition = v3;
            NeutralObject.localRotation = rotation;
        }

        PositionFilter posFilter;
        QuaternionFilter rotFilter;
        if (FilteredObject != null &&
            (posFilter = FilteredObject.GetComponent<PositionFilter>()) != null &&
            (rotFilter = FilteredObject.GetComponent<QuaternionFilter>()) != null)
        {
            Vector3 filteredPos = posFilter.Filter(newPosition);
            filteredPos.x += -0.5f * k_TestObjectXOffsetM;
            FilteredObject.transform.localPosition = filteredPos;

            Quaternion filteredRotation = rotFilter.Filter(newRotation);
            FilteredObject.transform.localRotation = filteredRotation;
        }

        if (NoisyObject != null)
        {
            newPosition.x += 0.5f * k_TestObjectXOffsetM;
            NoisyObject.localPosition = newPosition;

            NoisyObject.transform.localRotation = newRotation;
        }

        m_PrevPos = v3;
    }
}
