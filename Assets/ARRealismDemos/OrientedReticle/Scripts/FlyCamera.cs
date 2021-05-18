//-----------------------------------------------------------------------
// <copyright file="FlyCamera.cs" company="Google LLC">
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

/// <summary>
/// Small camera controller that only manipulates the orientation.
/// </summary>
public class FlyCamera : MonoBehaviour
{
    /// <summary>
    /// Camera rotation speed.
    /// </summary>
    public float CameraRotationSpeed = 50;

    private float _rotationX = 0.0f;
    private float _rotationY = 0.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _rotationX = this.transform.eulerAngles.y;
        _rotationY = -this.transform.eulerAngles.x;
    }

    private void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            _rotationX += Input.GetAxis("Mouse X") * CameraRotationSpeed * Time.deltaTime;
            _rotationY += Input.GetAxis("Mouse Y") * CameraRotationSpeed * Time.deltaTime;
            _rotationY = Mathf.Clamp(_rotationY, -40, 40);
            _rotationX = Mathf.Clamp(_rotationX, -40, 40);

            transform.rotation = Quaternion.AngleAxis(_rotationX, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(_rotationY, Vector3.left);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState =
                (Cursor.lockState == CursorLockMode.None) ?
                    CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
