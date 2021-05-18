//-----------------------------------------------------------------------
// <copyright file="ObjectCollisionEvent.cs" company="Google LLC">
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
    private const float _lowBound = 0.02f;

    /// <summary>
    /// Percentage of collided vertices when we show orange collision.
    /// </summary>
    private const float _midBound = 0.2f;

    /// <summary>
    /// Percentage of collided vertices when we show red collision.
    /// </summary>
    private const float _highBound = 0.8f;
    private static readonly Color _orangeColor = new Color(1f, 0.5f, 0f, 1f);

    // The current manipulated object.
    private GameObject _manipulatedObject;

    // Progress in precentage of the collision animation.
    private float _collisionAnimationProgress = 1f;
    private float _collisionPercentage = 0f;

    /// <summary>
    /// Returns whether the selected object is in collision.
    /// </summary>
    /// <returns>Whether the object is being collided with the environment.</returns>
    public override bool IsTriggering()
    {
        return _collisionAnimationProgress < 1f;
    }

    /// <summary>
    /// Trigger the collision event.
    /// </summary>
    /// <param name="collidedObject">The object collided with the environment.</param>
    /// <param name="collisionPercentage">The collision percentage.</param>
    public override void Trigger(GameObject collidedObject, float collisionPercentage = 1f)
    {
        _manipulatedObject = collidedObject;
        _collisionPercentage = collisionPercentage;

        if (collisionPercentage > _midBound)
        {
            _collisionAnimationProgress = 0f;
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
        foreach (var mat in _manipulatedObject.GetComponent<Renderer>().materials)
        {
            mat.color = colorToChange;
        }

        _collisionPercentage = 0f;
        _collisionAnimationProgress = 1f;
        PlaceButton.EnableButton();
    }

    /// <summary>
    /// Updates the collision animiation every frame: rotates Andy when collision occurs.
    /// </summary>
    private void Update()
    {
        if (!IsTriggering() || _manipulatedObject == null)
        {
            return;
        }

        _collisionAnimationProgress += Time.deltaTime / AnimationInSeconds;

        var colorToChange = OriginalColor;

        if (EnableAnimation)
        {
            // Animates the material color from white to red, keep red for a while, and then white.
            if (_collisionAnimationProgress < _midBound)
            {
                var intensity = (_midBound - _collisionAnimationProgress) / _midBound;
                colorToChange = new Color(1, intensity, intensity, 1);
            }
            else if (_collisionAnimationProgress > _highBound)
            {
                var intensity = (_collisionAnimationProgress - _highBound) / (1f - _highBound);
                colorToChange = new Color(1, intensity, intensity, 1);
            }
            else
            {
                colorToChange = Color.red;
            }
        }
        else
        {
            if (_collisionPercentage > _midBound)
            {
                colorToChange = Color.red;
            }
            else if (_collisionPercentage < _midBound && _collisionPercentage > _lowBound)
            {
                colorToChange = _orangeColor;
            }
        }

        foreach (var mat in _manipulatedObject.GetComponent<Renderer>().materials)
        {
            mat.color = colorToChange;
        }
    }
}
