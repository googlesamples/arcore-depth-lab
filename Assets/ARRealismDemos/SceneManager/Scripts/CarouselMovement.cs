//-----------------------------------------------------------------------
// <copyright file="CarouselMovement.cs" company="Google LLC">
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

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

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

    private const string _defaultSceneLabel = "Depth-Lab";
    private const float _sceneButtonAngle = 4.5f;

    private Component[] _buttons;

    private Coroutine _interpolateCoroutine;

    private GameObject _centerButton;

    private GameObject _delayedCenterButton;

    private GameObject _triggeredCenterButton;

    private Texture _guiTex;

    private bool _centerButtonChanged = true;

    private float _sceneLoadTimer = 0;

    private float _clickHoldTimer = 0;

    private float _startCursorPosition = 0;

    private float _lastCursorPosition = 0;

    private float _startCarouselRotation = 0;

    private float _destinyCarouselRotation = 0;

    private float _carouselVelocity = 0.0f;

    private float _carouseltemAngleStep = 4.5f;

    private int _negativeHalfNumberOfItens = 3;

    private int _positiveHalfNumberOfItens = 3;

    private bool _touchStarted = false;

    // Start is called before the first frame update.
    private void Start()
    {
        _guiTex = GUICamera.targetTexture;
        _buttons = GetComponentsInChildren<SceneButton>();

        if (_buttons.Length == 0)
        {
            // Demo Carousel is the only active scene.
            ButtonLabel.text = _defaultSceneLabel;
            gameObject.SetActive(false);
            return;
        }

        float maxRotation = float.NegativeInfinity;
        float minRotation = float.PositiveInfinity;

        int buttonIndex = 0;
        foreach (SceneButton button in _buttons)
        {
            // List all scene buttons from left to right based on the index.
            GameObject item = button.gameObject.transform.parent.gameObject;
            item.transform.localEulerAngles = new Vector3(0, -buttonIndex * _sceneButtonAngle, 0);

            float rotation = WrapAngle(item.transform.localEulerAngles.y) * -1;
            maxRotation = Mathf.Max(rotation, maxRotation);
            minRotation = Mathf.Min(rotation, minRotation);

            buttonIndex++;
        }

        _negativeHalfNumberOfItens = (int)(minRotation / _carouseltemAngleStep);
        _positiveHalfNumberOfItens = (int)(maxRotation / _carouseltemAngleStep);

        // Starts the carousel on the leftmost item.
        transform.localEulerAngles = new Vector3(0, minRotation, 0);
        _destinyCarouselRotation = minRotation;
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
        Screen.sleepTimeout = ARSession.notTrackingReason == NotTrackingReason.None
            ? SleepTimeout.NeverSleep
            : SleepTimeout.SystemSetting;
    }

    private bool UpdateMovement()
    {
        if (Input.GetMouseButtonDown(0) && IsCursorOnTouchRegion())
        {
            // On touch start.
            _startCursorPosition = Input.mousePosition.x;
            _lastCursorPosition = Input.mousePosition.x;
            _startCarouselRotation = transform.localEulerAngles.y;
            _clickHoldTimer = 0f;
            _touchStarted = true;
            LoadingSpinner.Instance.Show();
        }
        else if (Input.GetMouseButton(0) && _touchStarted)
        {
            // On touch move.
            float dx = (_lastCursorPosition - Input.mousePosition.x) / Screen.width;
            _carouselVelocity = dx / Time.deltaTime;
            _lastCursorPosition = Input.mousePosition.x;
            float distanceFromStart = _startCursorPosition - Input.mousePosition.x;
            distanceFromStart /= Screen.width;
            distanceFromStart *= Screen.width * 0.02f; // Small speed boost.
            transform.localEulerAngles = new Vector3(0,
                _startCarouselRotation + distanceFromStart, 0);
            _clickHoldTimer += Time.deltaTime;
        }
        else if (Input.GetMouseButtonUp(0) && _touchStarted)
        {
            float destiny = 0;
            //// On touch end.
            //// Check if it was just a tap.
            if (_clickHoldTimer < 0.1f)
            {
                // If its just a tap retrieve the tapped icon and
                // make the carousel animate to it.
                float rescaled_x = (_lastCursorPosition / Screen.width) * _guiTex.width;
                destiny = GetTappedItem(new Vector2(rescaled_x,
                    _guiTex.height / 2));
            }
            else
            {
                float current_y = transform.localEulerAngles.y;
                current_y += _carouselVelocity;
                destiny = WrapAngle(current_y);
                destiny = Mathf.Round(destiny / _carouseltemAngleStep);
            }

            destiny = Mathf.Min(_positiveHalfNumberOfItens, destiny);
            destiny = Mathf.Max(_negativeHalfNumberOfItens, destiny);

            _destinyCarouselRotation = _carouseltemAngleStep * destiny;
            _touchStarted = false;
        }

        float current = WrapAngle(transform.localEulerAngles.y);
        float interpolatedRotation = current + (0.1f * (_destinyCarouselRotation - current));
        transform.localEulerAngles = new Vector3(0, interpolatedRotation, 0);

        return (interpolatedRotation - current) < 0.01f;
    }

    private bool IsCursorOnTouchRegion()
    {
        return Input.mousePosition.y > Screen.height - (Screen.height * 0.2f);
    }

    private int GetTappedItem(Vector2 cursorPosition)
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

        return (int)Mathf.Round(rotation / _carouseltemAngleStep);
    }

    private void LoadCenterButtonScene()
    {
        // Timer and trigger logic for carousel.
        _sceneLoadTimer += Time.deltaTime;
        if (_sceneLoadTimer > SecondsBeforeSwitching)
        {
            _sceneLoadTimer = 0f;
            if (_delayedCenterButton == _centerButton &&
                _triggeredCenterButton != _centerButton)
            {
                // Trigger button press when carousel has been at center for.
                SceneButton button = _centerButton.GetComponent<SceneButton>();
                if (button != null)
                {
                    button.Press();
                }

                _triggeredCenterButton = _centerButton;
            }
            else if (!_touchStarted && _triggeredCenterButton == _centerButton)
            {
                LoadingSpinner.Instance.Hide();
            }

            _delayedCenterButton = _centerButton;
        }
    }

    private void UpdateScaleAndLabels()
    {
        // Cast ray from center of UI and assign the detected center button to the centerButton
        // gameobject.
        Ray centerRay = GUICamera.ScreenPointToRay(new Vector2(
            _guiTex.width / 2, _guiTex.height / 2));
        RaycastHit centerRayHit;

        GameObject center_button = null;
        if (Physics.Raycast(centerRay, out centerRayHit, Mathf.Infinity))
        {
            center_button = centerRayHit.collider.gameObject;
        }

        _centerButtonChanged = center_button != _centerButton;

        if (_centerButtonChanged)
        {
            _centerButton = center_button;
            //// Provide user with haptic feedback.
            HapticManager.HapticFeedback();
            //// Update button label based on center button.
            if (_centerButton.GetComponent<SceneButton>() != null)
            {
                string centerButtonLabel = _centerButton.GetComponent<SceneButton>().SceneLabel;

                if (centerButtonLabel != null)
                {
                    ButtonLabel.text = centerButtonLabel;
                }
            }
        }

        // Scale down buttons that aren't in the center, and scale up the center button.
        foreach (SceneButton button in _buttons)
        {
            if (button.gameObject == _centerButton)
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