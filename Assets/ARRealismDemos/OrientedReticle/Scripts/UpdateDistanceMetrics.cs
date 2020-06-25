//-----------------------------------------------------------------------
// <copyright file="UpdateDistanceMetrics.cs" company="Google LLC">
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
using UnityEngine.UI;

/// <summary>
/// Estimates and updates the text label for the distance metrics.
/// </summary>
public class UpdateDistanceMetrics : MonoBehaviour
{
    /// <summary>
    /// Reference to the distance label text.
    /// </summary>
    public Text DistanceLabel;

    /// <summary>
    /// References to the height label text.
    /// </summary>
    public Text HeightLabel;

    private OrientedReticle m_OrientedReticle;

    private LinkedList<float> m_FloorHeights = new LinkedList<float>();

    private float EstimateFloorHeight()
    {
        float current_height = m_OrientedReticle.transform.position.y;
        var nodeToInsert = m_FloorHeights.First;

        // Searches for the right place in the list to place the new height.
        while (nodeToInsert != null)
        {
            if (current_height < nodeToInsert.Value)
            {
                nodeToInsert = nodeToInsert.Next;
            }
            else
            {
                m_FloorHeights.AddBefore(nodeToInsert, current_height);
                break;
            }
        }

        // If we didn't find any node where to insert the new height
        // value this means that we found a new historical min value.
        if (nodeToInsert == null)
        {
            // In this case add it to the bottom of the stack.
            m_FloorHeights.AddLast(current_height);
        }

        // Trims the List.
        while (m_FloorHeights.Count > 100)
        {
            m_FloorHeights.RemoveFirst();
        }

        return m_FloorHeights.First.Value;
    }

    private float estimateCurrentHeight()
    {
        float current_height = transform.position.y;
        float floor_height = EstimateFloorHeight();
        return Mathf.Max(current_height - floor_height, 0.0f);
    }

    private void Start()
    {
        m_OrientedReticle = GetComponent<OrientedReticle>();
        m_FloorHeights.AddLast(float.PositiveInfinity);
    }

    private void Update()
    {
        HeightLabel.text = "Height\n" + estimateCurrentHeight().ToString("F2") + " meters";
        DistanceLabel.text = "Distance\n" + m_OrientedReticle.Distance.ToString("F2") + " meters";
    }
}
