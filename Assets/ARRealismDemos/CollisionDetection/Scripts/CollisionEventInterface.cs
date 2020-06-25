//-----------------------------------------------------------------------
// <copyright file="CollisionEventInterface.cs" company="Google LLC">
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
/// Interface to handle all collision events.
/// </summary>
public abstract class CollisionEventInterface : MonoBehaviour
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
    /// Returns whether the selected object is in collision.
    /// </summary>
    /// <returns>Whether the object is being collided with the environment.</returns>
    public abstract bool IsTriggering();

    /// <summary>
    /// Stops the collision event.
    /// </summary>
    /// <param name="collidedObject">The tested object.</param>
    /// <param name="collisionPercerntage">Percentage of the collision.</param>
    public abstract void Stop(GameObject collidedObject, float collisionPercerntage = 1f);

    /// <summary>
    /// Trigger the collision event.
    /// </summary>
    /// <param name="collidedObject">The object collided with the environment.</param>
    /// <param name="collisionPercerntage">Percentage of the collision.</param>
    public abstract void Trigger(GameObject collidedObject, float collisionPercerntage = 1f);
}
