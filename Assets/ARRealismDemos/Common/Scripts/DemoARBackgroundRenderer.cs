//-----------------------------------------------------------------------
// <copyright file="DemoARBackgroundRenderer.cs" company="Google LLC">
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

        private static readonly float k_BlackScreenDuration = 0.5f;

        private static readonly float k_FadingInDuration = 0.5f;

        private RenderTexture m_TemporaryRenderTexture;

        private Material m_CurrentMaterial;

        private Camera m_Camera;

        private Texture m_TransitionImageTexture;

        private BackgroundTransitionState m_TransitionState = BackgroundTransitionState.BlackScreen;

        private float m_CurrentStateElapsed = 0.0f;

        private bool m_SessionEnabled = false;

        private bool m_UserInvertCullingValue = false;

        private CameraClearFlags m_CameraClearFlags = CameraClearFlags.Skybox;

        private CommandBuffer m_CommandBuffer = null;

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

            m_CurrentMaterial = materialToSwap;

            EnableARBackgroundRendering();
        }

        /// <summary>
        /// Reset the material to the original one.
        /// </summary>
        public void ResetBackgroundMaterial()
        {
            DisableARBackgroundRendering();

            m_CurrentMaterial = BackgroundMaterial;

            EnableARBackgroundRendering();
        }

        /// <summary>
        /// Enables AR Background rendering.
        /// </summary>
        private void EnableARBackgroundRendering()
        {
            m_CurrentMaterial.SetTexture("_TransitionIconTex", m_TransitionImageTexture);

            if (m_CurrentMaterial == null || m_Camera == null)
            {
                return;
            }

            m_CameraClearFlags = m_Camera.clearFlags;
            m_Camera.clearFlags = CameraClearFlags.Depth;

            m_CommandBuffer = new CommandBuffer();

            m_CommandBuffer.Blit(m_CurrentMaterial.mainTexture,
                BuiltinRenderTextureType.CameraTarget, m_CurrentMaterial);

            m_Camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, m_CommandBuffer);
            m_Camera.AddCommandBuffer(CameraEvent.BeforeGBuffer, m_CommandBuffer);
        }

        /// <summary>
        /// Disables AR Background rendering.
        /// </summary>
        private void DisableARBackgroundRendering()
        {
            if (m_CommandBuffer == null || m_Camera == null)
            {
                return;
            }

            m_Camera.clearFlags = m_CameraClearFlags;

            m_Camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, m_CommandBuffer);
            m_Camera.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, m_CommandBuffer);
        }

        private void OnEnable()
        {
            m_CurrentMaterial = BackgroundMaterial;

            if (m_CurrentMaterial == null)
            {
                Debug.LogError("ArCameraBackground:: No material assigned.");
                return;
            }

            LifecycleManager.Instance.OnSessionSetEnabled += _OnSessionSetEnabled;

            m_Camera = Camera.main;

            m_TransitionImageTexture = Resources.Load<Texture2D>("ViewInARIcon");

            EnableARBackgroundRendering();
        }

        private void OnDisable()
        {
            LifecycleManager.Instance.OnSessionSetEnabled -= _OnSessionSetEnabled;
            m_TransitionState = BackgroundTransitionState.BlackScreen;
            m_CurrentStateElapsed = 0.0f;

            m_Camera.ResetProjectionMatrix();

            DisableARBackgroundRendering();
        }

        private void OnPreRender()
        {
            m_UserInvertCullingValue = GL.invertCulling;
            var sessionComponent = LifecycleManager.Instance.SessionComponent;
            if (sessionComponent != null &&
                sessionComponent.DeviceCameraDirection == DeviceCameraDirection.FrontFacing)
            {
                GL.invertCulling = true;
            }

            if (GrabBackgroundTexture)
            {
                m_Camera.targetTexture = BackgroundRenderTexture;
            }
        }

        private void OnPostRender()
        {
            if (GrabBackgroundTexture)
            {
                m_Camera.targetTexture = null;

                if (RenderToScreen)
                {
                    Graphics.Blit(BackgroundRenderTexture, null as RenderTexture);
                }
            }

            GL.invertCulling = m_UserInvertCullingValue;
        }

        private void Start()
        {
            BackgroundRenderTexture = new RenderTexture(Screen.width, Screen.height, 16);
            BackgroundRenderTexture.Create();
        }

        private void Update()
        {
            m_CurrentStateElapsed += Time.deltaTime;
            _UpdateState();
            _UpdateShaderVariables();
        }

        private void _UpdateState()
        {
            if (!m_SessionEnabled && m_TransitionState != BackgroundTransitionState.BlackScreen)
            {
                m_TransitionState = BackgroundTransitionState.BlackScreen;
                m_CurrentStateElapsed = 0.0f;
            }
            else if (m_SessionEnabled &&
                     m_TransitionState == BackgroundTransitionState.BlackScreen &&
                     m_CurrentStateElapsed > k_BlackScreenDuration)
            {
                m_TransitionState = BackgroundTransitionState.FadingIn;
                m_CurrentStateElapsed = 0.0f;
            }
            else if (m_SessionEnabled &&
                     m_TransitionState == BackgroundTransitionState.FadingIn &&
                     m_CurrentStateElapsed > k_FadingInDuration)
            {
                m_TransitionState = BackgroundTransitionState.CameraImage;
                m_CurrentStateElapsed = 0.0f;
            }
        }

        private void _UpdateShaderVariables()
        {
            const string brightnessVar = "_Brightness";
            if (m_TransitionState == BackgroundTransitionState.BlackScreen)
            {
                m_CurrentMaterial.SetFloat(brightnessVar, 0.0f);
            }
            else if (m_TransitionState == BackgroundTransitionState.FadingIn)
            {
                m_CurrentMaterial.SetFloat(
                    brightnessVar,
                    _CosineLerp(m_CurrentStateElapsed, k_FadingInDuration));
            }
            else
            {
                m_CurrentMaterial.SetFloat(brightnessVar, 1.0f);
            }

            // Set transform of the transition image texture, it may be visible or invisible based
            // on lerp value.
            const string transformVar = "_TransitionIconTexTransform";
            m_CurrentMaterial.SetVector(transformVar, _TextureTransform());

            // Background texture should not be rendered when the session is disabled or
            // there is no camera image texture available.
            if (m_TransitionState == BackgroundTransitionState.BlackScreen ||
                Frame.CameraImage.Texture == null)
            {
                return;
            }

            const string mainTexVar = "_MainTex";
            const string topLeftRightVar = "_UvTopLeftRight";
            const string bottomLeftRightVar = "_UvBottomLeftRight";

            m_CurrentMaterial.SetTexture(mainTexVar, Frame.CameraImage.Texture);

            var uvQuad = Frame.CameraImage.TextureDisplayUvs;
            m_CurrentMaterial.SetVector(
                topLeftRightVar,
                new Vector4(
                    uvQuad.TopLeft.x, uvQuad.TopLeft.y, uvQuad.TopRight.x, uvQuad.TopRight.y));
            m_CurrentMaterial.SetVector(
                bottomLeftRightVar,
                new Vector4(uvQuad.BottomLeft.x, uvQuad.BottomLeft.y, uvQuad.BottomRight.x,
                    uvQuad.BottomRight.y));

            m_Camera.projectionMatrix = Frame.CameraImage.GetCameraProjectionMatrix(
                m_Camera.nearClipPlane, m_Camera.farClipPlane);
        }

        private void _OnSessionSetEnabled(bool sessionEnabled)
        {
            m_SessionEnabled = sessionEnabled;
            if (!m_SessionEnabled)
            {
                _UpdateState();
                _UpdateShaderVariables();
            }
        }

        private float _CosineLerp(float elapsed, float duration)
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
        private Vector4 _TextureTransform()
        {
            float transitionWidthTransform = (m_TransitionImageTexture.width - Screen.width) /
                (2.0f * m_TransitionImageTexture.width);
            float transitionHeightTransform = (m_TransitionImageTexture.height - Screen.height) /
                (2.0f * m_TransitionImageTexture.height);
            return new Vector4(
                (float)Screen.width / m_TransitionImageTexture.width,
                transitionWidthTransform,
                (float)Screen.height / m_TransitionImageTexture.height,
                transitionHeightTransform);
        }
    }
}
