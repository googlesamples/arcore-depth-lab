//-----------------------------------------------------------------------
// <copyright file="ObjectViewerInteractionController.cs" company="Google LLC">
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
/// Controller for handling user interaction with the placed object.
/// </summary>
public class ObjectViewerInteractionController : MonoBehaviour
{
    // The higher the multiplier is, the more degrees it rotates with the same dragging gesture.
    private const float _rotationMultiplier = 10;
    private GameObject _manipulatedObject;
    private Plane _objectPlane;
    private ManipulationState _manipulationState = ManipulationState.Undefined;
    private Vector3 _initialTouchVector;
    private Quaternion _initialRotation;

    private enum ManipulationState
    {
        Undefined,
        Placed,
        Dragging,
        Rotating
    }

    /// <summary>
    /// Sets the manipulated object.
    /// </summary>
    /// <param name="obj">The object to be manipulated.</param>
    public void SetManipulatedObject(GameObject obj)
    {
        _manipulatedObject = obj;

        var collider = _manipulatedObject.GetComponentInChildren<Collider>(true);
        if (collider != null)
        {
            collider.enabled = true;
        }

        _manipulationState = ManipulationState.Placed;
        _objectPlane = new Plane(Vector3.up, _manipulatedObject.transform.position);
    }

    private void EndCurrentAction()
    {
        switch (_manipulationState)
        {
            case ManipulationState.Dragging:
                _manipulationState = ManipulationState.Placed;
                break;
            case ManipulationState.Rotating:
                _manipulationState = ManipulationState.Placed;
                break;
            case ManipulationState.Placed:
                break;
            case ManipulationState.Undefined:
                break;
        }
    }

    private void Update()
    {
        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        // Don't do anything if the object is not placed yet or no touch has been detected.
        if (_manipulationState == ManipulationState.Undefined || Input.touchCount == 0 ||
            _manipulatedObject == null || !_manipulatedObject.activeInHierarchy)
        {
            EndCurrentAction();
            return;
        }

        Touch touch = Input.GetTouch(0);

        // Screen ray and ray & plane intersection.
        Ray touchRay = Camera.main.ScreenPointToRay(touch.position);
        float enter = 0.0f;
        Vector3? planeHitPoint = null;
        if (_objectPlane.Raycast(touchRay, out enter))
        {
            planeHitPoint = touchRay.GetPoint(enter);
        }

        // Ends the current action when the touch is ended or cancelled.
        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            EndCurrentAction();
            return;
        }
        else if (touch.phase == TouchPhase.Began)
        {
            if (_manipulationState != ManipulationState.Placed)
            {
                EndCurrentAction();
            }

            // Checks for ray intersection with the bounding collider.
            bool colliderHitFound = false;
            RaycastHit colliderHitInfo;
            if (Physics.Raycast(touchRay, out colliderHitInfo))
            {
                var hitTransform = colliderHitInfo.transform;

                // Activates the dragging state when the hit test was successful. (Dragging)
                _manipulatedObject = hitTransform.gameObject;
                _objectPlane = new Plane(Vector3.up, _manipulatedObject.transform.position);
                colliderHitFound = true;
                _manipulationState = ManipulationState.Dragging;
            }

            // Checks for ray intersection with the object's plane. (Rotating)
            if (!colliderHitFound && planeHitPoint.HasValue)
            {
                _manipulationState = ManipulationState.Rotating;
                var touchVector = planeHitPoint.Value - transform.position;
                touchVector.y = 0;
                touchVector.Normalize();
                _initialTouchVector = touchVector;
                _initialRotation = _manipulatedObject.transform.rotation;
                _manipulationState = ManipulationState.Rotating;
            }
        }
        else if (touch.phase == TouchPhase.Moved)
        {
            if (_manipulationState == ManipulationState.Dragging && planeHitPoint.HasValue)
            {
                Vector3 nextPosition = planeHitPoint.Value;

                // Check if next object position will make the object collide with the depth map.
                // If that's the case don't update the object's position.
                Matrix4x4 nextTransform = _manipulatedObject.transform.localToWorldMatrix;
                nextTransform.SetColumn(3,
                  new Vector4(nextPosition.x, nextPosition.y, nextPosition.z, 1.0f));
                float collisionPercentage = SimpleCollisionHelper.TestCollision(
                  _manipulatedObject, nextTransform);
                if (collisionPercentage > 0.1f)
                {
                    return;
                }

                // Sets the object's position to the plane hit point.
                _manipulatedObject.transform.position = nextPosition;
            }
            else if (_manipulationState == ManipulationState.Rotating)
            {
                var touchVector = planeHitPoint.Value - transform.position;
                touchVector.y = 0;
                touchVector.Normalize();

                var newRotation = Quaternion.FromToRotation(touchVector, _initialTouchVector);

                float angle;
                Vector3 axis;
                newRotation.ToAngleAxis(out angle, out axis);
                newRotation = Quaternion.AngleAxis(angle * _rotationMultiplier, axis);

                _manipulatedObject.transform.rotation = _initialRotation * newRotation;
            }
        }
    }
}
