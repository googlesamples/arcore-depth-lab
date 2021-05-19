//-----------------------------------------------------------------------
// <copyright file="UpdateDistanceMetrics.cs" company="Google LLC">
//
// Copyright 2020 Google LLC
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

    private OrientedReticle _orientedReticle;

    private LinkedList<float> _floorHeights = new LinkedList<float>();

    private float EstimateFloorHeight()
    {
        float current_height = _orientedReticle.transform.position.y;
        var nodeToInsert = _floorHeights.First;

        // Searches for the right place in the list to place the new height.
        while (nodeToInsert != null)
        {
            if (current_height < nodeToInsert.Value)
            {
                nodeToInsert = nodeToInsert.Next;
            }
            else
            {
                _floorHeights.AddBefore(nodeToInsert, current_height);
                break;
            }
        }

        // If we didn't find any node where to insert the new height
        // value this means that we found a new historical min value.
        if (nodeToInsert == null)
        {
            // In this case add it to the bottom of the stack.
            _floorHeights.AddLast(current_height);
        }

        // Trims the List.
        while (_floorHeights.Count > 100)
        {
            _floorHeights.RemoveFirst();
        }

        return _floorHeights.First.Value;
    }

    private float EstimateCurrentHeight()
    {
        float current_height = transform.position.y;
        float floor_height = EstimateFloorHeight();
        return Mathf.Max(current_height - floor_height, 0.0f);
    }

    private void Start()
    {
        _orientedReticle = FindObjectOfType<OrientedReticle>();
        if (_orientedReticle == null)
        {
            Debug.LogError("Cannot find OrientedReticle.");
        }

        gameObject.SetActive(_orientedReticle != null);

        _floorHeights.AddLast(float.PositiveInfinity);
    }

    private void Update()
    {
        HeightLabel.text = "Height\n" + EstimateCurrentHeight().ToString("F2") + " meters";
        DistanceLabel.text = "Distance\n" + _orientedReticle.Distance.ToString("F2") + " meters";
    }
}
