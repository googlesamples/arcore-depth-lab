//-----------------------------------------------------------------------
// <copyright file="DepthDataSourceConfig.cs" company="Google LLC">
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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the name of the depth data source class.
/// </summary>
[CreateAssetMenu(
    fileName = "DepthDataSourceConfig", menuName = "Depth Lab/Depth Data Source Config", order = 1)]
public class DepthDataSourceConfig : ScriptableObject
{
    [Header("Depth Data Source")]

    /// <summary>
    /// Assembly qualified class name of the depth data source implementing the IDepthDataSource
    /// interface.
    /// </summary>
    [Tooltip("Assembly qualified class name of the depth data source implementing the" +
             "IDepthDataSource interface.")]
    public string DepthSourceClassName;

    /// <summary>
    /// Instantiated depth data source.
    /// </summary>
    public IDepthDataSource DepthDataSource;

    /// <summary>
    /// Unity OnValidate.
    /// </summary>
    public void Awake()
    {
        Type type = Type.GetType(DepthSourceClassName);
        DepthDataSource = (IDepthDataSource)Activator.CreateInstance(type);
    }
}
