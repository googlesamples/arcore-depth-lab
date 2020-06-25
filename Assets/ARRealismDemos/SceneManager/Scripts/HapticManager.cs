//-----------------------------------------------------------------------
// <copyright file="HapticManager.cs" company="Google LLC">
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
/// Class for enabling haptic feedback calls in the scene.
/// </summary>
public class HapticManager : MonoBehaviour
{
    // Cache the Manager for performance.
    private static HapticFeedbackManager s_HapticFeedbackManager;

    /// <summary>
    /// Run the haptic feedback.
    /// </summary>
    /// <returns>Returns HapticFeedback execute.</returns>
    public static bool HapticFeedback()
    {
        if (s_HapticFeedbackManager == null)
        {
            s_HapticFeedbackManager = new HapticFeedbackManager();
        }

        return s_HapticFeedbackManager.Execute();
    }

    private class HapticFeedbackManager
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private int HapticFeedbackConstantsKey;
        private AndroidJavaObject UnityPlayer;
#else
#endif

        /// <summary>
        /// Connect the haptic feedback system to Unity.
        /// </summary>
        public HapticFeedbackManager()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            HapticFeedbackConstantsKey=new AndroidJavaClass(
            "android.view.HapticFeedbackConstants").GetStatic<int>("VIRTUAL_KEY");
            UnityPlayer=new AndroidJavaClass (
            "com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>(
            "currentActivity").Get<AndroidJavaObject>("mUnityPlayer");
#else
#endif
        }

        public bool Execute()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return UnityPlayer.Call<bool> ("performHapticFeedback",HapticFeedbackConstantsKey);
#else
            return false;
#endif
        }
    }
}
