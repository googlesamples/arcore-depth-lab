//-----------------------------------------------------------------------
// <copyright file="CollisionEvent.cs" company="Google LLC">
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

    private const float k_DegreesPerCircle = 360f;

    // The current manipulated object.
    private GameObject m_ManipulatedObject;

    // Progress in precentage of the collision animation.
    // Rotates the model when value smaller than 1.
    private float m_CollisionAnimationProgress = 1f;

    /// <summary>
    /// Returns whether the selected object is in collision.
    /// </summary>
    /// <returns>Whether the object is being collided with the environment.</returns>
    public bool IsTriggering()
    {
        return m_CollisionAnimationProgress < 1f;
    }

    /// <summary>
    /// Trigger the collision event.
    /// </summary>
    /// <param name="collidedObject">The object collided with the environment.</param>
    public void Trigger(GameObject collidedObject)
    {
        m_ManipulatedObject = collidedObject;
        m_CollisionAnimationProgress = 0f;
    }

    /// <summary>
    /// Updates the collision animiation every frame: rotates Andy when collision occurs.
    /// </summary>
    protected void Update()
    {
        if (!IsTriggering() || m_ManipulatedObject == null)
        {
            return;
        }

        m_CollisionAnimationProgress += Time.deltaTime / AnimationInSeconds;

        if (m_ManipulatedObject != null && EnableAnimation)
        {
            float rotationDegrees = Time.deltaTime * k_DegreesPerCircle * RotationNumCircles;
            m_ManipulatedObject.transform.Rotate(0, rotationDegrees, 0, Space.Self);
        }
    }
}
