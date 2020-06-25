//-----------------------------------------------------------------------
// <copyright file="RandomizeScaleAndRotation.cs" company="Google LLC">
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
/// Randomizes the scale in all dimensions and random rotate the object its local z axis.
/// </summary>
public class RandomizeScaleAndRotation : MonoBehaviour
{
    /// <summary>
    /// The deviant that is applied to the random range.
    /// </summary>
    public float Deviantion = 0.006f;

    private void Start()
    {
        float low = transform.localScale.x - Deviantion;
        float high = transform.localScale.x + Deviantion;
        float scale_x = Random.Range(low, high);
        float scale_y = Random.Range(low, high);
        float scale_z = Random.Range(low, high);
        transform.localScale += new Vector3(scale_x, scale_y, scale_z);
    }
}
