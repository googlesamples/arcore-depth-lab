//-----------------------------------------------------------------------
// <copyright file="ReimportShaders.cs" company="Google LLC">
//
// Copyright 2020 Google Inc. All Rights Reserved.
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
using UnityEditor;
using UnityEngine;

/// <summary>
/// This script reimports all demo shader resources after the ARCoreDepth.cginc shader in ARCore SDK
/// for Unity has been loaded into the project. The user may have to manually reimport all demo
/// shaders without this script.
/// </summary>
public class ReimportShaders : AssetPostprocessor
{
    private static readonly string k_ARCoreDepthShaderName = "ARCoreDepth.cginc";
    private static readonly string k_ARCoreDepthSamplesPath = "Assets/ARRealismDemos";

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string str in importedAssets)
        {
            if (str.EndsWith(k_ARCoreDepthShaderName))
            {
                string[] assetsPaths = { k_ARCoreDepthSamplesPath };
                string[] demoShaders = AssetDatabase.FindAssets("t:Shader", assetsPaths);

                foreach (string shaderGUID in demoShaders)
                {
                    var shaderPath = AssetDatabase.GUIDToAssetPath(shaderGUID);
                    AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ImportRecursive);
                    Debug.Log("Reimported shader: " + shaderPath);
                }

                Debug.Log("Number of reimported demo shdaers: " + demoShaders.Length);
                break;
            }
        }
    }
}
