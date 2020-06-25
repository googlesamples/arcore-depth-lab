//-----------------------------------------------------------------------
// <copyright file="ObjectCollisionAwarePlacementManager.cs" company="Google LLC">
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
using UnityEngine.UI;

/// <summary>
/// Controls the demo flow of hovering Andy.
/// </summary>
public class ObjectCollisionAwarePlacementManager : MonoBehaviour
{
    /// <summary>
    /// References the 3D cursor.
    /// </summary>
    public GameObject DepthCursor;

    /// <summary>
    /// References the object to place.
    /// </summary>
    public GameObject[] ObjectPrefabs;

    private const float k_AvatarOffsetMeters = 0.015f;
    private ObjectViewerInteractionController m_InteractionController;
    private int m_CurrentPrefabId = 0;
    private GameObject m_Root;

    /// <summary>
    /// Places the object in the current position.
    /// </summary>
    public void PlaceModel()
    {
        // Instantiates and clones the transform.
        var currentTransform = GetCurrentModel().transform;
        var newPosition = currentTransform.position;
        var newModel = Instantiate(GetCurrentModel(), newPosition, currentTransform.rotation,
            m_Root.transform);

        if (m_InteractionController != null)
        {
            m_InteractionController.SetManipulatedObject(newModel);
        }

        newModel.GetComponent<ObjectCollisionController>().enabled = false;
        newModel.GetComponent<ObjectCollisionEvent>().enabled = false;

        // Clones the material for each submesh.
        var oldRenderers = GetCurrentModel().GetComponentsInChildren<Renderer>();
        var newRenderers = newModel.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < oldRenderers.Length; ++i)
        {
            newRenderers[i].material = new Material(oldRenderers[i].material);
            newRenderers[i].gameObject.AddComponent(typeof(DepthTarget));
        }

        SetPrefabVisibility(m_CurrentPrefabId, false);
    }

    /// <summary>
    /// Rotates the object clockwise by degrees.
    /// </summary>
    /// <param name="degrees">Rotation degrees.</param>
    public void RotateModel(float degrees)
    {
        // Rotates the object clockwise by degrees around its up vector.
        GetCurrentModel().transform.rotation *= Quaternion.Euler(0, degrees, 0);
    }

    /// <summary>
    /// Switches to the next model.
    /// </summary>
    public void SwitchToNextModel()
    {
        m_CurrentPrefabId = (m_CurrentPrefabId + 1) % ObjectPrefabs.Length;
        HideInactiveModels();

        var placeText = GameObject.Find("PlaceButton").gameObject.GetComponentInChildren<Text>();
        placeText.text = "Place Object";
    }

    /// <summary>
    /// Clears all placed models.
    /// </summary>
    public void ClearModels()
    {
        if (m_Root != null)
        {
            foreach (Transform child in m_Root.transform)
            {
                Destroy(child.gameObject);
            }
        }

        SetPrefabVisibility(m_CurrentPrefabId, true);
    }

    private void Start()
    {
        m_Root = new GameObject("Colliders");
        m_InteractionController = GetComponent<ObjectViewerInteractionController>();

        HideInactiveModels();
    }

    private void HideInactiveModels()
    {
        for (int i = 0; i < ObjectPrefabs.Length; ++i)
        {
            SetPrefabVisibility(i, i == m_CurrentPrefabId);
        }

        // Updates the vertices list for faster collision checking.
        GetCurrentModel().GetComponent<ObjectCollisionController>().UpdateVerticesList();
    }

    private void SetPrefabVisibility(int prefabId, bool isActive)
    {
        var renderers = ObjectPrefabs[prefabId].GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = isActive;
        }

        ObjectPrefabs[prefabId].GetComponent<ObjectCollisionController>().enabled = isActive;
        ObjectPrefabs[prefabId].GetComponent<ObjectCollisionEvent>().enabled = isActive;
    }

    private GameObject GetCurrentModel()
    {
        return ObjectPrefabs[m_CurrentPrefabId];
    }

    private void Update()
    {
        Vector3 toCamera = Camera.main.transform.position - DepthCursor.transform.position;
        toCamera.Normalize();

        GetCurrentModel().transform.position = DepthCursor.transform.position +
            (toCamera * k_AvatarOffsetMeters);
    }

    private void OnDestroy()
    {
        if (m_Root != null)
        {
            Destroy(m_Root);
        }

        m_Root = null;
    }
}
