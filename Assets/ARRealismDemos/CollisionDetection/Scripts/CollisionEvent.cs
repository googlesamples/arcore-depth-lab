//-----------------------------------------------------------------------
// <copyright file="CollisionEvent.cs" company="Google LLC">
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
/// Handles all collision events. In this example, we rotate the Andy quickly to indicate collision.
/// </summary>
public class CollisionEvent : MonoBehaviour
{
    /// <summary>
    /// Whether or not to enable the collision animation.
    /// </summary>
    public bool EnableAnimation = true;

    /// <summary>
    /// The duration of the collision animation.
    /// </summary>
    public float AnimationInSeconds = 0.5f;

    /// <summary>
    /// Number of circles to rotate when collision happens.
    /// </summary>
    public float RotationNumCircles = 3f;

    private const float _degreesPerCircle = 360f;

    // The current manipulated object.
    private GameObject _manipulatedObject;

    // Progress in precentage of the collision animation.
    // Rotates the model when value smaller than 1.
    private float _collisionAnimationProgress = 1f;

    /// <summary>
    /// Returns whether the selected object is in collision.
    /// </summary>
    /// <returns>Whether the object is being collided with the environment.</returns>
    public bool IsTriggering()
    {
        return _collisionAnimationProgress < 1f;
    }

    /// <summary>
    /// Trigger the collision event.
    /// </summary>
    /// <param name="collidedObject">The object collided with the environment.</param>
    public void Trigger(GameObject collidedObject)
    {
        _manipulatedObject = collidedObject;
        _collisionAnimationProgress = 0f;
    }

    /// <summary>
    /// Updates the collision animiation every frame: rotates Andy when collision occurs.
    /// </summary>
    protected void Update()
    {
        if (!IsTriggering() || _manipulatedObject == null)
        {
            return;
        }

        _collisionAnimationProgress += Time.deltaTime / AnimationInSeconds;

        if (_manipulatedObject != null && EnableAnimation)
        {
            float rotationDegrees = Time.deltaTime * _degreesPerCircle * RotationNumCircles;
            _manipulatedObject.transform.Rotate(0, rotationDegrees, 0, Space.Self);
        }
    }
}
