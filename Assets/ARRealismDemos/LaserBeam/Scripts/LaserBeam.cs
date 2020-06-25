//-----------------------------------------------------------------------
// <copyright file="LaserBeam.cs" company="Google LLC">
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
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Renders a bouncing laser beam starting from the camera to the touch point in mixed reality.
/// </summary>
public class LaserBeam : MonoBehaviour
{
    /// <summary>
    /// The material attached with the laser.
    /// </summary>
    public Material LaserBeamMaterial;

    /// <summary>
    /// The material attached with the hit quad.
    /// </summary>
    public Material LaserSpotMaterial;

    /// <summary>
    /// Laser beam velocity.
    /// </summary>
    public float LaserVelocity = 0.75f;

    /// <summary>
    /// Message notification for when laser beam is out of camera view.
    /// </summary>
    public HelpBalloonController PopupMessage;

    /// <summary>
    /// For each valid depth pixel, the neighborhood in a square window of radius
    /// window_radius_pixels is searched.
    /// </summary>
    private const int k_WindowRadiusPixels = 4;

    /// <summary>
    /// Each valid neighbor is checked whether it is an inlier in depth. Inliers
    /// are defined as within a distance of (outlier_depth_ratio * depth) of a
    /// point at 'depth' depth.
    /// </summary>
    private const float k_OutlierDepthRatio = 0.2f;

    private const float k_LaserHitQuadScale = 0.07f;

    private bool m_AllowTouch = false;
    private Vector3 m_LaserOffset = new Vector3(0, -0.1f, 0);
    private Vector3 m_StartPosition = Vector3.zero;
    private Vector3 m_LaserDirection = Vector3.zero;
    private Vector3 m_LaserPosition = Vector3.zero;
    private Vector2 m_LastTouchPosition = new Vector2(-1, -1);
    private LineRenderer m_LaserRenderer;
    private List<GameObject> m_NormalDebuggers = new List<GameObject>();

    private LaserState m_CurrentState = LaserState.Inactive;
    private LaserState m_NextState = LaserState.Inactive;

    private enum LaserState
    {
        Inactive,
        Fire,
        Update,
        OutOfScreen,
    }

    /// <summary>
    /// Shoots a new laser, if a laser is active, it gets reset.
    /// </summary>
    public void Shoot()
    {
        m_NextState = LaserState.Fire;
    }

    /// <summary>
    /// Resets a current ongoing laser.
    /// </summary>
    public void Reset()
    {
        m_NextState = LaserState.Inactive;
        PopupMessage.FadeOut();
    }

    /// <summary>
    /// Returns whether laser has been triggered.
    /// </summary>
    /// <returns>Returns true if the laser is currently active.</returns>
    public bool HasLaserBeenTriggered()
    {
        return m_CurrentState != LaserState.Inactive;
    }

    /// <summary>
    /// Initializes the line renderer for the laser and the normal vector.
    /// </summary>
    private void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;

        m_LaserRenderer = gameObject.AddComponent<LineRenderer>();
        m_LaserRenderer.material = LaserBeamMaterial;
        m_LaserRenderer.startWidth = 0.13f;
        m_LaserRenderer.endWidth = 0.13f;
        m_LaserRenderer.alignment = LineAlignment.View;
        m_LaserRenderer.positionCount = 0;

        // Makes sure DepthSource is initialized.
        bool initializeDepthSource = DepthSource.Initialized;
    }

    private void ResetBeam()
    {
        foreach (var debugger in m_NormalDebuggers)
        {
            Destroy(debugger);
        }

        m_NormalDebuggers.Clear();
        m_LaserRenderer.positionCount = 0;
    }

    /// <summary>
    /// Renders the normal vector for debugging.
    /// </summary>
    /// <param name="hitPosition">The hit point in the world space.</param>
    /// <param name="normal">The normal vector in the world space.</param>
    private void VisualizeNormalVector(Vector3 hitPosition, Vector3 normal)
    {
        var debugger = GameObject.CreatePrimitive(PrimitiveType.Quad);
        debugger.transform.SetParent(gameObject.transform);
        debugger.transform.localScale = debugger.transform.localScale * k_LaserHitQuadScale;
        debugger.GetComponent<Renderer>().material = LaserSpotMaterial;

        debugger.transform.position = hitPosition;
        debugger.transform.rotation =
          Quaternion.FromToRotation(debugger.transform.up, normal) * debugger.transform.rotation;
        m_NormalDebuggers.Add(debugger);
    }

    private void Update()
    {
        if (m_NextState != m_CurrentState)
        {
            switch (m_NextState)
            {
                case LaserState.Inactive:
                    OnInactiveStateEnter();
                    break;
                case LaserState.Fire:
                    OnFireStateEnter();
                    break;
                case LaserState.Update:
                    OnUpdateStateEnter();
                    break;
                case LaserState.OutOfScreen:
                    OnOutOfScreenStateEnter();
                    break;
            }

            m_CurrentState = m_NextState;
        }

        switch (m_CurrentState)
        {
            case LaserState.Fire:
                OnFireStateUpdate();
                break;
            case LaserState.Update:
                OnUpdateStateUpdate();
                break;
            case LaserState.OutOfScreen:
                OnOutOfScreenStateUpdate();
                break;
        }
    }

    private void OnInactiveStateEnter()
    {
        ResetBeam();
    }

    private void OnFireStateEnter()
    {
        ResetBeam();

        if (!m_AllowTouch)
        {
            m_LastTouchPosition.x = Screen.width / 2.0f;
            m_LastTouchPosition.y = Screen.height / 2.0f;
        }

        // Shoots the laser from the camera.
        m_StartPosition = Camera.main.transform.position + m_LaserOffset;

        // Computes the first hit position in the world space.
        Vector3 hitPosition = DepthSource.GetVertexInWorldSpaceFromScreenXY(
                            (int)m_LastTouchPosition.x, (int)m_LastTouchPosition.y);

        m_LaserDirection = (hitPosition - m_StartPosition).normalized;
        m_LaserPosition = m_StartPosition + (m_LaserDirection * 0.1f);
    }

    private void OnFireStateUpdate()
    {
        // Checks for valid laser direction.
        if (m_LaserDirection.magnitude >= 0.95f)
        {
            m_LaserRenderer.positionCount = 3;
            m_LaserRenderer.SetPosition(0, m_StartPosition);
            m_LaserRenderer.SetPosition(1, (m_StartPosition + m_LaserPosition) * 0.5f);
            m_LaserRenderer.SetPosition(2, m_LaserPosition);

            m_NextState = LaserState.Update;
        }
        else
        {
            m_NextState = LaserState.Inactive;
        }
    }

    private void OnUpdateStateEnter()
    {
        PopupMessage.FadeOut();
    }

    private void OnUpdateStateUpdate()
    {
        m_LaserPosition += m_LaserDirection * LaserVelocity * Time.deltaTime;
        m_LaserRenderer.SetPosition(m_LaserRenderer.positionCount++, m_LaserPosition);

        var screenPosition = Camera.main.WorldToScreenPoint(m_LaserPosition);

        if ((screenPosition.x < 0) || (screenPosition.x > Screen.width) ||
            (screenPosition.y < 0) || (screenPosition.y > Screen.height))
        {
            // Clamps to screen space.
            screenPosition.x = Mathf.Clamp(screenPosition.x, 0, Screen.width - 1);
            screenPosition.y = Mathf.Clamp(screenPosition.y, 0, Screen.height - 1);

            m_NextState = LaserState.OutOfScreen;
        }

        Vector2 hitUv = ScreenPointToViewCoordinate(screenPosition);
        Vector2 depthUV = DepthSource.ScreenToDepthUV(hitUv);
        float depth = DepthSource.GetDepthFromUV(depthUV, DepthSource.DepthArray);

        if (screenPosition.z > depth)
        {
            Vector3 CurrentDirection = m_LaserDirection;
            Vector3 normal = ComputeNormalMapFromDepthWeightedMeanGradient(hitUv);

            // Checks if the normal is valid.
            if (float.IsInfinity(normal.x) ||
                float.IsNegativeInfinity(normal.x) ||
                float.IsNaN(normal.x))
            {
                return;
            }

            // Transforms normal to the world space.
            normal = Camera.main.transform.TransformDirection(normal);
            m_LaserDirection = Reflect(CurrentDirection, normal);

            // Adds collision quad.
            VisualizeNormalVector(m_LaserPosition, normal);

            m_LaserPosition = m_LaserPosition + (m_LaserDirection * LaserVelocity * Time.deltaTime);
            m_LaserRenderer.SetPosition(m_LaserRenderer.positionCount++, m_LaserPosition);
        }
    }

    private void OnOutOfScreenStateEnter()
    {
        if (HasLaserBeenTriggered())
        {
            PopupMessage.FadeIn();
        }
    }

    private void OnOutOfScreenStateUpdate()
    {
        Vector3 nextPosition =
          m_LaserPosition + (m_LaserDirection * LaserVelocity * Time.deltaTime);
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(nextPosition);
        if ((screenPosition.x > 0) && (screenPosition.x < Screen.width) &&
            (screenPosition.y > 0) && (screenPosition.y < Screen.height))
        {
            m_NextState = LaserState.Update;
        }
    }

    /// <summary>
    /// Computes the reflected ray direction based on input ray and the normal vector.
    /// </summary>
    /// <param name="direction">Incoming ray direction.</param>
    /// <param name="normal">The normal vector.</param>
    /// <returns>Outgoing ray direction.</returns>
    private Vector3 Reflect(Vector3 direction, Vector3 normal)
    {
        Vector3 reflection = direction - (2f * Vector3.Dot(normal, direction) * normal);
        return (normal + (reflection * 0.5f)).normalized;
    }

    /// <summary>
    /// Computes the viewport coordinate from a screen point.
    /// </summary>
    /// <param name="screenPosition">Screen position in pixels.</param>
    /// <returns>Viewport coordinate in [0, 1].</returns>
    private Vector2 ScreenPointToViewCoordinate(Vector3 screenPosition)
    {
        return new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
    }

    /// <summary>
    /// Estimates the normal vector for each point based on weighted mean gradients
    /// on neighborhood depth data.
    /// </summary>
    /// <param name="screenUV">The normalized screen uv coordinates.</param>
    /// <returns>The computed normal.</returns>
    private Vector3 ComputeNormalMapFromDepthWeightedMeanGradient(Vector2 screenUV)
    {
        short[] depthMap = DepthSource.DepthArray;
        Vector2 depthUV = DepthSource.ScreenToDepthUV(screenUV);
        Vector2Int depthXY = DepthSource.DepthUVtoXY(depthUV);
        float depth_m = DepthSource.GetDepthFromUV(depthUV, depthMap);

        if (depth_m == DepthSource.InvalidDepthValue)
        {
            return Vector3.negativeInfinity;
        }

        // Iterates over neighbors to compute normal vector.
        float neighbor_corr_x = 0.0f;
        float neighbor_corr_y = 0.0f;
        float outlier_distance_m = k_OutlierDepthRatio * depth_m;
        int radius = k_WindowRadiusPixels;
        float neighbor_sum_confidences_x = 0.0f;
        float neighbor_sum_confidences_y = 0.0f;
        for (int dy = -radius; dy <= radius; ++dy)
        {
            for (int dx = -radius; dx <= radius; ++dx)
            {
                // Self isn't a neighbor.
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                Vector2Int offset = DepthSource.ReorientDepthXY(dx, dy);
                int currentX = depthXY.x + offset.x;
                int currentY = depthXY.y + offset.y;

                // Retrieves neighbor value.
                float neighbor_depth_m = DepthSource.GetDepthFromXY(currentX, currentY, depthMap);

                // Confidence is not currently being packed yet, so for now this hardcoded.
                float neighbor_confidence = 1.0f;
                if (neighbor_depth_m == 0.0)
                {
                    continue;  // Neighbor does not exist.
                }

                float neighbor_distance_m = neighbor_depth_m - depth_m;

                // Checks for outliers.
                if (neighbor_confidence == 0.0f ||
                    Mathf.Abs(neighbor_distance_m) > outlier_distance_m)
                {
                    continue;
                }

                // Updates correlations in each dimension.
                if (dx != 0)
                {
                    neighbor_sum_confidences_x += neighbor_confidence;
                    neighbor_corr_x += neighbor_confidence * neighbor_distance_m / dx;
                }

                if (dy != 0)
                {
                    neighbor_sum_confidences_y += neighbor_confidence;
                    neighbor_corr_y += neighbor_confidence * neighbor_distance_m / dy;
                }
            }
        }

        if (neighbor_sum_confidences_x == 0 && neighbor_sum_confidences_y == 0)
        {
            return Vector3.negativeInfinity;
        }

        // Estimates the normal vector by finding the weighted averages of
        // the surface gradients in x and y.
        float pixel_width_m = depth_m / DepthSource.FocalLength.x;
        float slope_x = neighbor_corr_x / (pixel_width_m * neighbor_sum_confidences_x);
        float slope_y = neighbor_corr_y / (pixel_width_m * neighbor_sum_confidences_y);

        // The normal points towards the camera, so its z-component is negative.
        Vector3 normal = new Vector3(slope_x, -slope_y, -1.0f);
        normal.Normalize();

        return normal;
    }
}
