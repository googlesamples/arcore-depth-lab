//-----------------------------------------------------------------------
// <copyright file="CollisionDetector.cs" company="Google LLC">
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
namespace GoogleARCore.Examples.ObjectManipulation
{
    using System.Collections;
    using System.Collections.Generic;
    using GoogleARCore;
    using UnityEngine;

    /// <summary>
    /// Controls the placement of Andy objects via a tap gesture.
    /// Manages the collision-aware manipulator.
    /// </summary>
    public class CollisionDetector : Manipulator
    {
        /// <summary>
        /// A model to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject ObjectPrefab;

        /// <summary>
        /// Collision-aware Manipulator prefab to attach placed objects to.
        /// </summary>
        public GameObject CollisionAwareManipulatorPrefab;

        /// <summary>
        /// Handles the collision event.
        /// </summary>
        public CollisionEvent CollisionEvent;

        /// <summary>
        /// Type of depth texture to attach to the material.
        /// </summary>
        public bool UseSparseDepth = true;

        /// <summary>
        /// Whether to use the mesh's bounding box to represent the collider instead of usinsg
        /// the entire mesh vertices.
        /// </summary>
        public bool UseBoundingBox = true;

        /// <summary>
        /// Whether to use a mesh proxy to represent the collider instead of using the entire mesh
        /// vertices. Note the using the entire mesh vertices may trmendously slow down the
        /// application.
        /// </summary>
        public bool UseColliderProxy = false;

        /// <summary>
        /// Whether or not to create a new object on touch.
        /// </summary>
        public bool CreateNewObjectOnTouch = true;

        /// <summary>
        /// A simplified collider proxy for detecting collision.
        /// </summary>
        public GameObject ColliderProxy;

        /// <summary>
        /// Minimum ratio of vertices required for a valid collision detection.
        /// </summary>
        [Tooltip("Minimum ratio of vertices required for a valid collision detection."),
         Range(0f, 1f)]
        public float MeshCollisionThresholdInPercentage = 0.2f;

        /// <summary>
        /// Minimum depth difference in meters that a vertex is judged as collided.
        /// </summary>
        [Tooltip("Minimum depth difference in meters that a vertex is judged as collided."),
         Range(0f, 0.5f)]
        public float VertexCollisionThresholdInMeters = 0.15f;

        /// <summary>
        /// Returns true if the manipulation can be started for the given gesture.
        /// </summary>
        /// <param name="gesture">The current gesture.</param>
        /// <returns>True if the manipulation can be started.</returns>
        protected override bool CanStartManipulationForGesture(TapGesture gesture)
        {
            return gesture.TargetObject == null;
        }

        /// <summary>
        /// The Unity's Update method.
        /// </summary>
        protected override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// Function called when the manipulation is ended.
        /// </summary>
        /// <param name="gesture">The current gesture.</param>
        protected override void OnEndManipulation(TapGesture gesture)
        {
            if (!CreateNewObjectOnTouch)
            {
                return;
            }

            if (gesture.WasCancelled)
            {
                return;
            }

            // If gesture is targeting an existing object we are done.
            if (gesture.TargetObject != null)
            {
                return;
            }

            // Raycasts against the location the player touched to search for planes.
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;

            if (Frame.Raycast(
                gesture.StartPosition.x, gesture.StartPosition.y, raycastFilter, out hit))
            {
                // Uses hit pose and camera pose to check if hittest is from the
                // back of the plane, if it is, no need to create the anchor.
                if ((hit.Trackable is DetectedPlane) &&
                  Vector3.Dot(Camera.main.transform.position - hit.Pose.position,
                    hit.Pose.rotation * Vector3.up) < 0)
                {
                    Debug.Log("Hit at back of the current DetectedPlane");
                }
                else
                {
                    // Instantiates Andy model at the hit pose.
                    var andyObject = Instantiate(ObjectPrefab, hit.Pose.position,
                                                 hit.Pose.rotation);

                    // Instantiates manipulator.
                    var manipulator = Instantiate(CollisionAwareManipulatorPrefab,
                                        hit.Pose.position, hit.Pose.rotation);

                    // Allows the manipulator to read user-customized parameters in the detector.
                    var manipulatorScript = manipulator.GetComponent<CollisionAwareManipulator>();
                    manipulatorScript.SetCollisionDetector(this);

                    // Makes Andy model a child of the manipulator.
                    andyObject.transform.parent = manipulator.transform;

                    // Creates an anchor to allow ARCore to track the hitpoint as understanding of
                    // the physical world evolves.
                    var anchor = hit.Trackable.CreateAnchor(hit.Pose);

                    // Makes manipulator a child of the anchor.
                    manipulator.transform.parent = anchor.transform;

                    // Selects the placed object.
                    manipulator.GetComponent<Manipulator>().Select();
                }
            }
        }
    }
}
