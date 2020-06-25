//-----------------------------------------------------------------------
// <copyright file="ArcMotion.cs" company="Google LLC">
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
/// Simulate the arc motion of a projectile.
/// </summary>
public class ArcMotion : MonoBehaviour
{
    /// <summary>
    /// Prefab with the splat object that
    /// will be instantiated on collision.
    /// </summary>
    public GameObject Splat;

    /// <summary>
    /// Max allowed distance from target to consider
    /// that the projectile arrived at the destination.
    /// </summary>
    public float MaxDistanceFromTarget = 0.1f;

    /// <summary>
    /// If the projectile drops bellow this value it will
    /// be considered out of bounds and killed.
    /// </summary>
    public float MaxYValue = 5.0f;

    /// <summary>
    /// The particle material.
    /// </summary>
    public Material ParticleMaterial;

    /// <summary>
    /// The splat material.
    /// </summary>
    public Material SplatMaterial;

    /// <summary>
    /// The projectile material.
    /// </summary>
    public Material ProjectileMaterial;

    private Vector3 m_LastPosition;

    private Vector3 m_Impulse;

    private float m_Gravity;

    private Quaternion m_DestinyRotation;

    private Vector3 m_DestinyPosition;

    /// <summary>
    /// Initialize the arc motion.
    /// </summary>
    /// <param name="initialPosition">The motion initial position.</param>
    /// <param name="gravity">The gravity vector to use.</param>
    /// <param name="destinyPosition">Final destiny position.</param>
    /// <param name="destinyRotation">Final destiny rotation.</param>
    public void Initialize(Vector3 initialPosition, float gravity,
                Vector3 destinyPosition, Quaternion destinyRotation)
    {
        transform.position = initialPosition;
        this.m_Gravity = gravity;
        this.m_DestinyRotation = destinyRotation;
        this.m_DestinyPosition = destinyPosition;
        this.m_LastPosition = transform.position;
    }

    /// <summary>
    /// Adds an impulse force to the motion.
    /// </summary>
    /// <param name="impulse">The impulse vector.</param>
    public void AddImpulse(Vector3 impulse)
    {
        this.m_Impulse += impulse;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float dtSquared = dt * dt;
        Vector3 acceleration = -m_Gravity * Vector3.up;

        Vector3 currentPosition = transform.position;
        Vector3 newPosition = currentPosition + (currentPosition - m_LastPosition)
                    + (m_Impulse * dt) + (acceleration * dtSquared);
        m_LastPosition = currentPosition;
        transform.position = newPosition;
        transform.forward = newPosition - m_LastPosition;

        m_Impulse = Vector3.zero;

        if (Vector3.Distance(transform.position, m_DestinyPosition) < MaxDistanceFromTarget)
        {
            GameObject splat = GameObject.Instantiate<GameObject>(Splat, m_DestinyPosition,
                                                                  m_DestinyRotation,
                                                                  transform.parent.transform);
            splat.GetComponent<MeshRenderer>().sharedMaterial = SplatMaterial;
            splat.GetComponentsInChildren<ParticleSystemRenderer>()[0].sharedMaterial =
                                ParticleMaterial;
            Destroy(gameObject);
        }

        if (transform.position.y < -MaxYValue)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GetComponent<MeshRenderer>().sharedMaterial = ProjectileMaterial;
    }
}
