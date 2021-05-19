//-----------------------------------------------------------------------
// <copyright file="SceneSwitcher.cs" company="Google LLC">
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Class for handling scene switching functions.
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    /// <summary>
    /// Instance of the SceneSwitcher class.
    /// </summary>
    public static SceneSwitcher Instance;

    /// <summary>
    /// Parent GameObject for storing objects moved from other scenes.
    /// </summary>
    public Transform ActiveObjectsContainer;

    /// <summary>
    /// Parent GameObject for storing objects to destroy after switching scenes.
    /// </summary>
    public Transform ActiveObjectsToDestroy;

    /// <summary>
    /// The first scene to load.
    /// Note: if the scene is not enabled in BuildSettings, this value will be reset to null
    /// during the build.
    /// </summary>
    public string StartScene;

    private Scene _baseScene;

    private bool _sceneChangeComplete = true;

    private string _currentScene;

    /// <summary>
    /// Calls the scene loading IEnumerator.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// Calls the initial scene loading.
    /// </summary>
    /// <returns>Returns WaitForEndOfFrame.</returns>
    private IEnumerator InitSceneLoad()
    {
        yield return new WaitForEndOfFrame();

        // Loads the start scene.
        LoadScene(StartScene);
    }

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update.
    private void Start()
    {
        StartCoroutine(InitSceneLoad());
        _baseScene = SceneManager.GetActiveScene();
    }

    /// <summary>
    /// Scene loading logic.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    /// <returns>Returns WaitForEndOfFrame.</returns>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            yield break;
        }

        LoadingSpinner.Instance.Show();
        AsyncOperation _async = new AsyncOperation();

        Debug.Log("LoadSceneAsync: " + sceneName);

        // Checks that the new scene isn't null,
        // we're not trying to reload the same scene or that a scene load is in progress.
        if (sceneName != _currentScene && _sceneChangeComplete)
        {
            _sceneChangeComplete = false;

            // Checks if the scene we're about to load isn't already loaded.
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                // Unloads the previous scene.
                // Doing it before loading a new scene to prevent AR background effect bug.
                Scene currentScene = SceneManager.GetSceneByName(_currentScene);
                if (currentScene != null && currentScene.isLoaded)
                {
                    if (ActiveObjectsToDestroy != null)
                    {
                        // Destroys all objects in ActiveObjectsToDestroy.
                        foreach (Transform child in ActiveObjectsToDestroy)
                        {
                            Destroy(child.gameObject);
                        }
                    }

                    _async = SceneManager.UnloadSceneAsync(currentScene);

                    // Waits until the old scene is unloaded.
                    while (!_async.isDone)
                    {
                        yield return null;
                    }
                }

                // Loads new scene.
                _async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                LoadingSpinner.Instance.SetLoadingOperation(_async);

                // Waits until new scene is loaded.
                while (!_async.isDone)
                {
                    yield return null;
                }

                // Assigns new scene as current scene.
                _currentScene = sceneName;

                _sceneChangeComplete = true;
                Debug.Log("Loaded the scene.");
            }
        }
        else
        {
            LoadingSpinner.Instance.Hide();
        }
    }
}
