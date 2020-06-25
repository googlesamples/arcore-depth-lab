//-----------------------------------------------------------------------
// <copyright file="AvatarController.cs" company="Google LLC">
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
using UnityEngine.Serialization;

/// <summary>
/// Manages the movement of Andy.
/// </summary>
public class AvatarController : MonoBehaviour
{
    /// <summary>
    /// The translation speed of andy.
    /// </summary>
    [FormerlySerializedAs("baseSpeed")]
    public float BaseSpeed = .1f;

    /// <summary>
    /// Flag to enable or disable Avatar manipulation.
    /// </summary>
    [HideInInspector]
    public bool EnableAvatarManipulation;

    private const float k_DistanceToPickUp = 0.6f;
    private const float k_LookAtCameraRotationDuration = 0.5f;
    private const float k_MaxAvatarDistanceInMeter = 0.5f;
    private const float k_RaycastStartOffsetInMeter = 0.5f;
    private const float k_RaycastEndOffsetInMeter = -1;
    private const float k_RaycastStepSizeInMeter = 0.02f;
    private const float k_MaxChaseDistanceInMeter = 10;
    private const float k_MaxHeightAbovePawnInMeter = 1f;
    private const float k_MinimumDistanceChange = 0.005f;

    /// <summary>
    /// This point determines where the avatar should move to next.
    /// </summary>
    private Vector3 m_GuidePosition = Vector3.negativeInfinity;

    /// <summary>
    /// The list where we keep track of all the cube game objects.
    /// </summary>
    private List<GameObject> m_CubeObjects;

    /// <summary>
    /// Adds a new cube object in the scene.
    /// </summary>
    /// <param name="cubeObject">Cube game object.</param>
    public void AddNewCubeObject(GameObject cubeObject)
    {
        m_CubeObjects.Add(cubeObject);
    }

    /// <summary>
    /// Stops the "look at camera" animation if we have a new place to look.
    /// </summary>
    public void StopLookAtCamera()
    {
        StopCoroutine("LookAtCameraCoroutine");
    }

    /// <summary>
    /// Initializes the cubes.
    /// </summary>
    private void Start()
    {
        m_CubeObjects = new List<GameObject>();
    }

    /// <summary>
    /// Moves the avatar towards the cubes on every frame as long as there exists cubes.
    /// </summary>
    private void Update()
    {
        if (m_CubeObjects.Count > 0)
        {
            MoveTowardCubes();
        }
    }

    /// <summary>
    /// Moves the avatar towards the cubes.
    /// </summary>
    private void MoveTowardCubes()
    {
        var myTransform = EnableAvatarManipulation ? transform.parent.transform : transform;

        // Resets the guide position whenever the avatar has moved by a significant distance.
        if ((myTransform.position - m_GuidePosition).magnitude > k_MaxAvatarDistanceInMeter)
        {
            m_GuidePosition = myTransform.position;
        }

        // Limits the look rotation because the andy's origin is at its feet.
        Vector3 newLookAtPosition = new Vector3(m_CubeObjects[0].transform.position.x,
          myTransform.position.y, m_CubeObjects[0].transform.position.z);

        myTransform.LookAt(newLookAtPosition, Vector3.up);

        // Limits the new postion in the Y axis, so the andy doesn't go flying everywhere.
        Vector3 newPos = new Vector3(m_CubeObjects[0].transform.position.x,
          m_CubeObjects[0].transform.position.y, m_CubeObjects[0].transform.position.z);

        // Measures the distance to the new position.
        float currentDistance = Vector3.Distance(m_GuidePosition, newPos);

        float interpolationTime = BaseSpeed * Time.deltaTime;

        Vector3 interpolatedPos = Vector3.MoveTowards(m_GuidePosition, newPos, interpolationTime);
        Vector3 backupPos = interpolatedPos;

        Vector3 raycastStartPos = interpolatedPos;
        raycastStartPos.y += k_RaycastStartOffsetInMeter;

        Vector3 raycastEndPos = interpolatedPos;
        raycastEndPos.y += k_RaycastEndOffsetInMeter;

        // Raycasts from the avatar's position downwards to find a hit point in the depth map.
        raycastStartPos = AvatarNavigationHelper.RaycastDepth(DepthSource.DepthArray,
          raycastStartPos, raycastEndPos, k_RaycastStepSizeInMeter);

        // Uses the hit point, if a valid hit point has been detected.
        bool isRaycastStartPosInvalid =
            float.IsInfinity(raycastStartPos.x) ||
            float.IsInfinity(raycastStartPos.y) ||
            float.IsInfinity(raycastStartPos.z) ||
            float.IsNaN(raycastStartPos.x) ||
            float.IsNaN(raycastStartPos.y) ||
            float.IsNaN(raycastStartPos.z);
        interpolatedPos = isRaycastStartPosInvalid ? interpolatedPos : raycastStartPos;

        // Never let Andy go over the pawn for more than k_MaxHeightAbovePawnInMeter.
        if (interpolatedPos.y > newPos.y + k_MaxHeightAbovePawnInMeter)
        {
            interpolatedPos.y = newPos.y + k_MaxHeightAbovePawnInMeter;
        }

        // Normalizes the speed of the avatar after adjusting the trajectory based on depth.
        interpolatedPos = Vector3.MoveTowards(m_GuidePosition, interpolatedPos, interpolationTime);

        float distanceChange = Vector3.Distance(m_GuidePosition, interpolatedPos);

        if (distanceChange < k_MinimumDistanceChange)
        {
            interpolatedPos = backupPos;
        }

        m_GuidePosition = interpolatedPos;

        PositionFilter posFilter = GetComponent<PositionFilter>();
        if (posFilter != null)
        {
            // Smoothes the trajectory of the avatar using a position filter.
            interpolatedPos = posFilter.Filter(interpolatedPos);
        }

        myTransform.position = interpolatedPos;

        currentDistance = Vector3.Distance(interpolatedPos, newPos);

        if (currentDistance < k_DistanceToPickUp ||
            m_CubeObjects[0].transform.position.y < -k_MaxChaseDistanceInMeter)
        {
            GameObject cubeToRemove = m_CubeObjects[0];
            m_CubeObjects.Remove(cubeToRemove);
            Destroy(cubeToRemove);
            currentDistance = 0;
            LookAtCamera();
        }
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

        // Limits the look rotation because our Andy's origin is at its feet.
        Vector3 LookAtPos = Camera.main.transform.position - transform.position;
        LookAtPos.y = 0;

        Quaternion sourceRotation = transform.rotation;
        while (elapsedTime < k_LookAtCameraRotationDuration)
        {
            float progress = elapsedTime / k_LookAtCameraRotationDuration;
            var destRotation = Quaternion.LookRotation(LookAtPos);
            transform.rotation = Quaternion.Lerp(sourceRotation, destRotation, progress);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();
    }
}
