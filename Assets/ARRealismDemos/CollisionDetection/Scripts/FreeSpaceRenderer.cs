//-----------------------------------------------------------------------
// <copyright file="FreeSpaceRenderer.cs" company="Google LLC">
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
namespace GoogleARCore.Examples.FreeSpaceRenderer
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using GoogleARCore;
    using GoogleARCore.Examples.ObjectManipulationInternal;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Visualizes free spaces when the user taps on the plane. Free space is determined by
    /// collision test of the virtual object and the physical environment.
    /// </summary>
    public class FreeSpaceRenderer : MonoBehaviour
    {
        /// <summary>
        /// Limits depth map polling by a user-defined frame rate.
        /// </summary>
        public float DepthPollingFrameRate = 30;

        /// <summary>
        /// Limits depth map polling by a user-defined frame rate.
        /// </summary>
        public float FreeSpaceDetectionFrameRate = 10;

        /// <summary>
        /// Minimum depth difference in meters that a vertex is judged as collided.
        /// </summary>
        public float VertexCollisionThresholdInMeters = 0.15f;

        /// <summary>
        /// The gap between adjacent free space visualizer in the world space.
        /// </summary>
        public float FreeSpaceAnchorGapInMeters = 0.05f;

        /// <summary>
        /// The maximum volume to represent the free space.
        /// </summary>
        public Vector3 FreeSpaceVolumeSizeInMeters = new Vector3(5.0f, 2.0f, 5.0f);

        /// <summary>
        /// The origin of the bounding box for collision test.
        /// </summary>
        public Vector3 BoundingBoxOrigin = new Vector3(0.0f, 0.3f, 0.0f);

        /// <summary>
        /// The dimensions of the bounding box for collision test.
        /// </summary>
        public Vector3 BoundingBoxDimensions = new Vector3(0.5f, 0.5f, 0.5f);

        /// <summary>
        /// A prefab with mesh representing the free space.
        /// </summary>
        public GameObject FreeSpaceVisualizerPrefab;

        /// <summary>
        /// If the object is translated more than this distance, make the movement invalid.
        /// </summary>
        public float MaxTranslationDistanceInMeters = 5;

        /// <summary>
        /// Tolerant distance for elevation.
        /// </summary>
        public float ElevationThreshold = 0.5f;

        /// <summary>
        /// The translation mode of this object.
        /// </summary>
        public TransformationUtility.TranslationMode ObjectTranslationMode;

        private const float k_MaxDepthInMeters = 7f;
        private const float k_DiskScale = 10f;
        private const int k_FreeSpaceEdgeSize = 256;
        private const int k_FreeSpaceVolumeMaxCapacity =
            k_FreeSpaceEdgeSize * k_FreeSpaceEdgeSize * k_FreeSpaceEdgeSize;

        private const int k_ScreenMaxBinsY = 20;
        private const int k_ScreenMaxBinsX = 40;

        // A small default texture size to create a texture of unknown size.
        private const int k_DefaultTextureSize = 2;

        private const int k_MaxIterations = 10000;

        // The maximum depth value from ArCore.
        private const short k_ArCoreMaxDepthMM = 8192;
        private const float k_ArCoreMMToM = 0.001f;
        private const float k_StepX = 1f / k_ScreenMaxBinsX;
        private const float k_StepY = 1f / k_ScreenMaxBinsY;
        private static readonly Vector3[] k_Directions = new[]
        {
            new Vector3(-k_StepX, 0, 0), new Vector3(k_StepX, 0, 0),
            new Vector3(0, -k_StepY, 0), new Vector3(0, k_StepY, 0)
        };

        private Anchor m_TouchAnchor;
        private Matrix4x4 m_ScreenRotation = Matrix4x4.Rotate(Quaternion.identity);

        // Updates every frame with the latest depth data.
        private Texture2D m_DepthTexture;

        // Stores whether the world space is occupied
        private bool[] m_WorldSpaceMap = new bool[k_FreeSpaceVolumeMaxCapacity];

        private bool[] m_ScreenSpaceMap = new bool[k_ScreenMaxBinsY * k_ScreenMaxBinsX];

        private CameraIntrinsics m_CameraIntrinsics;
        private bool m_Initialized = false;
        private bool m_IsRendering = false;
        private DateTime m_LastDepthUpdateTimestamp;
        private Vector3[] m_BoundingBoxVertices;

        /// <summary>
        /// Initialize the debug console and the HashSet.
        /// </summary>
        protected void Start()
        {
            // Default texture, will be updated each frame.
            m_DepthTexture = new Texture2D(k_DefaultTextureSize, k_DefaultTextureSize);

            m_BoundingBoxVertices = Utilities.GetBoundingBoxVertices(
                BoundingBoxOrigin, BoundingBoxDimensions);

            ClearFreeSpaceWorldMap();
            ClearFreeSpaceScreenMap();
        }

        /// <summary>
        /// Updates the depth map and the collision detection event.
        /// </summary>
        protected void Update()
        {
            UpdateScreenOrientation();
            DateTime now = DateTime.Now;
            if ((now - m_LastDepthUpdateTimestamp).TotalSeconds >= 1 / DepthPollingFrameRate)
            {
                Frame.CameraImage.UpdateDepthTexture(ref m_DepthTexture);

                // Waits until MotionStereo provides real data.
                if (!m_Initialized && m_DepthTexture.width > k_DefaultTextureSize
                    && m_DepthTexture.height > k_DefaultTextureSize)
                {
                    InitializeCameraIntrinsics();
                }

                m_LastDepthUpdateTimestamp = now;
            }

            UpdateTouch();
        }

        /// <summary>
        /// Updates touch event. Triggers free space identification when touching.
        /// </summary>
        protected void UpdateTouch()
        {
            // If the player has not touched the screen, we are done with this update.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            // Raycasts against the location the player touched to search for planes.
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
                TrackableHitFlags.FeaturePointWithSurfaceNormal;

            if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
            {
                // Uses hit pose and camera pose to check if hittest is from the
                // back of the plane, if it is, no need to create the anchor.
                if (!((hit.Trackable is DetectedPlane) &&
                    Vector3.Dot(Camera.main.transform.position - hit.Pose.position,
                        hit.Pose.rotation * Vector3.up) < 0))
                {
                    // Creates an anchor to allow ARCore to track the hitpoint.
                    m_TouchAnchor = hit.Trackable.CreateAnchor(hit.Pose);

                    if (m_IsRendering)
                    {
                        return;
                    }

                    m_IsRendering = true;
                    var renderThread = new Thread(RenderFreeSpace);
                    renderThread.Start();
                }
            }
        }

        /// <summary>
        /// Tests if worldPosition is behind the physical environment.
        /// </summary>
        /// <param name="worldPosition">
        /// The position of the virtual object in the world space.
        /// </param>
        /// <returns>True if collision occurs.</returns>
        private bool TestCollisionAtSingleWorldPoint(Vector3 worldPosition)
        {
            // Computes the environment's depth.
            var screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            var normalizedScreenPosition = new Vector3(
              Mathf.Clamp01(1f - (screenPosition.y / Screen.currentResolution.height)),
              Mathf.Clamp01(1f - (screenPosition.x / Screen.currentResolution.width)),
              screenPosition.z);

            var environmentDepth = FetchEnvironmentDepth(normalizedScreenPosition);

            // Computes the virtual object's depth.
            var targetDepth = normalizedScreenPosition.z;

            return targetDepth > environmentDepth + VertexCollisionThresholdInMeters;
        }

        /// <summary>
        /// Renders free space.
        /// </summary>
        private void RenderFreeSpace()
        {
            // Updates the current anchor's world position and screen position.
            var worldPosition = m_TouchAnchor.transform.position;
            var screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            var normalizedScreenPoint = new Vector3(
                Mathf.Clamp01(1f - (screenPosition.y / Screen.currentResolution.height)),
                Mathf.Clamp01(1f - (screenPosition.x / Screen.currentResolution.width)),
                screenPosition.z);
            screenPosition.z = FetchEnvironmentDepth(normalizedScreenPoint);

            // Intializes the free space maps.
            ClearFreeSpaceScreenMap();
            AddWorldVertexToHashSet(worldPosition);
            VisualizeWorldRegion(worldPosition, screenPosition.z);
            AddScreenPointToHashSet(normalizedScreenPoint.GetXY());

            // Searches the free map in the screen space.
            Queue<Vector3> queue = new Queue<Vector3>();
            queue.Enqueue(normalizedScreenPoint);
            int totalQuads = 1;
            int iterations = 0;

            // Breath-first search to avoid obstacles in the screen and the world space.
            while (queue.Count > 0)
            {
                ++iterations;
                if (iterations > k_MaxIterations)
                {
                    break;
                }

                var front = queue.Peek();
                queue.Dequeue();

                foreach (var dir in k_Directions)
                {
                    var nextScreenPosition = front + dir;
                    if (IsScreenPointRegionExplored(nextScreenPosition.GetXY()))
                    {
                        continue;
                    }

                    nextScreenPosition.z = FetchEnvironmentDepth(nextScreenPosition);

                    var nextWorldPosition = ComputeVertexInWorldSpace(nextScreenPosition);
                    AddScreenPointToHashSet(nextScreenPosition.GetXY());

                    if (Mathf.Abs(nextWorldPosition.y - worldPosition.y) > ElevationThreshold)
                    {
                        continue;
                    }

                    // After a free space is found, we continue the search.
                    if (!TestCollisionOfBoxAtWorldPoint(nextWorldPosition, m_BoundingBoxVertices))
                    {
                        AddWorldVertexToHashSet(nextWorldPosition);
                        VisualizeWorldRegion(nextWorldPosition, nextScreenPosition.z);
                        queue.Enqueue(nextScreenPosition);
                        totalQuads += 1;
                    }
                }
            }

            m_IsRendering = false;
        }

        private bool TestCollisionOfBoxAtWorldPoint(Vector3 worldPosition, Vector3[] boundingBox)
        {
            foreach (var vertex in boundingBox)
            {
                if (TestCollisionAtSingleWorldPoint(worldPosition + vertex))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the visualization of the free space.
        /// </summary>
        /// <param name="worldPoint">
        /// The world position of the free space visualizer (i.e., quad or circle prefabs).
        /// </param>
        /// <param name="depthToCamera">
        /// Depth to camera to adjust the visualizer scales.
        /// </param>
        private void VisualizeWorldRegion(Vector3 worldPoint, float depthToCamera)
        {
            // Instantiates Andy model at the hit pose.
            var mesh = Instantiate(FreeSpaceVisualizerPrefab, worldPoint, Quaternion.identity);

            // Rescale the distance to camera to a reverse exponential mapping towards 5.
            var depthScale = ((Mathf.Exp(Mathf.Min(depthToCamera, k_MaxDepthInMeters)
                                 / k_MaxDepthInMeters) - 1f) * k_DiskScale) + 1f;
            mesh.transform.localScale = mesh.transform.localScale * depthScale;
        }

        /// <summary>
        /// Clears the free space hashmaps in the world's and the screen space.
        /// </summary>
        private void ClearFreeSpaceWorldMap()
        {
            for (int i = 0; i < m_WorldSpaceMap.Length; ++i)
            {
                m_WorldSpaceMap[i] = false;
            }
        }

        /// <summary>
        /// Clears the free space hashmaps in the world's and the screen space.
        /// </summary>
        private void ClearFreeSpaceScreenMap()
        {
            for (int i = 0; i < m_ScreenSpaceMap.Length; ++i)
            {
                m_ScreenSpaceMap[i] = false;
            }
        }

        /// <summary>
        /// Adds vertex in the world space to HashSet.
        /// </summary>
        /// <param name="worldPosition">The position of the vertex in the world space.</param>
        private void AddWorldVertexToHashSet(Vector3 worldPosition)
        {
            var hashId = GetWorldPointHashId(worldPosition);
            m_WorldSpaceMap[hashId] = true;
        }

        /// <summary>
        /// Adds vertex in the world space to HashSet.
        /// </summary>
        /// <param name="worldPosition">
        /// The position of the vertex in the world space.
        /// </param>
        /// <returns>Hasing ID up to k_FreeSpaceVolumeMaxSize ^ 3.</returns>
        private bool IsWorldRegionExplored(Vector3 worldPosition)
        {
            var hashId = GetWorldPointHashId(worldPosition);
            if (hashId < 0 || hashId >= m_WorldSpaceMap.Length)
            {
                return true;
            }

            return m_WorldSpaceMap[hashId];
        }

        /// <summary>
        /// Adds vertex in the world space to HashSet.
        /// </summary>
        /// <param name="normalizedScreenPosition">
        /// The normalized position of the vertex in the screen space.
        /// </param>
        private void AddScreenPointToHashSet(Vector2 normalizedScreenPosition)
        {
            var hashId = GetScreenPointHashId(normalizedScreenPosition);
            m_ScreenSpaceMap[hashId] = true;
        }

        /// <summary>
        /// Adds vertex in the world space to HashSet.
        /// </summary>
        /// <param name="normalizedScreenPosition">
        /// The normalized position of the vertex in the screen space.
        /// </param>
        /// <returns>Hasing ID up to k_FreeSpaceVolumeMaxSize ^ 3.</returns>
        private bool IsScreenPointRegionExplored(Vector2 normalizedScreenPosition)
        {
            var hashId = GetScreenPointHashId(normalizedScreenPosition);
            if (hashId < 0 || hashId >= m_ScreenSpaceMap.Length)
            {
                return true;
            }

            return m_ScreenSpaceMap[hashId];
        }

        /// <summary>
        /// Gets the world spae vertex's hasing id.
        /// </summary>
        /// <param name="position">The position of the vertex in the screen space.</param>
        /// <returns>Hasing ID up to k_FreeSpaceVolumeMaxSize ^ 3.</returns>
        private int GetWorldPointHashId(Vector3 position)
        {
            // Bounds the position into the free space volume around the anchor.
            var bounds = FreeSpaceVolumeSizeInMeters;
            position.x = Mathf.Clamp(position.x, -bounds.x, bounds.x) + bounds.x;
            position.y = Mathf.Clamp(position.y, -bounds.y, bounds.y) + bounds.y;
            position.z = Mathf.Clamp(position.z, -bounds.z, bounds.z) + bounds.z;

            // Scales the position to fit into bins.
            position = position / FreeSpaceAnchorGapInMeters;
            int binX = (int)Mathf.Round(position.x);
            int binY = (int)Mathf.Round(position.y);
            int binZ = (int)Mathf.Round(position.z);

            // Calculates the hashing ID based on the bin number.
            int hashId = (binY * k_FreeSpaceEdgeSize * k_FreeSpaceEdgeSize) +
                         (binX * k_FreeSpaceEdgeSize) + binZ;

            return hashId;
        }

        /// <summary>
        /// Gets the world space vertex's hasing id.
        /// </summary>
        /// <param name="position">The position of the vertex in the screen space.</param>
        /// <returns>Hasing ID up to k_FreeSpaceVolumeMaxSize ^ 3.</returns>
        private int GetScreenPointHashId(Vector2 position)
        {
            int binX = (int)Mathf.Round(Mathf.Clamp01(position.x) * k_ScreenMaxBinsX);
            int binY = (int)Mathf.Round(Mathf.Clamp01(position.y) * k_ScreenMaxBinsY);
            if (binX == k_ScreenMaxBinsX)
            {
                binX = k_ScreenMaxBinsX - 1;
            }

            if (binY == k_ScreenMaxBinsX)
            {
                binY = k_ScreenMaxBinsY - 1;
            }

            int hashId = (binY * k_ScreenMaxBinsX) + binX;
            return hashId;
        }

        /// <summary>
        /// Fetches the depth value of the physical environment corresponding to the screenPosition.
        /// </summary>
        /// <param name="screenPosition">The position of the vertex in the screen space.</param>
        /// <returns>The depth value in meters.</returns>
        private float FetchEnvironmentDepth(Vector3 screenPosition)
        {
            int depthY = (int)(screenPosition.y * m_DepthTexture.height);
            int depthX = (int)(screenPosition.x * m_DepthTexture.width);

            // Obtains the depth value in 16 bits.
#if UNITY_2018_3_OR_NEWER
            var depthData = m_DepthTexture.GetRawTextureData<short>();
            var depthIndex = depthY * m_DepthTexture.width + depthX;
            var depthInShort = depthData[depthIndex];
#else
            var depthData = m_DepthTexture.GetRawTextureData();
            var depthIndex = ((depthY * m_DepthTexture.width) + depthX) * 2;
            byte[] value = new byte[2];
            value[0] = depthData[depthIndex];
            value[1] = depthData[depthIndex + 1];
            var depthInShort = BitConverter.ToInt16(value, 0);
#endif

            var result = Mathf.Min(depthInShort, k_ArCoreMaxDepthMM) * k_ArCoreMMToM;

            return result;
        }

        /// <summary>
        /// Queries and correctly scales camera intrinsics for depth to vertex reprojection.
        /// </summary>
        private void InitializeCameraIntrinsics()
        {
            // Gets the camera parameters to create the required number of vertices.
            m_CameraIntrinsics = Frame.CameraImage.TextureIntrinsics;

            // Scales camera intrinsics to the depth map size.
            Vector2 intrinsicsScale;
            intrinsicsScale.x = m_DepthTexture.width / (float)m_CameraIntrinsics.ImageDimensions.x;
            intrinsicsScale.y = m_DepthTexture.height / (float)m_CameraIntrinsics.ImageDimensions.y;

            m_CameraIntrinsics.FocalLength = Utilities.MultiplyVector2(
                m_CameraIntrinsics.FocalLength, intrinsicsScale);
            m_CameraIntrinsics.PrincipalPoint = Utilities.MultiplyVector2(
                m_CameraIntrinsics.PrincipalPoint, intrinsicsScale);
            m_CameraIntrinsics.ImageDimensions =
                new Vector2Int(m_DepthTexture.width, m_DepthTexture.height);

            m_Initialized = true;
        }

        /// <summary>
        /// Reprojects a depth point to a 3D vertex given the image x, y coordinates and depth z.
        /// </summary>
        /// <param name="x">Image coordinate x value.</param>
        /// <param name="y">Image coordinate y value.</param>
        /// <param name="z">Depth z from the depth map.</param>
        /// <returns>Computed 3D vertex in world space.</returns>
        private Vector3 ComputeVertexInCameraSpace(int x, int y, float z)
        {
            Vector3 vertex = Vector3.negativeInfinity;

            if (z > 0)
            {
                float vertex_x = (x - m_CameraIntrinsics.PrincipalPoint.x) * z /
                    m_CameraIntrinsics.FocalLength.x;
                float vertex_y = (y - m_CameraIntrinsics.PrincipalPoint.y) * z /
                    m_CameraIntrinsics.FocalLength.y;
                vertex.x = vertex_x;
                vertex.y = -vertex_y;
                vertex.z = z;
            }

            return vertex;
        }

        /// <summary>
        /// Computes world-space vertex position from normalized screen position.
        /// </summary>
        /// <param name="normalizedScreenPosition">Normalized screen position.</param>
        /// <returns>Computed 3D vertex in world space.</returns>
        private Vector3 ComputeVertexInWorldSpace(Vector3 normalizedScreenPosition)
        {
            var vertex = ComputeVertexInCameraSpace(
                (int)(normalizedScreenPosition.x * m_DepthTexture.width),
                (int)(normalizedScreenPosition.y * m_DepthTexture.height),
                FetchEnvironmentDepth(normalizedScreenPosition));

            if (vertex == Vector3.negativeInfinity)
            {
                return Vector3.negativeInfinity;
            }

            vertex = (Camera.main.cameraToWorldMatrix *
                m_ScreenRotation).MultiplyPoint(-vertex);
            return vertex;
        }

        /// <summary>
        /// Sets a rotation Matrix4x4 to correctly transform the point cloud when the phone is used
        /// in different screen orientations.
        /// </summary>
        private void UpdateScreenOrientation()
        {
            switch (Screen.orientation)
            {
                case ScreenOrientation.Portrait:
                    m_ScreenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90));
                    break;
                case ScreenOrientation.LandscapeLeft:
                    m_ScreenRotation = Matrix4x4.Rotate(Quaternion.identity);
                    break;
                case ScreenOrientation.PortraitUpsideDown:
                    m_ScreenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, -90));
                    break;
                case ScreenOrientation.LandscapeRight:
                    m_ScreenRotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 180));
                    break;
            }
        }
    }
}
