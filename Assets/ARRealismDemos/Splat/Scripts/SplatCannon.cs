//-----------------------------------------------------------------------
// <copyright file="SplatCannon.cs" company="Google LLC">
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
/// Shoot projectiles prefabs.
/// </summary>
public class SplatCannon : MonoBehaviour
{
    /// <summary>
    /// Prefab that is going to be shoot.
    /// </summary>
    public GameObject Projectile;

    /// <summary>
    /// Gravity force that is applied to the motion.
    /// </summary>
    public float Gravity = 10;

    /// <summary>
    /// Initial velocity value.
    /// </summary>
    public float Velocity = 5;

    /// <summary>
    /// Offset from where the projectile starts.
    /// </summary>
    public float ForwardOffset = 0.25f;

    /// <summary>
    /// List of particle materials.
    /// </summary>
    public List<Material> ParticleMaterials;

    /// <summary>
    /// List of splat materials.
    /// </summary>
    public List<Material> SplatMaterials;

    /// <summary>
    /// List of projectile materials.
    /// </summary>
    public List<Material> ProjectileMaterials;

    /// <summary>
    /// If true while a touch is detected the app will shoot.
    /// </summary>
    public bool TouchShoot = false;

    private GameObject m_Root;

    private bool m_Running = true;

    /// <summary>
    /// Clears all the instantiated projectiles.
    /// </summary>
    public void Clear()
    {
        m_Running = false;
        if (m_Root != null)
        {
            foreach (Transform child in m_Root.transform)
            {
                Destroy(child.gameObject);
            }
        }

        StartCoroutine("ReEnable");
    }

    /// <summary>
    /// Shoot a projectile.
    /// </summary>
    public void Shoot()
    {
        if (m_Root == null)
        {
            m_Root = new GameObject("Projectiles");
        }

        Vector3 targetPos = transform.position;
        Vector3 projPos = Camera.main.transform.position +
                  (Camera.main.transform.forward * ForwardOffset);

        float distance = Vector3.Distance(targetPos, projPos);
        Vector3 impulse = CalculateImpulseToDestiny(projPos, Velocity + distance,
                                                    targetPos, Gravity);
        GameObject proj = GameObject.Instantiate<GameObject>(Projectile, m_Root.transform);
        ArcMotion motion = proj.GetComponent<ArcMotion>();
        int index = Random.Range(0, SplatMaterials.Count);
        motion.ParticleMaterial = ParticleMaterials[index];
        motion.SplatMaterial = SplatMaterials[index];
        motion.ProjectileMaterial = ProjectileMaterials[index];
        motion.Initialize(projPos, Gravity, transform.position, transform.rotation);
        motion.AddImpulse(impulse);
    }

    private void OnDestroy()
    {
        Destroy(m_Root);
        m_Root = null;
    }

    private IEnumerator ReEnable()
    {
        yield return new WaitForSeconds(1f);
        m_Running = true;
    }

    private void Start()
    {
        if (SplatMaterials.Count != ParticleMaterials.Count)
        {
            Material referenceProjectileMaterial = ProjectileMaterials[0];
            Material referenceParticleMaterial = ParticleMaterials[0];
            ParticleMaterials.Clear();
            ProjectileMaterials.Clear();

            foreach (Material splatMaterial in SplatMaterials)
            {
                Color color = splatMaterial.color;
                Material particleMaterial = new Material(referenceParticleMaterial);
                Material projectileMaterial = new Material(referenceProjectileMaterial);
                particleMaterial.SetColor("_Color", color);
                projectileMaterial.SetColor("_Color", color);
                ParticleMaterials.Add(particleMaterial);
                ProjectileMaterials.Add(projectileMaterial);
            }
        }
    }

    private void LateUpdate()
    {
        if (TouchShoot && m_Running && Input.GetMouseButtonDown(0)
                    && Input.mousePosition.y < Screen.height * 0.8)
        {
            Shoot();
        }
    }

    private Vector3 CalculateImpulseToDestiny(Vector3 proj_pos, float proj_speed,
                    Vector3 target, float gravity)
    {
        Vector3 delta = target - proj_pos;
        Vector3 deltaXZ = new Vector3(delta.x, 0f, delta.z);

        float speedSquared = proj_speed * proj_speed;
        float speedQuartic = proj_speed * proj_speed * proj_speed * proj_speed;
        float y = delta.y;
        float x = deltaXZ.magnitude;
        float gx = gravity * x;

        float root = speedQuartic - (gravity * ((gravity * (x * x)) + ((2 * y) * speedSquared)));
        if (root < 0)
        {
            return Vector3.zero;
        }

        root = Mathf.Sqrt(root);
        Vector3 groundDir = deltaXZ.normalized;
        float lowAng = Mathf.Atan2(speedSquared - root, gx);
        return ((groundDir * Mathf.Cos(lowAng)) * proj_speed) +
                  ((Vector3.up * Mathf.Sin(lowAng)) * proj_speed);
    }
}
