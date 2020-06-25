//-----------------------------------------------------------------------
// <copyright file="AvatarSceneController.cs" company="Google LLC">
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
using GoogleARCore.Examples.Common;
using GoogleARCore.Examples.ObjectManipulation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Manages the interaction between the avatar and the scene.
/// </summary>
public class AvatarSceneController : MonoBehaviour
{
    /// <summary>
    /// The first-person camera being used to render the passthrough camera image (i.e. AR
    /// background).
    /// </summary>
    public Camera FirstPersonCamera;

    /// <summary>
    /// The avatar prefab to be placed when a raycast from a user touch hits a plane.
    /// </summary>
    [FormerlySerializedAs("avatarPrefab")]
    public GameObject AvatarPrefab;

    /// <summary>
    /// The mesh collider for shooting cubes.
    /// </summary>
    public GameObject ColliderMesh;

    /// <summary>
    /// We'll place a ground plane so that the projector lands on it.
    /// </summary>
    [FormerlySerializedAs("groundPlane")]
    public GameObject GroundPlane;

    /// <summary>
    /// The projectile prefab.
    /// </summary>
    [FormerlySerializedAs("projectile")]
    public GameObject Projectile;

    /// <summary>
    /// How much thrust are we giving to the projectile.
    /// </summary>
    [FormerlySerializedAs("projectileThrust")]
    public float ProjectileThrust = 5;

    /// <summary>
    /// The UI for our buttons.
    /// </summary>
    [FormerlySerializedAs("interactionUI")]
    public GameObject InteractionUI;

    /// <summary>
    /// Ignores the clicks from the bottom percentage of the screen.
    /// </summary>
    [FormerlySerializedAs("percentageOfScreenWithoutTouches")]
    public float PercentageOfScreenWithoutTouches = 0.2f;

    /// <summary>
    /// Track our avatar once we've place it.
    /// </summary>
    [HideInInspector]
    [FormerlySerializedAs("avatar")]
    public GameObject Avatar;

    /// <summary>
    /// Flag to enable or disable Avatar manipulation.
    /// </summary>
    public bool EnableAvatarManipulation;

    /// <summary>
    /// Collision-aware Manipulator prefab to attach placed objects to.
    /// </summary>
    public GameObject CollisionAwareManipulatorPrefab;

    /// <summary>
    /// Collision detection module.
    /// </summary>
    public GameObject CollisionDetectorGameObject;

    /// <summary>
    /// Event for touch detection.
    /// </summary>
    public UnityEvent TouchDetected;

    /// <summary>
    /// Plane icon.
    /// </summary>
    public GameObject PlaneIcon;

    /// <summary>
    /// Occlusion controller.
    /// </summary>
    public ToggleOcclusions ToggleOcclusions;

    /// <summary>
    /// Depth effect controller.
    /// </summary>
    public BackgroundToDepthMapEffectController DepthEffectController;

    /// <summary>
    /// The rotation in degrees need to apply to model when the Andy model is placed.
    /// </summary>
    private const float k_ModelRotation = 180.0f;

    /// <summary>
    /// True if the app is in the process of quitting due to an ARCore connection error,
    /// otherwise false.
    /// </summary>
    private bool m_IsQuitting = false;

    /// <summary>
    /// True once the avatar is placed,
    /// otherwise false. Placing the avatar will turn of placing functionality.
    /// </summary>
    private bool m_AvatarPlaced = false;

    private bool m_IsDepthShowing = false;

    private GameObject m_SceneContainer;

    /// <summary>
    /// Toggles the occlusion in the avatar gameobject.
    /// </summary>
    public void ToggleOcclusion()
    {
        ToggleOcclusions.Toggle();
    }

    /// <summary>
    /// Toggles the depthPlane.
    /// </summary>
    public void ToggleDepth()
    {
        if (m_IsDepthShowing)
        {
            DepthEffectController.StartTransitionToCamera();
        }
        else
        {
            DepthEffectController.StartTransitionToDepth();
        }

        m_IsDepthShowing = !m_IsDepthShowing;
    }

    /// <summary>
    /// Throws projectile upon where the user is touching.
    /// </summary>
    public void ThrowProjectile()
    {
        // Instantiates the projectile model.
        GameObject projectileObject = Instantiate(Projectile, FirstPersonCamera.transform.position,
          Quaternion.identity);
        Rigidbody RB = projectileObject.GetComponent<Rigidbody>();
        Vector3 ForceVector = FirstPersonCamera.transform.forward;
        RB.velocity = ForceVector * ProjectileThrust;
    }

    /// <summary>
    /// Controller to reset everything.
    /// </summary>
    public void ResetWholeScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    /// <summary>
    /// Toggles the UI elements.
    /// </summary>
    private void Start()
    {
        if (FirstPersonCamera == null)
        {
            FirstPersonCamera = Camera.main;
        }

        ToggleUI();

        if (EnableAvatarManipulation)
        {
            CollisionDetectorGameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Manages the touch events.
    /// </summary>
    private void Update()
    {
        _UpdateApplicationLifecycle();

        // If the player has not touched the screen, we are done with this update.
        Touch touch;
        if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
        {
            return;
        }

        // Should not handle input if the player is pointing on UI. We are also discarding the
        // entire bottom 20% of the screen to avoid erroneou clicks around the UI area.
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId) ||
            touch.position.y < (Screen.height * PercentageOfScreenWithoutTouches))
        {
            return;
        }

        // Shoots the cubes.
        if (m_AvatarPlaced)
        {
            TouchDetected?.Invoke();
            return;
        }

        // Raycasts against the location the player touched to search for planes.
        TrackableHit hit;
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
            TrackableHitFlags.FeaturePointWithSurfaceNormal;

        if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
        {
            // We can't use hit.Pose.up cause it seems to cause build issues in the
            // 2017.x Unity version.
            Vector3 hitUp = Vector3.Normalize(hit.Pose.rotation * Vector3.up);

            // Use hit pose and camera pose to check if hittest is from the
            // back of the plane, if it is, no need to create the anchor.
            if ((hit.Trackable is DetectedPlane) &&
                Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                    hit.Pose.rotation * Vector3.up) < 0)
            {
                Debug.Log("Hit at back of the current DetectedPlane");
            }
            else if (Mathf.Abs(Vector3.Dot(hitUp, Vector3.up)) < 0.95)
            {
                Debug.Log("Hit a vertical wall.");
            }
            else
            {
                if (!m_AvatarPlaced)
                {
                    PlaceAvatarScene(hit);
                    m_AvatarPlaced = true;
                }
            }
        }
    }

    /// <summary>
    /// Function for placing avatar and the ground plane.
    /// </summary>
    /// <param name="hit">Trackable hit.</param>
    private void PlaceAvatarScene(TrackableHit hit)
    {
        m_SceneContainer = new GameObject();

        // Creates an anchor to allow ARCore to track the hitpoint as understanding of
        // the physical world evolves.
        var anchor = hit.Trackable.CreateAnchor(hit.Pose);

        // Instantiates avatar model at the hit pose.
        var avatarObject = Instantiate(AvatarPrefab, hit.Pose.position, hit.Pose.rotation);

        // Compensates for the hitPose rotation facing away from the raycast (i.e. camera).
        avatarObject.transform.Rotate(0, k_ModelRotation, 0, Space.Self);

        // Makes avatar model a child of the anchor.
        Avatar = avatarObject;
        var avatarController = Avatar.GetComponent<AvatarController>();

        if (avatarController != null)
        {
            avatarController.EnableAvatarManipulation = EnableAvatarManipulation;
        }

        if (EnableAvatarManipulation)
        {
            // Attaches the collision-aware manipulator.
            var manipulator = Instantiate(CollisionAwareManipulatorPrefab,
                                hit.Pose.position, hit.Pose.rotation);

            // Allows the manipulator to read user-customized parameters in the detector.
            var manipulatorScript = manipulator.GetComponent<CollisionAwareManipulator>();
            var collisionDetector = CollisionDetectorGameObject.GetComponent<CollisionDetector>();
            manipulatorScript.SetCollisionDetector(collisionDetector);
            avatarObject.transform.parent = manipulator.transform;

            anchor.transform.parent = m_SceneContainer.transform;

            // Makes manipulator a child of the anchor.
            manipulator.transform.parent = anchor.transform;

            // Selects the placed object.
            manipulator.GetComponent<Manipulator>().Select();
        }
        else
        {
            anchor.transform.parent = m_SceneContainer.transform;
            Avatar.transform.parent = m_SceneContainer.transform;
        }

        // Places the ground plane to avoid the cubes to fall to holes.
        Vector3 planePosition = hit.Pose.position + new Vector3(0.0f, 0.0f, 0.0f);
        GameObject NewGroundPlane = Instantiate(GroundPlane, planePosition, hit.Pose.rotation)
          as GameObject;
        NewGroundPlane.transform.Rotate(0, k_ModelRotation, 0, Space.Self);
        NewGroundPlane.transform.parent = anchor.transform;

        ToggleUI();
    }

    /// <summary>
    /// Toggles the our button UI only after the avatar has been placed.
    /// </summary>
    private void ToggleUI()
    {
        InteractionUI.GetComponent<Canvas>().enabled =
          !InteractionUI.GetComponent<Canvas>().enabled;
    }

    /// <summary>
    /// Checks and updates the application lifecycle.
    /// </summary>
    private void _UpdateApplicationLifecycle()
    {
        // Exits the app when the 'back' button is pressed.
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        // Only allows the screen to sleep when not tracking.
        if (Session.Status != SessionStatus.Tracking)
        {
            const int kLostTrackingSleepTimeout = 15;
            Screen.sleepTimeout = kLostTrackingSleepTimeout;
        }
        else
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        if (m_IsQuitting)
        {
            return;
        }
    }

    /// <summary>
    /// Actually quits the application.
    /// </summary>
    private void _DoQuit()
    {
        Application.Quit();
    }
}
