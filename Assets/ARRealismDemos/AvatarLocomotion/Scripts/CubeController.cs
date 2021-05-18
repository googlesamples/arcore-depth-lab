//-----------------------------------------------------------------------
// <copyright file="CubeController.cs" company="Google LLC">
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

/// <summary>
/// Manages cubes for Andy to catch.
/// </summary>
public class CubeController : MonoBehaviour
{
    private AvatarController _android;

    /// <summary>
    /// Determines if this pickable can be added to the pickable list.
    /// </summary>
    private bool _canBeAddedToPickableList = true;

    private void Start()
    {
        GameObject andy = GameObject.FindGameObjectWithTag("Andy");
        _android = andy.GetComponentInChildren<AvatarController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_canBeAddedToPickableList)
        {
            _android.AddNewCubeObject(gameObject);
            _canBeAddedToPickableList = false;
        }
    }
}
