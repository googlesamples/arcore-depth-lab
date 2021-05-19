//-----------------------------------------------------------------------
// <copyright file="AvatarController.cs" company="Google LLC">
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

    private const float _distanceToPickUp = 0.6f;
    private const float _lookAtCameraRotationDuration = 0.5f;
    private const float _maxAvatarDistanceInMeter = 0.5f;
    private const float _raycastStartOffsetInMeter = 0.5f;
    private const float _raycastEndOffsetInMeter = -1;
    private const float _raycastStepSizeInMeter = 0.02f;
    private const float _maxChaseDistanceInMeter = 10;
    private const float _maxHeightAbovePawnInMeter = 1f;
    private const float _minimumDistanceChange = 0.005f;

    /// <summary>
    /// This point determines where the avatar should move to next.
    /// </summary>
    private Vector3 _guidePosition = Vector3.negativeInfinity;

    /// <summary>
    /// The list where we keep track of all the cube game objects.
    /// </summary>
    private List<GameObject> _cubeObjects;

    /// <summary>
    /// Adds a new cube object in the scene.
    /// </summary>
    /// <param name="cubeObject">Cube game object.</param>
    public void AddNewCubeObject(GameObject cubeObject)
    {
        _cubeObjects.Add(cubeObject);
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
        _cubeObjects = new List<GameObject>();
    }

    /// <summary>
    /// Moves the avatar towards the cubes on every frame as long as there exists cubes.
    /// </summary>
    private void Update()
    {
        if (_cubeObjects.Count > 0)
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
        if ((myTransform.position - _guidePosition).magnitude > _maxAvatarDistanceInMeter)
        {
            _guidePosition = myTransform.position;
        }

        // Limits the look rotation because the andy's origin is at its feet.
        Vector3 newLookAtPosition = new Vector3(_cubeObjects[0].transform.position.x,
          myTransform.position.y, _cubeObjects[0].transform.position.z);

        myTransform.LookAt(newLookAtPosition, Vector3.up);

        // Limits the new postion in the Y axis, so the andy doesn't go flying everywhere.
        Vector3 newPos = new Vector3(_cubeObjects[0].transform.position.x,
          _cubeObjects[0].transform.position.y, _cubeObjects[0].transform.position.z);

        // Measures the distance to the new position.
        float currentDistance = Vector3.Distance(_guidePosition, newPos);

        float interpolationTime = BaseSpeed * Time.deltaTime;

        Vector3 interpolatedPos = Vector3.MoveTowards(_guidePosition, newPos, interpolationTime);
        Vector3 backupPos = interpolatedPos;

        Vector3 raycastStartPos = interpolatedPos;
        raycastStartPos.y += _raycastStartOffsetInMeter;

        Vector3 raycastEndPos = interpolatedPos;
        raycastEndPos.y += _raycastEndOffsetInMeter;

        // Raycasts from the avatar's position downwards to find a hit point in the depth map.
        raycastStartPos = AvatarNavigationHelper.RaycastDepth(DepthSource.DepthArray,
          raycastStartPos, raycastEndPos, _raycastStepSizeInMeter);

        // Uses the hit point, if a valid hit point has been detected.
        bool isRaycastStartPosInvalid =
            float.IsInfinity(raycastStartPos.x) ||
            float.IsInfinity(raycastStartPos.y) ||
            float.IsInfinity(raycastStartPos.z) ||
            float.IsNaN(raycastStartPos.x) ||
            float.IsNaN(raycastStartPos.y) ||
            float.IsNaN(raycastStartPos.z);
        interpolatedPos = isRaycastStartPosInvalid ? interpolatedPos : raycastStartPos;

        // Never let Andy go over the pawn for more than _maxHeightAbovePawnInMeter.
        if (interpolatedPos.y > newPos.y + _maxHeightAbovePawnInMeter)
        {
            interpolatedPos.y = newPos.y + _maxHeightAbovePawnInMeter;
        }

        // Normalizes the speed of the avatar after adjusting the trajectory based on depth.
        interpolatedPos = Vector3.MoveTowards(_guidePosition, interpolatedPos, interpolationTime);

        float distanceChange = Vector3.Distance(_guidePosition, interpolatedPos);

        if (distanceChange < _minimumDistanceChange)
        {
            interpolatedPos = backupPos;
        }

        _guidePosition = interpolatedPos;

        PositionFilter posFilter = GetComponent<PositionFilter>();
        if (posFilter != null)
        {
            // Smoothes the trajectory of the avatar using a position filter.
            interpolatedPos = posFilter.Filter(interpolatedPos);
        }

        myTransform.position = interpolatedPos;

        currentDistance = Vector3.Distance(interpolatedPos, newPos);

        if (currentDistance < _distanceToPickUp ||
            _cubeObjects[0].transform.position.y < -_maxChaseDistanceInMeter)
        {
            GameObject cubeToRemove = _cubeObjects[0];
            _cubeObjects.Remove(cubeToRemove);
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
        Vector3 LookAtPos = DepthSource.ARCamera.transform.position - transform.position;
        LookAtPos.y = 0;

        Quaternion sourceRotation = transform.rotation;
        while (elapsedTime < _lookAtCameraRotationDuration)
        {
            float progress = elapsedTime / _lookAtCameraRotationDuration;
            var destRotation = Quaternion.LookRotation(LookAtPos);
            transform.rotation = Quaternion.Lerp(sourceRotation, destRotation, progress);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();
    }
}
