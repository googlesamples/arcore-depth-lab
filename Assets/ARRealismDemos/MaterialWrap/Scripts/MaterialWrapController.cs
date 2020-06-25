//-----------------------------------------------------------------------
// <copyright file="MaterialWrapController.cs" company="Google LLC">
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
/// Creates a dynamically updating triangle mesh object from the depth texture.
/// </summary>
public class MaterialWrapController : MonoBehaviour
{
    /// <summary>
    /// Prefab of MaterialWrap event.
    /// </summary>
    public GameObject MaterialWrapPrefab;

    /// <summary>
    /// Array of materials to cycle through.
    /// </summary>
    public Material[] WrapMaterials;

    /// <summary>
    /// Controls enabling/disabling the "Clear" button.
    /// </summary>
    public ButtonInteraction ClearButton;

    private int m_MaterialIndex;
    private int m_MaterialCount;

    /// <summary>
    /// Starts MaterialWrap instantiation coroutine.
    /// </summary>
    public void CreateMaterialWrap()
    {
        StartCoroutine(MaterialWrapCorountine());
    }

    /// <summary>
    /// Destroys all MaterialWrap meshes from the scene.
    /// </summary>
    public void ClearAllMeshes()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.tag == "MaterialWrap")
                {
                    GameObject.Destroy(child.gameObject);
                }
        }

        m_MaterialCount = 0;
    }

    /// <summary>
    /// Sets wrap material selection integer.
    /// </summary>
    /// <param name="i">Wrap material index.</param>
    public void SetWrapMaterial(int i)
    {
        m_MaterialIndex = i;
    }

    private IEnumerator MaterialWrapCorountine()
    {
        GameObject newTouch = Instantiate(MaterialWrapPrefab, Vector3.zero, Quaternion.identity);
        newTouch.GetComponent<MeshRenderer>().material = WrapMaterials[m_MaterialIndex];
        newTouch.transform.parent = transform;
        yield return null;
        newTouch.SetActive(true);
        m_MaterialCount++;
    }

    private void Start()
    {
        m_MaterialIndex = 0;
        m_MaterialCount = 0;
    }

    private void Update()
    {
        if (m_MaterialCount == 0)
        {
            ClearButton.DisableButton();
        }
        else
        {
            ClearButton.EnableButton();
        }
    }
}
