//-----------------------------------------------------------------------
// <copyright file="PaintEmitter.cs" company="Google LLC">
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
/// Simulates a Paint particle effect, but using oriented prefabs. While
/// the mouse is down a prefab is generated every frame, when the operation
/// finishes the spawned prefabs are combined into a single one.
/// </summary>
public class PaintEmitter : MonoBehaviour
{
    /// <summary>
    /// The prefab that will be spawned every frame.
    /// </summary>
    public GameObject PaintPrefab;
    private GameObject m_Root;
    private GameObject m_CurrentStroke;
    private int m_CurrentStrokeId = 0;
    private bool m_ContinuousMode = false;
    private List<GameObject> m_GameObjects = new List<GameObject>();

    /// <summary>
    /// Clear all projectiles.
    /// </summary>
    public void Clear()
    {
        foreach (GameObject go in m_GameObjects)
        {
            Destroy(go);
        }

        m_GameObjects.Clear();
        Destroy(m_Root);
        m_Root = new GameObject("Strokes");
    }

    /// <summary>
    /// Drop a projectile.
    /// </summary>
    public void Drop()
    {
        var point =
                Instantiate(PaintPrefab, transform.position, transform.rotation) as GameObject;
        m_GameObjects.Add(point);
        point.transform.parent = m_Root.transform;
    }

    private void Start()
    {
        m_Root = new GameObject("Strokes");
    }

    private void OnDestroy()
    {
        foreach (GameObject go in m_GameObjects)
        {
            Destroy(go);
        }

        m_GameObjects.Clear();
        Destroy(m_Root);
        m_Root = null;
    }

    private void Update()
    {
        if (!m_ContinuousMode)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && m_CurrentStroke == null)
        {
            m_CurrentStroke = new GameObject("Stroke " + m_CurrentStrokeId);
            m_CurrentStroke.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = m_CurrentStroke.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.allowOcclusionWhenDynamic = true;
            meshRenderer.receiveShadows = false;
            m_CurrentStroke.transform.parent = m_Root.transform;
            ++m_CurrentStrokeId;
        }

        // When the continuous mode is turned on, combine decals painted on touch up.
        if (m_ContinuousMode && Input.GetMouseButtonUp(0))
        {
            MeshFilter[] meshFilters = m_CurrentStroke.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length <= 1)
            {
                return;
            }

            CombineInstance[] combine = new CombineInstance[meshFilters.Length - 1];

            int combinedMeshFilterIndex = 0;
            for (int i = 0; i < meshFilters.Length; ++i)
            {
                if (meshFilters[i].sharedMesh != null)
                {
                    combine[combinedMeshFilterIndex].mesh = meshFilters[i].sharedMesh;
                    combine[combinedMeshFilterIndex].transform =
                        meshFilters[i].transform.localToWorldMatrix;
                    Object.Destroy(meshFilters[i].gameObject);
                    ++combinedMeshFilterIndex;
                }
            }

            m_CurrentStroke.transform.GetComponent<MeshFilter>().mesh = new Mesh();
            m_CurrentStroke.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
            m_CurrentStroke.transform.gameObject.SetActive(true);
            m_CurrentStroke.GetComponent<Renderer>().material =
                meshFilters[1].GetComponent<Renderer>().sharedMaterial;

            m_CurrentStroke = null;
        }

        // When the continuous mode is turned on, paint while the finger presses the screen.
        // Otherwise, only drop a single decal at each screen tap.
        if ((m_CurrentStroke != null && Input.GetMouseButton(0))
                || Input.GetMouseButtonDown(0))
        {
            var point =
                Instantiate(PaintPrefab, transform.position, transform.rotation) as GameObject;
            point.transform.parent = m_CurrentStroke.transform;
        }
    }
}
