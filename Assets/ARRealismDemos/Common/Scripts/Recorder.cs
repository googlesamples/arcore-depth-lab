//-----------------------------------------------------------------------
// <copyright file="Recorder.cs" company="Google LLC">
//
// Copyright 2021 Google LLC
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
using System.IO;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Recorder for saving a dataset into mp4 and loading a dataset for playing back.
/// </summary>
public class Recorder : MonoBehaviour
{
    /// <summary>
    /// ARCoreExtensions component in Unity Scene.
    /// </summary>
    public ARCoreExtensions Extensions;

    private ARRecordingManager _recordingManager;
    private ARPlaybackManager _playbackManager;

    /// <summary>
    /// Button to start recording.
    /// </summary>
    public Button RecordButton;

    /// <summary>
    /// Button to stop recording.
    /// </summary>
    public Button StopRecordingButton;

    /// <summary>
    /// Button to start playing back.
    /// </summary>
    public Button PlaybackButton;

    /// <summary>
    /// Button to stop playing back.
    /// </summary>
    public Button StopPlayingbackButton;

    /// <summary>
    /// Toggle script of carousel UI.
    /// </summary>
    public ToggleUIVisibility CarouselUIToggle;

    /// <summary>
    /// Game object of the Carousel UI.
    /// </summary>
    public GameObject Carousel3D;

    /// <summary>
    /// Text UI of a timer for recording.
    /// </summary>
    public Text RecordingTimerText;

    // The name of the ARCore session created from the Depth ARCore prefab.
    private const string _depthArcoreSessionName = "DepthARCoreDeviceOnly(Clone)";

    // Default name of the ARCore dataset.
    private const string _defaultDatasetName = "DepthLab.mp4";

    // Default name of the raw point cloud blender.
    private const string _rawPointCloudObjectName = "RawPointCloudBlender";

    // The status of the recorder.
    private RecorderStatus _status = RecorderStatus.Stopped;

    // The temporary filename to save for the recorder.
    private string _filenameToSave;

    // Time when recording starts.
    private float _timeWhenRecorderStarts;
    private ARCoreRecordingConfig _recordingConfig = null;

    /// <summary>
    /// Status of the recorder. Only one workflow is enabled at a time:
    /// 1) Stopped -> ReadyToRecord -> Recording -> Stopped.
    /// </summary>
    protected enum RecorderStatus
    {
        /// <summary>
        /// Initial state and the final state of exiting playback.
        /// </summary>
        Stopped,

        /// <summary>
        /// Transition mode after recording is requested to start but haven't started.
        /// </summary>
        ReadyToRecord,

        /// <summary>
        /// Recording mode.
        /// </summary>
        Recording,

        /// <summary>
        /// Exporting recording to local dataset.
        /// </summary>
        Exporting,

        /// <summary>
        ///  Transition mode after playing back is requested to start but ARCore hasn't rebooted.
        /// </summary>

        ReadyToPlayback,

        /// <summary>
        /// Playing back mode.
        /// </summary>
        Playingback,
    }

    /// <summary>
    /// Starts to record the RGBD sequence if the current status is Stopped.
    /// Stops the recording otherwise.
    /// </summary>
    public void OnRecordButtonTapped()
    {
        if (_status == RecorderStatus.Stopped)
        {
            _status = RecorderStatus.ReadyToRecord;
            IEnumerator recordThread = Record();
            RecordButton.gameObject.SetActive(false);
            PlaybackButton.gameObject.SetActive(false);
            StopRecordingButton.gameObject.SetActive(true);
            StartCoroutine(recordThread);
        }
        else if (_status == RecorderStatus.Recording)
        {
            _status = RecorderStatus.Exporting;
            RecordButton.gameObject.SetActive(true);
            PlaybackButton.gameObject.SetActive(true);
            StopRecordingButton.gameObject.SetActive(false);
            RecordingTimerText.gameObject.SetActive(false);
            _recordingManager.StopRecording();
            _status = RecorderStatus.Stopped;
            ResetScenes();
        }
    }

    /// <summary>
    /// Starts to playback the latest RGBD sequence.
    /// Stops the recording otherwise.
    /// </summary>
    public void OnPlaybackButtonTapped()
    {
        if (_status == RecorderStatus.Stopped)
        {
            _status = RecorderStatus.ReadyToPlayback;
            PlaybackButton.gameObject.SetActive(false);
            StopPlayingbackButton.gameObject.SetActive(true);
            RecordButton.gameObject.SetActive(false);
            CarouselUIToggle.CarouselVisible(false);
            Carousel3D.gameObject.SetActive(false);
            IEnumerator playbackThread = PlaybackDataset(_filenameToSave);
            StartCoroutine(playbackThread);
        }
        else if (_status == RecorderStatus.Playingback)
        {
            CarouselUIToggle.CarouselVisible(true);
            Carousel3D.gameObject.SetActive(true);
            RecordButton.gameObject.SetActive(true);
            PlaybackButton.gameObject.SetActive(true);
            StopPlayingbackButton.gameObject.SetActive(false);
            IEnumerator stopPlaybackThread = PlaybackDataset(null);
            StartCoroutine(stopPlaybackThread);
        }
    }

    /// <summary>
    /// Obtains the default filename of the dataset.
    /// </summary>
    /// <returns>A string containing the current timestamp.</returns>
    private string GetDefaultDatasetName()
    {
        return Application.persistentDataPath + "/" + _defaultDatasetName;
    }

    /// <summary>
    /// Thread to record the ARCore session.
    /// </summary>
    /// <returns>An event of WaitForSeconds or null.</returns>
    private IEnumerator Record()
    {
        Extensions.Session.enabled = false;

        // It can take up to 10s until AR Foundation unlocked session native pointer.
        // Before that, the session handle returns IntPtr.Zero and fails StartRecording().
        RecordingResult result = RecordingResult.SessionNotReady;
        _recordingConfig.Mp4DatasetFilepath = _filenameToSave;

        yield return new WaitWhile(() =>
        {
            result = _recordingManager.StartRecording(_recordingConfig);
            return result == RecordingResult.SessionNotReady;
        });

        if (result != RecordingResult.OK)
        {
            Extensions.Session.enabled = true;
            _status = RecorderStatus.Stopped;
            yield break;
        }

        ResetScenes();
        yield return null;
        Extensions.Session.enabled = true;
        _status = RecorderStatus.Recording;
        _timeWhenRecorderStarts = Time.time;
        RecordingTimerText.gameObject.SetActive(true);
    }

    /// <summary>
    /// Thread to playback an ARCore session.
    /// </summary>
    /// <param name="datasetPath">Location of the dataset.</param>
    /// <returns>An event of WaitForSeconds or null.</returns>
    private IEnumerator PlaybackDataset(string datasetPath)
    {
        // Deal with cases:
        // - Idle -> PreparePlayback => Playback.
        // - Playback -> PrepareIdle => Idle.
        Extensions.Session.enabled = false;

        // It can take up to 10s until AR Foundation unlocked session native pointer.
        // Before that, the session handle returns IntPtr.Zero and fails SetPlaybackDataset().
        PlaybackResult result = PlaybackResult.SessionNotReady;
        yield return new WaitWhile(() =>
        {
            result = _playbackManager.SetPlaybackDataset(datasetPath);
            return result == PlaybackResult.SessionNotReady;
        });

        if (result != PlaybackResult.OK)
        {
            yield break;
        }

        yield return null;
        Extensions.Session.enabled = true;
        if (datasetPath == null)
        {
            _status = RecorderStatus.Stopped;
        }
        else
        {
            _status = RecorderStatus.Playingback;
        }
    }

    /// <summary>
    /// Checks if there exists a pre-recorded dataset on start. If so, show the PlayBack button.
    /// </summary>
    private void Start()
    {
        _recordingConfig = ScriptableObject.CreateInstance<ARCoreRecordingConfig>();
        _filenameToSave = GetDefaultDatasetName();
        _recordingConfig.Mp4DatasetFilepath = _filenameToSave;

        if (System.IO.File.Exists(Application.persistentDataPath + "/" + _defaultDatasetName))
        {
            PlaybackButton.gameObject.SetActive(true);
        }

        _recordingManager = gameObject.AddComponent<ARRecordingManager>();
        _playbackManager = gameObject.AddComponent<ARPlaybackManager>();
    }

    /// <summary>
    /// Handles events when recording or playing back.
    /// </summary>
    private void Update()
    {
        if (_status == RecorderStatus.Playingback)
        {
            // Checks if the dataset comes to an end when playing back.
            if (_playbackManager.PlaybackStatus != PlaybackStatus.OK)
            {
                ResetScenes();
                OnPlaybackButtonTapped();
            }
        }
        else if (_status == RecorderStatus.Recording)
        {
            // Updates the timer's text when recording.
            float seconds = Time.time - _timeWhenRecorderStarts;
            float minutes = Mathf.Floor(seconds / 60.0f);
            seconds -= minutes * 60.0f;
            RecordingTimerText.text = string.Format("{0:00}:{1:00}", (int)minutes, (int)seconds);
        }
    }

    /// <summary>
    /// Clears objects in the scenes like point clouds and splatting.
    /// </summary>
    private void ResetScenes()
    {
        // Reserved for adding scenes.
    }
}
