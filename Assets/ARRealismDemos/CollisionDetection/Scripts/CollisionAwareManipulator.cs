//-----------------------------------------------------------------------
// <copyright file="CollisionAwareManipulator.cs" company="Google LLC">
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

namespace GoogleARCore.Examples.ObjectManipulation
{
    using System;
    using System.Collections.Generic;
    using GoogleARCore;
    using GoogleARCore.Examples.ObjectManipulationInternal;
    using UnityEngine;

    /// <summary>
    /// Results of a single collision test.
    /// </summary>
    [Flags]
    public enum CollisionResults
    {
        /// <summary>
        /// Invalid depth.
        /// </summary>
        InvalidDepth = -1,

        /// <summary>
        /// No collision.
        /// </summary>
        NoCollision = 0,

        /// <summary>
        /// Valid collision.
        /// </summary>
        Collided = 1,
    }

    /// <summary>
    /// Manipulates the position of an object via a drag gesture.
    /// If not selected, the object will be selected when the drag gesture starts.
    /// Tests if collision occurs when dragging.
    /// </summary>
    [RequireComponent(typeof(SelectionManipulator))]
    public class CollisionAwareManipulator : Manipulator
    {
        /// <summary>
        /// Reference to the DepthTextureProvider instance to switch depth modes.
        /// </summary>
        public DepthTextureProvider DepthTextureProvider;

        /// <summary>
        /// Manages the collision parameters.
        /// </summary>
        public CollisionDetector ParentCollisionDetector;

        /// <summary>
        /// The translation mode of this object.
        /// </summary>
        public TransformationUtility.TranslationMode ObjectTranslationMode;

        /// <summary>
        /// If the object is translated more than this distance, make the movement invalid.
        /// </summary>
        public float MaxTranslationDistanceInMeters = 3;

        // A small default texture size to create a texture of unknown size.
        private const int _defaultTextureSize = 2;

        // The maximum distance in meters which the model translates per second.
        private const float _positionSpeedMS = 12.0f;
        private const float _translationThreshold = 0.0001f;
        private const float _minimumSquaredDistance = 2.5e-7f;

        // A vertical offset in meters to prevent inaccurate collision checking.
        private static float _objectVerticalPlacementOffset = 0.03f;

        /// <summary>
        /// A simplified collider proxy for detecting collision. Set by SetProxy().
        /// </summary>
        private GameObject _colliderProxy;

        /// <summary>
        /// Handles the collision event.
        /// </summary>
        private CollisionEvent _collisionEvent;

        // Updates every frame with the latest depth data.
        private Texture2D _depthTexture;
        private bool _isActive = false;
        private Vector3 _desiredAnchorPosition;
        private Vector3 _desiredLocalPosition;
        private Vector3 _lastValidLocalPosition;
        private Quaternion _desiredRotation;
        private float _groundingPlaneHeight;
        private TrackableHit _lastHit;
        private GameObject _manipulatedObject;
        private Vector3[] _selectedVerticesList;

        /// <summary>
        /// Sets the parental collision detector.
        /// </summary>
        /// <param name="detector">The managing collision detector script.</param>
        public void SetCollisionDetector(CollisionDetector detector)
        {
            ParentCollisionDetector = detector;
            _colliderProxy = ParentCollisionDetector.ColliderProxy;
            _collisionEvent = detector.CollisionEvent.GetComponent<CollisionEvent>();
        }

        /// <summary>
        /// The Unity's Start method.
        /// </summary>
        protected void Start()
        {
            _depthTexture = new Texture2D(_defaultTextureSize, _defaultTextureSize);
        }

        /// <summary>
        /// The Unity's Update method.
        /// </summary>
        protected override void Update()
        {
            base.Update();
            UpdatePosition();
        }

        /// <summary>
        /// Returns true if the manipulation can be started for the given gesture.
        /// </summary>
        /// <param name="gesture">The current gesture.</param>
        /// <returns>True if the manipulation can be started.</returns>
        protected override bool CanStartManipulationForGesture(DragGesture gesture)
        {
            if (gesture.TargetObject == null)
            {
                return false;
            }

            // If the gesture isn't targeting this item, don't start manipulating.
            if (gesture.TargetObject != gameObject)
            {
                return false;
            }

            Select();

            _manipulatedObject = gesture.TargetObject;

            if (!ParentCollisionDetector.UseColliderProxy)
            {
                if (ParentCollisionDetector.UseBoundingBox)
                {
                    _selectedVerticesList = Utilities.GetBoundingBoxVertices(_manipulatedObject);
                }
                else
                {
                    _selectedVerticesList = GetVerticesInChildren(_manipulatedObject);
                }
            }
            else
            {
                _selectedVerticesList = GetVerticesInChildren(_colliderProxy);
            }

            return true;
        }

        /// <summary>
        /// Function called when the manipulation is started.
        /// </summary>
        /// <param name="gesture">The current gesture.</param>
        protected override void OnStartManipulation(DragGesture gesture)
        {
            _groundingPlaneHeight = transform.parent.position.y;
            _isActive = true;
        }

        /// <summary>
        /// Continues the translation.
        /// </summary>
        /// <param name="gesture">The current gesture.</param>
        protected override void OnContinueManipulation(DragGesture gesture)
        {
            _isActive = true;

            TransformationUtility.Placement desiredPlacement =
              TransformationUtility.GetBestPlacementPosition(
                transform.parent.position, gesture.Position, _groundingPlaneHeight,
                _objectVerticalPlacementOffset, MaxTranslationDistanceInMeters,
                ObjectTranslationMode);

            // Discards invalid position.
            if (!desiredPlacement.HoveringPosition.HasValue ||
              !desiredPlacement.PlacementPosition.HasValue)
            {
                return;
            }

            _manipulatedObject = gesture.TargetObject;

            // Drops the value when collision occurs.
            if (TestCollision(desiredPlacement.PlacementPosition.Value))
            {
                _collisionEvent.Trigger(_manipulatedObject);
                return;
            }

            if (desiredPlacement.PlacementRotation.HasValue)
            {
                // Rotates if the plane direction has changed.
                if (((desiredPlacement.PlacementRotation.Value * Vector3.up) - transform.up)
                  .sqrMagnitude > _minimumSquaredDistance)
                {
                    _collisionEvent.Trigger(_manipulatedObject);
                    return;
                }
                else
                {
                    _desiredRotation = transform.rotation;
                }
            }

            // If desired position is lower than the current position, don't drop it until it's
            // finished.
            _desiredLocalPosition = transform.parent.InverseTransformPoint(
              desiredPlacement.HoveringPosition.Value);

            _desiredAnchorPosition = desiredPlacement.PlacementPosition.Value;

            _groundingPlaneHeight = desiredPlacement.UpdatedGroundingPlaneHeight;

            if (desiredPlacement.PlacementPlane.HasValue)
            {
                _lastHit = desiredPlacement.PlacementPlane.Value;
            }
        }

        /// <summary>
        /// Finishes the translation.
        /// </summary>
        /// <param name="gesture">The current gesture.</param>
        protected override void OnEndManipulation(DragGesture gesture)
        {
            if (_collisionEvent.IsTriggering())
            {
                return;
            }

            GameObject oldAnchor = transform.parent.gameObject;

            Pose desiredPose = new Pose(_desiredAnchorPosition, _lastHit.Pose.rotation);

            Vector3 desiredLocalPosition =
              transform.parent.InverseTransformPoint(desiredPose.position);

            if (desiredLocalPosition.magnitude > MaxTranslationDistanceInMeters)
            {
                desiredLocalPosition = desiredLocalPosition.normalized *
                    MaxTranslationDistanceInMeters;
            }

            desiredPose.position = transform.parent.TransformPoint(desiredLocalPosition);

            Anchor newAnchor = _lastHit.Trackable.CreateAnchor(desiredPose);

            if (TestCollision(newAnchor.transform.position))
            {
                _collisionEvent.Trigger(gesture.TargetObject);
                return;
            }

            var rotationDifference = (desiredPose.rotation * Vector3.up) - newAnchor.transform.up;
            if (rotationDifference.sqrMagnitude > _minimumSquaredDistance)
            {
                _collisionEvent.Trigger(gesture.TargetObject);
                return;
            }

            // Translates the model.
            transform.parent = newAnchor.transform;
            _desiredLocalPosition = Vector3.zero;

            // Discards if the plane direction has changed.
            _desiredRotation = newAnchor.transform.rotation;

            Destroy(oldAnchor);

            // Makes sure position is updated one last time.
            _isActive = true;
        }

        /// <summary>
        /// Fetches the depth value of the physical environment corresponding to the screenPosition.
        /// </summary>
        /// <param name="screenPosition">The position of the vertex in the screen space.</param>
        /// <returns>The depth value in meters.</returns>
        private float FetchEnvironmentDepth(Vector3 screenPosition)
        {
            int depthY = (int)(screenPosition.y * _depthTexture.height);
            int depthX = (int)(screenPosition.x * _depthTexture.width);

            // Obtains the depth value in short.
#if UNITY_2018_3_OR_NEWER
            var depthData = _depthTexture.GetRawTextureData<short>();
            var depthIndex = depthY * _depthTexture.width + depthX;
            var depthInShort = depthData[depthIndex];
#else
            var depthData = _depthTexture.GetRawTextureData();
            var depthIndex = ((depthY * _depthTexture.width) + depthX) * 2;
            byte[] value = new byte[2];
            value[0] = depthData[depthIndex];
            value[1] = depthData[depthIndex + 1];
            var depthInShort = BitConverter.ToInt16(value, 0);
#endif

            var result = depthInShort * 0.001f;

            return result;
        }

        /// <summary>
        /// Tests if worldPosition is behind the physical environment.
        /// </summary>
        /// <param name="targetWorldPosition">The position of the virtual object in the world space.</param>
        /// <returns>True if collision occurs.</returns>
        private CollisionResults CollisionTestSingleVertex(Vector3 targetWorldPosition)
        {
            // Computes the environment's depth.
            var screenPosition = Camera.main.WorldToScreenPoint(targetWorldPosition);

            // Note that the screenspace is in portrait mode.
            var normalizedScreenPosition = new Vector3(
              Mathf.Clamp01(1f - (screenPosition.y / Screen.currentResolution.height)),
              Mathf.Clamp01(1f - (screenPosition.x / Screen.currentResolution.width)),
              screenPosition.z);

            // Fetches the environment depth.
            var normalizedScreenPoint = new Vector2(
                normalizedScreenPosition.x, normalizedScreenPosition.y);
            var depthArray = ParentCollisionDetector.UseRawDepth ? DepthSource.RawDepthArray
                                                                    : DepthSource.DepthArray;
            var environmentDepth = DepthSource.GetDepthFromUV(normalizedScreenPoint,
                                                                      depthArray);

            //// Use this when new API is ready.
            //// var environmentDepth = FetchEnvironmentDepth(normalizedScreenPosition);

            if (environmentDepth == DepthSource.InvalidDepthValue)
            {
                return CollisionResults.InvalidDepth;
            }

            // Computes the virtual object's depth.
            var targetDepth = normalizedScreenPosition.z;

            return targetDepth >
                   environmentDepth + ParentCollisionDetector.VertexCollisionThresholdInMeters
                   ? CollisionResults.Collided : CollisionResults.NoCollision;
        }

        /// <summary>
        /// Get all vertices of a mesh in the local coordinate system.
        /// Tested: Real-time performance with GetRawTexture<short> optimization for over 1K vertices.
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <returns>A list of vertices local positions in Vector3.</returns>
        private Vector3[] GetVerticesInChildren(GameObject targetObject)
        {
            var meshFilters = targetObject.GetComponentsInChildren<MeshFilter>();
            var vertices = new List<Vector3>();
            foreach (var meshFilter in meshFilters)
            {
                vertices.AddRange(meshFilter.mesh.vertices);
            }

            return vertices.ToArray();
        }

        /// <summary>
        /// Tests if a collider at worldPosition is behind the physical environment.
        /// </summary>
        /// <param name="targetWorldPosition">The position of the virtual object in the world space.</param>
        /// <returns>Whether the mesh triggers collision at targetWorldPosition.</returns>
        private bool TestCollision(Vector3 targetWorldPosition)
        {
            // Retrieves the latest (sparse) depth data from ARCore.
            if (ParentCollisionDetector.UseRawDepth)
            {
                DepthSource.DepthDataSource.UpdateRawDepthTexture(ref _depthTexture);
            }
            else
            {
                DepthSource.DepthDataSource.UpdateDepthTexture(ref _depthTexture);
            }

            // Retrieves all vertices of the collider mesh.
            var coliderMesh = ParentCollisionDetector.UseColliderProxy ? _colliderProxy
                                                                       : _manipulatedObject;
            if (_selectedVerticesList == null)
            {
                _selectedVerticesList = GetVerticesInChildren(coliderMesh);
            }

            int numCollision = 0;
            float collisionPercentage = 0;
            int totalTests = _selectedVerticesList.Length;

            // Tests every single vertex of the mesh and gets the statistics of the results.
            foreach (var vertex in _selectedVerticesList)
            {
                var result = CollisionTestSingleVertex(targetWorldPosition + vertex);
                switch (result)
                {
                    case CollisionResults.Collided:
                        ++numCollision;
                        break;
                    case CollisionResults.InvalidDepth:
                        --totalTests;
                        break;
                }
            }

            // Reports no collision if the depth map is empty.
            if (totalTests == 0)
            {
                return false;
            }

            collisionPercentage = (float)numCollision / totalTests;
            return collisionPercentage > ParentCollisionDetector.MeshCollisionThresholdInPercentage;
        }

        /// <summary>
        /// Updates the position of the model.
        /// </summary>
        private void UpdatePosition()
        {
            // Blocks the translation when the manipulator is not active or the object is collided.
            if (!_isActive || _collisionEvent.IsTriggering())
            {
                _desiredLocalPosition = _lastValidLocalPosition;
                return;
            }

            // Lerps position.
            Vector3 oldLocalPosition = transform.localPosition;
            Vector3 newLocalPosition = Vector3.Lerp(
              oldLocalPosition, _desiredLocalPosition, Time.deltaTime * _positionSpeedMS);

            float diffLenghth = (_desiredLocalPosition - newLocalPosition).sqrMagnitude;
            if (diffLenghth < _minimumSquaredDistance)
            {
                newLocalPosition = _desiredLocalPosition;
                _isActive = false;
            }

            transform.localPosition = newLocalPosition;
            _lastValidLocalPosition = transform.localPosition;

            // Lerps rotation.
            Quaternion oldRotation = transform.rotation;
            Quaternion newRotation =
              Quaternion.Lerp(oldRotation, _desiredRotation, Time.deltaTime * _positionSpeedMS);
            transform.rotation = newRotation;

            // Avoids placing the selection higher than the object if the anchor is higher than the
            // object.
            float newElevation =
              Mathf.Max(0, -transform.InverseTransformPoint(_desiredAnchorPosition).y);
            GetComponent<SelectionManipulator>().OnElevationChanged(newElevation);
        }
    }
}
