//-----------------------------------------------------------------------
// <copyright file="KillObject.cs" company="Google LLC">
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
/// Kills the object after some time.
/// </summary>
public class KillObject : MonoBehaviour
{
    /// <summary>
    /// Delay =in seconds.
    /// </summary>
    public float DelayInSeconds = 1;

    // Start is called before the first frame update
    private void Start()
    {
        StartCoroutine("KillAfterTime");
    }

    private IEnumerator KillAfterTime()
    {
        yield return new WaitForSeconds(DelayInSeconds);
        Destroy(gameObject);
    }
}
