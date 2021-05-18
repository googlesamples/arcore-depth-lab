//-----------------------------------------------------------------------
// <copyright file="DemoARBackgroundRenderer.cs" company="Google LLC">
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

namespace GoogleARCore
{
    using System.Collections;
    using System.Collections.Generic;
    using GoogleARCoreInternal;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Renders the device's camera as a background to the attached Unity camera component.
    /// When using the front-facing (selfie) camera, this temporarily inverts culling when
    /// rendering.
    /// </summary>
    public class DemoARBackgroundRenderer : MonoBehaviour
    {
        /// <summary>
        /// A material used to render the AR background image.
        /// </summary>
        [Tooltip("A material used to render the AR background image.")]
        public Material BackgroundMaterial;

        /// <summary>
        /// AR background render texture.
        /// </summary>
        public RenderTexture BackgroundRenderTexture;

        /// <summary>
        /// Flag indicating whether the background renderer should render to screen.
        /// </summary>
        public bool RenderToScreen = true;

        /// <summary>
        /// Flag indicating whether the background texture should be grabbed.
        /// </summary>
        public bool GrabBackgroundTexture = false;

        private static readonly float _blackScreenDuration = 0.5f;

        private static readonly float _fadingInDuration = 0.5f;

        private RenderTexture _temporaryRenderTexture;

        private Material _currentMaterial;

        private Camera _camera;

        private Texture _transitionImageTexture;

        private BackgroundTransitionState _transitionState = BackgroundTransitionState.BlackScreen;

        private float _currentStateElapsed = 0.0f;

        private bool _sessionEnabled = false;

        private bool _userInvertCullingValue = false;

        private CameraClearFlags _cameraClearFlags = CameraClearFlags.Skybox;

        private CommandBuffer _commandBuffer = null;

        private enum BackgroundTransitionState
        {
            BlackScreen = 0,
            FadingIn = 1,
            CameraImage = 2,
        }

        /// <summary>
        /// Swap the current material for a new one.
        /// </summary>
        /// <param name="materialToSwap">Material to swap to.</param>
        public void SwapBackgroundMaterial(Material materialToSwap)
        {
            DisableARBackgroundRendering();

            _currentMaterial = materialToSwap;

            EnableARBackgroundRendering();
        }

        /// <summary>
        /// Reset the material to the original one.
        /// </summary>
        public void ResetBackgroundMaterial()
        {
            DisableARBackgroundRendering();

            _currentMaterial = BackgroundMaterial;

            EnableARBackgroundRendering();
        }

        /// <summary>
        /// Enables AR Background rendering.
        /// </summary>
        private void EnableARBackgroundRendering()
        {
            _currentMaterial.SetTexture("_TransitionIconTex", _transitionImageTexture);

            if (_currentMaterial == null || _camera == null)
            {
                return;
            }

            _cameraClearFlags = _camera.clearFlags;
            _camera.clearFlags = CameraClearFlags.Depth;

            _commandBuffer = new CommandBuffer();

            _commandBuffer.Blit(_currentMaterial.mainTexture,
                BuiltinRenderTextureType.CameraTarget, _currentMaterial);

            _camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
            _camera.AddCommandBuffer(CameraEvent.BeforeGBuffer, _commandBuffer);
        }

        /// <summary>
        /// Disables AR Background rendering.
        /// </summary>
        private void DisableARBackgroundRendering()
        {
            if (_commandBuffer == null || _camera == null)
            {
                return;
            }

            _camera.clearFlags = _cameraClearFlags;

            _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
            _camera.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, _commandBuffer);
        }

        private void OnEnable()
        {
            _currentMaterial = BackgroundMaterial;

            if (_currentMaterial == null)
            {
                Debug.LogError("ArCameraBackground:: No material assigned.");
                return;
            }

            LifecycleManager.Instance.OnSessionSetEnabled += OnSessionSetEnabled;

            _camera = Camera.main;

            _transitionImageTexture = Resources.Load<Texture2D>("ViewInARIcon");

            EnableARBackgroundRendering();
        }

        private void OnDisable()
        {
            LifecycleManager.Instance.OnSessionSetEnabled -= OnSessionSetEnabled;
            _transitionState = BackgroundTransitionState.BlackScreen;
            _currentStateElapsed = 0.0f;

            _camera.ResetProjectionMatrix();

            DisableARBackgroundRendering();
        }

        private void OnPreRender()
        {
            _userInvertCullingValue = GL.invertCulling;
            var sessionComponent = LifecycleManager.Instance.SessionComponent;
            if (sessionComponent != null &&
                sessionComponent.DeviceCameraDirection == DeviceCameraDirection.FrontFacing)
            {
                GL.invertCulling = true;
            }

            if (GrabBackgroundTexture)
            {
                _camera.targetTexture = BackgroundRenderTexture;
            }
        }

        private void OnPostRender()
        {
            if (GrabBackgroundTexture)
            {
                _camera.targetTexture = null;

                if (RenderToScreen)
                {
                    Graphics.Blit(BackgroundRenderTexture, null as RenderTexture);
                }
            }

            GL.invertCulling = _userInvertCullingValue;
        }

        private void Start()
        {
            BackgroundRenderTexture = new RenderTexture(Screen.width, Screen.height, 16);
            BackgroundRenderTexture.Create();
        }

        private void Update()
        {
            _currentStateElapsed += Time.deltaTime;
            UpdateState();
            UpdateShaderVariables();
        }

        private void UpdateState()
        {
            if (!_sessionEnabled && _transitionState != BackgroundTransitionState.BlackScreen)
            {
                _transitionState = BackgroundTransitionState.BlackScreen;
                _currentStateElapsed = 0.0f;
            }
            else if (_sessionEnabled &&
                     _transitionState == BackgroundTransitionState.BlackScreen &&
                     _currentStateElapsed > _blackScreenDuration)
            {
                _transitionState = BackgroundTransitionState.FadingIn;
                _currentStateElapsed = 0.0f;
            }
            else if (_sessionEnabled &&
                     _transitionState == BackgroundTransitionState.FadingIn &&
                     _currentStateElapsed > _fadingInDuration)
            {
                _transitionState = BackgroundTransitionState.CameraImage;
                _currentStateElapsed = 0.0f;
            }
        }

        private void UpdateShaderVariables()
        {
            const string brightnessVar = "_Brightness";
            if (_transitionState == BackgroundTransitionState.BlackScreen)
            {
                _currentMaterial.SetFloat(brightnessVar, 0.0f);
            }
            else if (_transitionState == BackgroundTransitionState.FadingIn)
            {
                _currentMaterial.SetFloat(
                    brightnessVar,
                    CosineLerp(_currentStateElapsed, _fadingInDuration));
            }
            else
            {
                _currentMaterial.SetFloat(brightnessVar, 1.0f);
            }

            // Set transform of the transition image texture, it may be visible or invisible based
            // on lerp value.
            const string transformVar = "_TransitionIconTexTransform";
            _currentMaterial.SetVector(transformVar, TextureTransform());

            // Background texture should not be rendered when the session is disabled or
            // there is no camera image texture available.
            if (_transitionState == BackgroundTransitionState.BlackScreen ||
                Frame.CameraImage.Texture == null)
            {
                return;
            }

            const string mainTexVar = "_MainTex";
            const string topLeftRightVar = "_UvTopLeftRight";
            const string bottomLeftRightVar = "_UvBottomLeftRight";

            _currentMaterial.SetTexture(mainTexVar, Frame.CameraImage.Texture);

            var uvQuad = Frame.CameraImage.TextureDisplayUvs;
            _currentMaterial.SetVector(
                topLeftRightVar,
                new Vector4(
                    uvQuad.TopLeft.x, uvQuad.TopLeft.y, uvQuad.TopRight.x, uvQuad.TopRight.y));
            _currentMaterial.SetVector(
                bottomLeftRightVar,
                new Vector4(uvQuad.BottomLeft.x, uvQuad.BottomLeft.y, uvQuad.BottomRight.x,
                    uvQuad.BottomRight.y));

            _camera.projectionMatrix = Frame.CameraImage.GetCameraProjectionMatrix(
                _camera.nearClipPlane, _camera.farClipPlane);
        }

        private void OnSessionSetEnabled(bool sessionEnabled)
        {
            _sessionEnabled = sessionEnabled;
            if (!_sessionEnabled)
            {
                UpdateState();
                UpdateShaderVariables();
            }
        }

        private float CosineLerp(float elapsed, float duration)
        {
            float clampedElapsed = Mathf.Clamp(elapsed, 0.0f, duration);
            return Mathf.Cos(((clampedElapsed / duration) - 1) * (Mathf.PI / 2));
        }

        /// <summary>
        /// Textures transform used in background shader to get texture uv coordinates based on
        /// screen uv.
        /// The transformation follows these equations:
        /// textureUv.x = transform[0] * screenUv.x + transform[1],
        /// textureUv.y = transform[2] * screenUv.y + transform[3].
        /// </summary>
        /// <returns>The transform.</returns>
        private Vector4 TextureTransform()
        {
            float transitionWidthTransform = (_transitionImageTexture.width - Screen.width) /
                (2.0f * _transitionImageTexture.width);
            float transitionHeightTransform = (_transitionImageTexture.height - Screen.height) /
                (2.0f * _transitionImageTexture.height);
            return new Vector4(
                (float)Screen.width / _transitionImageTexture.width,
                transitionWidthTransform,
                (float)Screen.height / _transitionImageTexture.height,
                transitionHeightTransform);
        }
    }
}
