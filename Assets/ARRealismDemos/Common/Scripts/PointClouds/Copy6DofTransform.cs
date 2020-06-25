//-----------------------------------------------------------------------
// <copyright file="Copy6DofTransform.cs" company="Google LLC">
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
/// Applies the transform of a referenced transform.
/// </summary>
public class Copy6DofTransform : MonoBehaviour
{
    /// <summary>
    /// The source of the transform to copy.
    /// </summary>
    public Transform TransformSource;

    private void Update()
    {
        if (TransformSource != null)
        {
            transform.position = TransformSource.position;
            transform.rotation = TransformSource.rotation;
        }
    }
}
