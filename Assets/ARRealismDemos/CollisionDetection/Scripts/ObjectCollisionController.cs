//-----------------------------------------------------------------------
// <copyright file="ObjectCollisionController.cs" company="Google LLC">
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
using GoogleARCore;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Results of a single collision test.
/// </summary>
public enum CollisionResults
{
    /// <summary>
    /// Invalid depth.
    /// </summary>
    InvalidDepth = -1,

    /// <summary>
    /// No collision.
    /// </summary>
    NoCollision = 0,

    /// <summary>
    /// Valid collision.
    /// </summary>
    Collided = 1,
}

/// <summary>
/// Manages the collision event of a virtual game object. Allows the user to use the game object's
/// bouding box, a proxy (simplified) mesh, or the entire mesh (including submeshes) to represent
/// the object.
/// </summary>
public class ObjectCollisionController : MonoBehaviour
{
    /// <summary>
    /// Reticle tilt checker.
    /// </summary>
    public CheckReticleOrientation ReticleTiltChecker;

    /// <summary>
    /// Flag to enable or disable object manipulation.
    /// </summary>
    [HideInInspector]
    public bool EnableAvatarManipulation;

    /// <summary>
    /// Flag to enable or disable collision checking.
    /// </summary>
    [HideInInspector]
    public bool EnableCollisionChecking = true;

    /// <summary>
    /// Type of depth texture to attach to the material.
    /// </summary>
    public bool UseSparseDepth = false;

    /// <summary>
    /// Whether to use the mesh's bounding box to represent the collider instead of usinsg
    /// the entire mesh vertices.
    /// </summary>
    public bool UseBoundingBox = false;

    /// <summary>
    /// Whether to use a mesh proxy to represent the collider instead of using the entire mesh
    /// vertices. Note the using the entire mesh vertices may trmendously slow down the
    /// application.
    /// </summary>
    public bool UseColliderProxy = false;

    /// <summary>
    /// A simplified collider proxy for detecting collision.
    /// </summary>
    public GameObject ColliderProxy;

    /// <summary>
    /// High-bound ratio of vertices required for a valid collision detection.
    /// </summary>
    [Tooltip("High-bound ratio of vertices required for a valid collision detection."),
     Range(0f, 1f)]
    public float CollisionPercentageHighBound = 0.2f;

    /// <summary>
    /// Minimum ratio of vertices required for a valid collision detection.
    /// </summary>
    [Tooltip("Minimum ratio of vertices required for a valid collision detection."),
     Range(0f, 1f)]
    public float CollisionPercentageLowBound = 0.1f;

    /// <summary>
    /// Minimum depth difference in meters that a vertex is judged as collided.
    /// </summary>
    [Tooltip("Minimum depth difference in meters that a vertex is judged as collided."),
     Range(0f, 0.5f)]
    public float VertexCollisionThresholdInMeters = 0.12f;

    /// <summary>
    /// Frame rate of the collision detection.
    /// </summary>
    [Tooltip("Frame rate of the collision detection."),
     Range(0f, 60f)]
    public float CollisionDetectionFps = 10f;

    /// <summary>
    /// Whether to check if the reticle's up vector is tilted larger than 90 degrees from (0, 1, 0).
    /// </summary>
    public bool CheckReticleUpVector = true;

    private const float k_LookAtCameraRotationDuration = 0.5f;
    private Vector3[] m_SelectedVerticesList;

    /// <summary>
    /// The script to handle collision event.
    /// </summary>
    private CollisionEventInterface m_CollisionEvent;

    private float m_LastCollisionCheckTimestamp = 0f;

    /// <summary>
    /// Get all vertices of a mesh in the local coordinate system for at most two hierachies.
    /// Tested: Real-time performance with GetRawTexture<short> optimization for over 1K vertices.
    /// </summary>
    /// <param name="targetObject">The target object.</param>
    /// <returns>A list of vertices local positions in Vector3.</returns>
    public static Vector3[] GetVerticesInChildren(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return null;
        }

        var meshFilters = targetObject.GetComponentsInChildren<MeshFilter>();
        var vertices = new List<Vector3>();
        foreach (var meshFilter in meshFilters)
        {
            var localVertices = new Vector3[meshFilter.mesh.vertices.Length];
            for (int i = 0; i < localVertices.Length; ++i)
            {
                localVertices[i] = meshFilter.transform.TransformPoint(meshFilter.mesh.vertices[i]);
            }

            vertices.AddRange(localVertices);
        }

        return vertices.ToArray();
    }

    /// <summary>
    /// Stops the "look at camera" animation if we have a new place to look.
    /// </summary>
    public void StopLookAtCamera()
    {
        StopCoroutine("LookAtCameraCoroutine");
    }

    /// <summary>
    /// Updates the vertices list.
    /// </summary>
    public void UpdateVerticesList()
    {
        // Sets the collider to a proxy mesh or the original game object.
        var colliderMesh = UseColliderProxy ? ColliderProxy : gameObject;

        if (colliderMesh == null)
        {
            Debug.LogError("Collider does not exist.");
            return;
        }

        if (UseBoundingBox)
        {
            m_SelectedVerticesList = Utilities.GetBoundingBoxVertices(colliderMesh);
        }
        else
        {
            m_SelectedVerticesList = GetVerticesInChildren(colliderMesh);
        }
    }

    /// <summary>
    /// Initializes the object collision event.
    /// </summary>
    private void Start()
    {
        m_CollisionEvent = gameObject.GetComponent<ObjectCollisionEvent>();
    }

    /// <summary>
    /// Moves the avatar towards the cubes on every frame as long as there exists cubes.
    /// </summary>
    private void Update()
    {
        if (EnableCollisionChecking &&
            Time.time - m_LastCollisionCheckTimestamp > 1f / CollisionDetectionFps / 1000f)
        {
            m_LastCollisionCheckTimestamp = Time.time;
            CheckCollisionState();
        }
    }

    /// <summary>
    /// Checks the collision state of the attached game object.
    /// </summary>
    private void CheckCollisionState()
    {
        var myTransform = EnableAvatarManipulation ? transform.parent.transform : transform;

        var collisionPercentage = TestCollisionOnMesh(myTransform.position);

        if (CheckReticleUpVector)
        {
            // Indicates maximum collision when the reticle is tilted and there pointing to
            // a vertical surfaces.
            if (ReticleTiltChecker != null && ReticleTiltChecker.ReticleTilted)
            {
                collisionPercentage = 1;
            }
        }

        if (collisionPercentage > CollisionPercentageLowBound)
        {
            m_CollisionEvent.Trigger(gameObject, collisionPercentage);
        }
        else
        {
            m_CollisionEvent.Stop(gameObject, collisionPercentage);
        }
    }

    /// <summary>
    /// Tests if a collider at worldPosition is behind the physical environment in O(N),
    /// where N is the total number of vertices in the collider mesh.
    /// </summary>
    /// <param name="targetWorldPosition">The position of the virtual object in the world space.
    /// </param>
    /// <returns>Collision percentage at targetWorldPosition.</returns>
    private float TestCollisionOnMesh(Vector3 targetWorldPosition)
    {
        // Retrieves all vertices of the collider mesh.
        UpdateVerticesList();

        // Reports no collision if the mesh or proxy is empty.
        if (m_SelectedVerticesList == null)
        {
            return 0;
        }

        int numCollision = 0;
        float collisionPercentage = 0;
        int totalTests = m_SelectedVerticesList.Length;

        // Tests every single vertex of the mesh and gets the statistics of the results.
        foreach (var vertex in m_SelectedVerticesList)
        {
            var result = TestCollisionOnVertex(vertex);
            switch (result)
            {
                case CollisionResults.Collided:
                    ++numCollision;
                    break;
                case CollisionResults.InvalidDepth:
                    --totalTests;
                    break;
            }
        }

        // Reports no collision if the depth map is empty.
        if (totalTests <= 0)
        {
            return 0;
        }

        collisionPercentage = (float)numCollision / totalTests;
        return collisionPercentage;
    }

    /// <summary>
    /// Tests if worldPosition is behind the physical environment in O(1).
    /// </summary>
    /// <param name="targetWorldPosition">The position of the virtual object in the world space.
    /// </param>
    /// <returns>True if collision occurs.</returns>
    private CollisionResults TestCollisionOnVertex(Vector3 targetWorldPosition)
    {
        // Computes the environment's depth.
        var screenPosition = Camera.main.WorldToScreenPoint(targetWorldPosition);
        var screenUv = new Vector2(screenPosition.x / Screen.width,
                                   screenPosition.y / Screen.height);
        var depthUv = DepthSource.ScreenToDepthUV(screenUv);
        var environmentDepth = DepthSource.GetDepthFromUV(depthUv, DepthSource.DepthArray);

        if (environmentDepth == DepthSource.InvalidDepthValue)
        {
            return CollisionResults.InvalidDepth;
        }

        // Computes the virtual object's depth.
        var targetDepth = screenPosition.z;

        return targetDepth >
               environmentDepth + VertexCollisionThresholdInMeters
               ? CollisionResults.Collided : CollisionResults.NoCollision;
    }

    /// <summary>
    /// Starts coroutine to make andy look at camera once it has gotten the projectile.
    /// </summary>
    private void LookAtCamera()
    {
        StartCoroutine("LookAtCameraCoroutine");
    }

    /// <summary>
    /// The Coroutine that animates andy to look at the camera.
    /// </summary>
    /// <returns>Animation time.</returns>
    private IEnumerator LookAtCameraCoroutine()
    {
        float elapsedTime = 0f;

        // Limit the look rotation because our andy's origin is at its feet.
        Vector3 LookAtPos = Camera.main.transform.position - transform.position;
        LookAtPos.y = 0;

        Quaternion sourceRotation = transform.rotation;
        while (elapsedTime < k_LookAtCameraRotationDuration)
        {
            float progress = elapsedTime / k_LookAtCameraRotationDuration;
            transform.rotation = Quaternion.Lerp(sourceRotation, Quaternion.LookRotation(LookAtPos),
              progress);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();
    }
}
