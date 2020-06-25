//-----------------------------------------------------------------------
// <copyright file="ObjectCollisionEvent.cs" company="Google LLC">
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
using UnityEngine.UI;

/// <summary>
/// Handles all collision events. In this example, we rotate the Andy quickly to indicate collision.
/// </summary>
public class ObjectCollisionEvent : CollisionEventInterface
{
    /// <summary>
    /// The button to place the AR object.
    /// </summary>
    public ButtonInteraction PlaceButton;

    /// <summary>
    /// The button to rotate the AR object.
    /// </summary>
    public ButtonInteraction RotateButton;

    /// <summary>
    /// The original color of the object to be placed.
    /// </summary>
    public Color OriginalColor = Color.white;

    /// <summary>
    /// Percentage of collided vertices when we trigger Stop().
    /// </summary>
    private const float k_LowBound = 0.02f;

    /// <summary>
    /// Percentage of collided vertices when we show orange collision.
    /// </summary>
    private const float k_MidBound = 0.2f;

    /// <summary>
    /// Percentage of collided vertices when we show red collision.
    /// </summary>
    private const float k_HighBound = 0.8f;
    private static readonly Color k_OrangeColor = new Color(1f, 0.5f, 0f, 1f);

    // The current manipulated object.
    private GameObject m_ManipulatedObject;

    // Progress in precentage of the collision animation.
    private float m_CollisionAnimationProgress = 1f;
    private float m_CollisionPercentage = 0f;

    /// <summary>
    /// Returns whether the selected object is in collision.
    /// </summary>
    /// <returns>Whether the object is being collided with the environment.</returns>
    public override bool IsTriggering()
    {
        return m_CollisionAnimationProgress < 1f;
    }

    /// <summary>
    /// Trigger the collision event.
    /// </summary>
    /// <param name="collidedObject">The object collided with the environment.</param>
    /// <param name="collisionPercentage">The collision percentage.</param>
    public override void Trigger(GameObject collidedObject, float collisionPercentage = 1f)
    {
        m_ManipulatedObject = collidedObject;
        m_CollisionPercentage = collisionPercentage;

        if (collisionPercentage > k_MidBound)
        {
            m_CollisionAnimationProgress = 0f;
            PlaceButton.DisableButton();
        }
    }

    /// <summary>
    /// Stops the collision event.
    /// </summary>
    /// <param name="collidedObject">The tested object.</param>
    /// <param name="collisionPercentage">The collision percentage.</param>
    public override void Stop(GameObject collidedObject, float collisionPercentage = 0)
    {
        var colorToChange = OriginalColor;
        foreach (var mat in m_ManipulatedObject.GetComponent<Renderer>().materials)
        {
            mat.color = colorToChange;
        }

        m_CollisionPercentage = 0f;
        m_CollisionAnimationProgress = 1f;
        PlaceButton.EnableButton();
    }

    /// <summary>
    /// Updates the collision animiation every frame: rotates Andy when collision occurs.
    /// </summary>
    private void Update()
    {
        if (!IsTriggering() || m_ManipulatedObject == null)
        {
            return;
        }

        m_CollisionAnimationProgress += Time.deltaTime / AnimationInSeconds;

        var colorToChange = OriginalColor;

        if (EnableAnimation)
        {
            // Animates the material color from white to red, keep red for a while, and then white.
            if (m_CollisionAnimationProgress < k_MidBound)
            {
                var intensity = (k_MidBound - m_CollisionAnimationProgress) / k_MidBound;
                colorToChange = new Color(1, intensity, intensity, 1);
            }
            else if (m_CollisionAnimationProgress > k_HighBound)
            {
                var intensity = (m_CollisionAnimationProgress - k_HighBound) / (1f - k_HighBound);
                colorToChange = new Color(1, intensity, intensity, 1);
            }
            else
            {
                colorToChange = Color.red;
            }
        }
        else
        {
            if (m_CollisionPercentage > k_MidBound)
            {
                colorToChange = Color.red;
            }
            else if (m_CollisionPercentage < k_MidBound && m_CollisionPercentage > k_LowBound)
            {
                colorToChange = k_OrangeColor;
            }
        }

        foreach (var mat in m_ManipulatedObject.GetComponent<Renderer>().materials)
        {
            mat.color = colorToChange;
        }
    }
}
