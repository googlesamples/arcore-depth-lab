//-----------------------------------------------------------------------
// <copyright file="CarouselMovement.cs" company="Google LLC">
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
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// This class handles the movement and logic of the 3D carousel.
/// </summary>
public class CarouselMovement : MonoBehaviour
{
    /// <summary>
    /// Camera that renders the carousel and related elements.
    /// </summary>
    public Camera GUICamera;

    /// <summary>
    /// The RawImage that renders the rendertexture.
    /// </summary>
    public RawImage RawImage;

    /// <summary>
    /// Text for the label under the center carousel button.
    /// </summary>
    public Text ButtonLabel;

    /// <summary>
    /// Number of seconds for the same button to be in the center before switching scenes.
    /// </summary>
    public float SecondsBeforeSwitching = 3f;

    /// <summary>
    /// Multiplier for carousel movement.
    /// </summary>
    public float TouchDeltaStrength = 4f;

    /// <summary>
    /// The minimum Y rotation angle for the carousel.
    /// </summary>
    public float MinimumRotation;

    /// <summary>
    /// The maximum Y rotation angle for the carousel.
    /// </summary>
    public float MaximumRotation;

    private Component[] m_Buttons;

    private Coroutine m_InterpolateCoroutine;

    private GameObject m_CenterButton;

    private GameObject m_DelayedCenterButton;

    private GameObject m_TriggeredCenterButton;

    private Texture m_GUITex;

    private bool m_CenterButtonChanged = true;

    private float m_SceneLoadTimer = 0;

    private float m_ClickHoldTimer = 0;

    private float m_StartCursorPosition = 0;

    private float m_LastCursorPosition = 0;

    private float m_StartCarouselRotation = 0;

    private float m_DestinyCarouselRotation = 0;

    private float m_CarouselVelocity = 0.0f;

    private float m_CarouseltemAngleStep = 4.5f;

    private int m_NegativeHalfNumberOfItens = 3;

    private int m_PositiveHalfNumberOfItens = 3;

    private bool m_TouchStarted = false;

    // Start is called before the first frame update.
    private void Start()
    {
        m_GUITex = GUICamera.targetTexture;
        m_Buttons = GetComponentsInChildren<SceneButton>();
        float maxRotation = float.NegativeInfinity;
        float minRotation = float.PositiveInfinity;

        foreach (SceneButton button in m_Buttons)
        {
            GameObject item = button.gameObject.transform.parent.gameObject;
            float rotation = WrapAngle(item.transform.localEulerAngles.y) * -1;
            maxRotation = Mathf.Max(rotation, maxRotation);
            minRotation = Mathf.Min(rotation, minRotation);
        }

        m_NegativeHalfNumberOfItens = (int)(minRotation / m_CarouseltemAngleStep);
        m_PositiveHalfNumberOfItens = (int)(maxRotation / m_CarouseltemAngleStep);

        // Starts the carousel on the leftmost item.
        transform.localEulerAngles = new Vector3(0, minRotation, 0);
        m_DestinyCarouselRotation = minRotation;
    }

    // Update is called once per frame.
    private void Update()
    {
        UpdateScaleAndLabels();
        bool carouselIsStopped = UpdateMovement();
        if (carouselIsStopped)
        {
            LoadCenterButtonScene();
        }

        // Only allow the screen to sleep when not tracking.
        if (Session.Status != SessionStatus.Tracking)
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
        else
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }

    private bool UpdateMovement()
    {
        if (Input.GetMouseButtonDown(0) && isCursorOnTouchRegion())
        {
            // On touch start.
            m_StartCursorPosition = Input.mousePosition.x;
            m_LastCursorPosition = Input.mousePosition.x;
            m_StartCarouselRotation = transform.localEulerAngles.y;
            m_ClickHoldTimer = 0f;
            m_TouchStarted = true;
            LoadingSpinner.Instance.Show();
        }
        else if (Input.GetMouseButton(0) && m_TouchStarted)
        {
            // On touch move.
            float dx = (m_LastCursorPosition - Input.mousePosition.x) / Screen.width;
            m_CarouselVelocity = dx / Time.deltaTime;
            m_LastCursorPosition = Input.mousePosition.x;
            float distanceFromStart = m_StartCursorPosition - Input.mousePosition.x;
            distanceFromStart /= Screen.width;
            distanceFromStart *= Screen.width * 0.02f; // Small speed boost.
            transform.localEulerAngles = new Vector3(0,
                        m_StartCarouselRotation + distanceFromStart, 0);
            m_ClickHoldTimer += Time.deltaTime;
        }
        else if (Input.GetMouseButtonUp(0) && m_TouchStarted)
        {
            float destiny = 0;
            //// On touch end.
            //// Check if it was just a tap.
            if (m_ClickHoldTimer < 0.1f)
            {
                // If its just a tap retrieve the tapped icon and
                // make the carousel animate to it.
                float rescaled_x = (m_LastCursorPosition / Screen.width) * m_GUITex.width;
                destiny = getTappedItem(new Vector2(rescaled_x,
                                            m_GUITex.height / 2));
            }
            else
            {
                float current_y = transform.localEulerAngles.y;
                current_y += m_CarouselVelocity;
                destiny = WrapAngle(current_y);
                destiny = Mathf.Round(destiny / m_CarouseltemAngleStep);
            }

            destiny = Mathf.Min(m_PositiveHalfNumberOfItens, destiny);
            destiny = Mathf.Max(m_NegativeHalfNumberOfItens, destiny);

            m_DestinyCarouselRotation = m_CarouseltemAngleStep * destiny;
            m_TouchStarted = false;
        }

        float current = WrapAngle(transform.localEulerAngles.y);
        float interpolatedRotation = current + (0.1f * (m_DestinyCarouselRotation - current));
        transform.localEulerAngles = new Vector3(0, interpolatedRotation, 0);

        return (interpolatedRotation - current) < 0.01f;
    }

    private bool isCursorOnTouchRegion()
    {
        return Input.mousePosition.y > Screen.height - (Screen.height * 0.2f);
    }

    private int getTappedItem(Vector2 cursorPosition)
    {
        float rotation = WrapAngle(transform.localEulerAngles.y);

        Ray ray = GUICamera.ScreenPointToRay(cursorPosition);
        RaycastHit rayHit;

        if (Physics.Raycast(ray, out rayHit, Mathf.Infinity))
        {
            if (rayHit.collider.gameObject.name == "quad")
            {
                GameObject item = rayHit.collider.gameObject.transform.parent.gameObject;
                rotation = WrapAngle(item.transform.localEulerAngles.y) * -1;
            }
        }

        return (int)Mathf.Round(rotation / m_CarouseltemAngleStep);
    }

    private void LoadCenterButtonScene()
    {
        // Timer and trigger logic for carousel.
        m_SceneLoadTimer += Time.deltaTime;
        if (m_SceneLoadTimer > SecondsBeforeSwitching)
        {
            m_SceneLoadTimer = 0f;
            if (m_DelayedCenterButton == m_CenterButton &&
                m_TriggeredCenterButton != m_CenterButton)
            {
                // Trigger button press when carousel has been at center for.
                SceneButton button = m_CenterButton.GetComponent<SceneButton>();
                if (button != null)
                {
                    button.Press();
                }

                m_TriggeredCenterButton = m_CenterButton;
            }
            else if (!m_TouchStarted && m_TriggeredCenterButton == m_CenterButton)
            {
                LoadingSpinner.Instance.Hide();
            }

            m_DelayedCenterButton = m_CenterButton;
        }
    }

    private void UpdateScaleAndLabels()
    {
        // Cast ray from center of UI and assign the detected center button to the centerButton
        // gameobject.
        Ray centerRay = GUICamera.ScreenPointToRay(new Vector2(
            m_GUITex.width / 2, m_GUITex.height / 2));
        RaycastHit centerRayHit;

        GameObject center_button = null;
        if (Physics.Raycast(centerRay, out centerRayHit, Mathf.Infinity))
        {
            center_button = centerRayHit.collider.gameObject;
        }

        m_CenterButtonChanged = center_button != m_CenterButton;

        if (m_CenterButtonChanged)
        {
            m_CenterButton = center_button;
            //// Provide user with haptic feedback.
            HapticManager.HapticFeedback();
            //// Update button label based on center button.
            if (m_CenterButton.GetComponent<SceneButton>() != null)
            {
                string centerButtonLabel = m_CenterButton.GetComponent<SceneButton>().SceneLabel;

                if (centerButtonLabel != null)
                {
                    ButtonLabel.text = centerButtonLabel;
                }
            }
        }

        // Scale down buttons that aren't in the center, and scale up the center button.
        foreach (SceneButton button in m_Buttons)
        {
            if (button.gameObject == m_CenterButton)
            {
                float scale = button.transform.localScale.x +
                        (0.25f * (1.12f - button.transform.localScale.x));
                button.transform.localScale = new Vector3(scale, scale, scale);
            }
            else
            {
                float scale = button.transform.localScale.x +
                        (0.1f * (1.0f - button.transform.localScale.x));
                button.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }

    /// <summary>
    /// Converts 360 angles to -180 -> +180 degrees.
    /// </summary>
    /// <param name="angle">Input unwrapped angle.</param>
    /// <returns>Returns wrapped angle.</returns>
    private float WrapAngle(float angle)
    {
        angle %= 360;
        if (angle > 180)
        {
            return angle - 360;
        }

        return angle;
    }

    /// <summary>
    /// Converts -180 -> +180 angles to 360 degrees.
    /// </summary>
    /// <param name="angle">Input wrapped angle.</param>
    /// <returns>Returns unwrapped angle.</returns>
    private float UnwrapAngle(float angle)
    {
        if (angle >= 0)
        {
            return angle;
        }

        angle = -angle % 360;

        return 360 - angle;
    }
}
