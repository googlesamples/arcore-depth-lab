//-----------------------------------------------------------------------
// <copyright file="PaintEmitter.cs" company="Google LLC">
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
    private GameObject _root;
    private GameObject _currentStroke;
    private int _currentStrokeId = 0;
    private bool _continuousMode = false;
    private List<GameObject> _gameObjects = new List<GameObject>();

    /// <summary>
    /// Clear all projectiles.
    /// </summary>
    public void Clear()
    {
        foreach (GameObject go in _gameObjects)
        {
            Destroy(go);
        }

        _gameObjects.Clear();
        Destroy(_root);
        _root = new GameObject("Strokes");
    }

    /// <summary>
    /// Drop a projectile.
    /// </summary>
    public void Drop()
    {
        var point =
                Instantiate(PaintPrefab, transform.position, transform.rotation) as GameObject;
        _gameObjects.Add(point);
        point.transform.parent = _root.transform;
    }

    private void Start()
    {
        _root = new GameObject("Strokes");
    }

    private void OnDestroy()
    {
        foreach (GameObject go in _gameObjects)
        {
            Destroy(go);
        }

        _gameObjects.Clear();
        Destroy(_root);
        _root = null;
    }

    private void Update()
    {
        if (!_continuousMode)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && _currentStroke == null)
        {
            _currentStroke = new GameObject("Stroke " + _currentStrokeId);
            _currentStroke.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = _currentStroke.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.allowOcclusionWhenDynamic = true;
            meshRenderer.receiveShadows = false;
            _currentStroke.transform.parent = _root.transform;
            ++_currentStrokeId;
        }

        // When the continuous mode is turned on, combine decals painted on touch up.
        if (_continuousMode && Input.GetMouseButtonUp(0))
        {
            MeshFilter[] meshFilters = _currentStroke.GetComponentsInChildren<MeshFilter>();
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

            _currentStroke.transform.GetComponent<MeshFilter>().mesh = new Mesh();
            _currentStroke.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
            _currentStroke.transform.gameObject.SetActive(true);
            _currentStroke.GetComponent<Renderer>().material =
                meshFilters[1].GetComponent<Renderer>().sharedMaterial;

            _currentStroke = null;
        }

        // When the continuous mode is turned on, paint while the finger presses the screen.
        // Otherwise, only drop a single decal at each screen tap.
        if ((_currentStroke != null && Input.GetMouseButton(0))
                || Input.GetMouseButtonDown(0))
        {
            var point =
                Instantiate(PaintPrefab, transform.position, transform.rotation) as GameObject;
            point.transform.parent = _currentStroke.transform;
        }
    }
}
