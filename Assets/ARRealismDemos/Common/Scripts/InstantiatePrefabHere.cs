//-----------------------------------------------------------------------
// <copyright file="InstantiatePrefabHere.cs" company="Google LLC">
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

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class for handling loading and unloading of demo scene components.
/// </summary>
public class InstantiatePrefabHere : MonoBehaviour
{
    /// <summary>
    /// Reference to the scene component prefab.
    /// Drag a prefab into this field in the Inspector.
    /// </summary>
    public GameObject Prefab;

    private void Awake()
    {
        // This only instantiates the Prefab if the current scene is the active scene.
        if (gameObject.scene == SceneManager.GetActiveScene())
        {
            // Instantiates at position (0, 0, 0) and zero rotation.
            GameObject go = Instantiate(Prefab, Vector3.zero, Quaternion.identity,
                transform.parent);

            // Moves all children to the new prefab instance.
            foreach (Transform child in transform)
            {
                child.SetParent(go.transform, true);
            }

            // Destroys itself.
            Destroy(gameObject);

            Debug.Log("Added " + Prefab.name + " on scene " + gameObject.scene.name);
        }
    }
}
