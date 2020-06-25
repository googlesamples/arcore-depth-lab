//-----------------------------------------------------------------------
// <copyright file="SceneSwitcher.cs" company="Google LLC">
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
    /// </summary>
    public string StartScene;

    private Scene m_BaseScene;

    private bool m_SceneChangeComplete = true;

    private string m_CurrentScene;

    /// <summary>
    /// Calls the scene loading IEnumerator.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// This method is for moving objects to the base scene.
    /// Use this for any objects that should stay in the scene after switching scenes.
    /// </summary>
    /// <param name="gameObject">Gameobject to move to the base scene.</param>
    public void MoveObjectToBase(GameObject gameObject)
    {
        SceneManager.MoveGameObjectToScene(gameObject, m_BaseScene);
        gameObject.transform.SetParent(ActiveObjectsContainer);
    }

    /// <summary>
    /// This method is for instantiating objects that should eventually be destroyed
    /// once there is a scene change.
    /// </summary>
    /// <param name="original">Object to instantiate.</param>
    /// <param name="position">Optional position to instantiate the object.</param>
    /// <param name="rotation">Optional rotation to instantiate the object.</param>
    public void InstantiateForCurrentScene(Object original,
        Vector3 position = new Vector3(), Quaternion rotation = new Quaternion())
    {
        // Checks whether 'rotation' is initialized with default '0' values.
        // Changes 'rotation' to identity, if that's the case.
        // There is no way to provide a default parameter with any specific value.
        if (rotation.Equals(new Quaternion()))
        {
            rotation = Quaternion.identity;
        }

        Instantiate(original, position, rotation, ActiveObjectsToDestroy);
    }

    /// <summary>
    /// Calls the initial scene loading.
    /// </summary>
    /// <returns>Returns WaitForEndOfFrame.</returns>
    public IEnumerator InitSceneLoad()
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
        m_BaseScene = SceneManager.GetActiveScene();
    }

    /// <summary>
    /// Scene loading logic.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    /// <returns>Returns WaitForEndOfFrame.</returns>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (sceneName == string.Empty)
        {
            yield break;
        }

        LoadingSpinner.Instance.Show();
        AsyncOperation Async = new AsyncOperation();

        // Checks that the new scene isn't null,
        // we're not trying to reload the same scene or that a scene load is in progress.
        if (sceneName != m_CurrentScene && m_SceneChangeComplete && sceneName != null)
        {
            m_SceneChangeComplete = false;

            // Checks if the scene we're about to load isn't already loaded.
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                // Unloads the previous scene.
                // Doing it before loading a new scene to prevent AR background effect bug.
                Scene currentScene = SceneManager.GetSceneByName(m_CurrentScene);
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

                    Async = SceneManager.UnloadSceneAsync(currentScene);

                    // Waits until the old scene is unloaded.
                    while (!Async.isDone)
                    {
                        yield return null;
                    }
                }

                // Loads new scene.
                Async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                LoadingSpinner.Instance.SetLoadingOperation(Async);

                // Waits until new scene is loaded.
                while (!Async.isDone)
                {
                    yield return null;
                }

                // Assigns new scene as current scene.
                m_CurrentScene = sceneName;

                m_SceneChangeComplete = true;
            }
        }
        else
        {
            LoadingSpinner.Instance.Hide();
        }
    }
}
