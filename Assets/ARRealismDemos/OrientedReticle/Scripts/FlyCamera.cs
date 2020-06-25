//-----------------------------------------------------------------------
// <copyright file="FlyCamera.cs" company="Google LLC">
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
using UnityEngine;

/// <summary>
/// Small camera controller that only manipulates the orientation.
/// </summary>
public class FlyCamera : MonoBehaviour
{
    /// <summary>
    /// Camera rotation speed.
    /// </summary>
    public float CameraRotationSpeed = 50;

    private float m_RotationX = 0.0f;
    private float m_RotationY = 0.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        m_RotationX = this.transform.eulerAngles.y;
        m_RotationY = -this.transform.eulerAngles.x;
    }

    private void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            m_RotationX += Input.GetAxis("Mouse X") * CameraRotationSpeed * Time.deltaTime;
            m_RotationY += Input.GetAxis("Mouse Y") * CameraRotationSpeed * Time.deltaTime;
            m_RotationY = Mathf.Clamp(m_RotationY, -40, 40);
            m_RotationX = Mathf.Clamp(m_RotationX, -40, 40);

            transform.rotation = Quaternion.AngleAxis(m_RotationX, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(m_RotationY, Vector3.left);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState =
                (Cursor.lockState == CursorLockMode.None) ?
                    CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
