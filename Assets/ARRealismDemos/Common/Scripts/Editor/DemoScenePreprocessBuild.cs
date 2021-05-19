//-----------------------------------------------------------------------
// <copyright file="DemoScenePreprocessBuild.cs" company="Google LLC">
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


using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Preprocess build to manage mutliple scenes.
/// </summary>
public class DemoScenePreprocessBuild : IPreprocessBuildWithReport
{
    /// <summary>
    /// Gets the relative callback order for callbacks. Callbacks with lower values are called
    /// before ones with higher values.
    /// </summary>
    public int callbackOrder => 0;

    /// <summary>
    /// A callback received before the build is started.
    /// </summary>
    /// <param name="report">A report containing information about the build,
    /// such as its target platform and output path.</param>
    public void OnPreprocessBuild(BuildReport report)
    {
        bool foundBaseScene = false;
        SceneSwitcher switcher = null;
        Dictionary<string, List<GameObject>> gameObjectsForDisable =
            new Dictionary<string, List<GameObject>>();

        foreach (EditorBuildSettingsScene editorScene in EditorBuildSettings.scenes)
        {
            if (editorScene.enabled)
            {
                Scene scene = SceneManager.GetSceneByPath(editorScene.path);
                if (!scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(
                        editorScene.path, OpenSceneMode.Additive);
                }

                gameObjectsForDisable[scene.name] = new List<GameObject>();
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    if (foundBaseScene && go.GetComponentInChildren<SceneSwitcher>())
                    {
                        throw new BuildFailedException(
                            "Found duplicate SceneSwitcher in " + editorScene.path);
                    }
                    else if (!foundBaseScene)
                    {
                        switcher ??= go.GetComponentInChildren<SceneSwitcher>();
                        foundBaseScene = switcher != null;
                    }

                    var shouldDestroy = go.GetComponent<DestroyInDemoScene>() ??
                        go.GetComponentInChildren<DestroyInDemoScene>();
                    if (shouldDestroy != null && shouldDestroy.ShouldDstroyInDemoScene)
                    {
                        gameObjectsForDisable[scene.name].Add(go);
                    }
                }
            }
        }

        if (switcher != null)
        {
            // Check whether SceneSwitcher.StartScene is valid.
            if (!gameObjectsForDisable.ContainsKey(switcher.StartScene))
            {
                Debug.LogWarningFormat(
                    "StartScene is not enabled during the build, reset to null.",
                    switcher.StartScene);
                switcher.StartScene = null;
            }

            // Setup scene buttons based on currently active scenes:
            foreach (SceneButton sceneButton in
                switcher.gameObject.GetComponentsInChildren<SceneButton>(true))
            {
                sceneButton.gameObject.transform.parent.gameObject.SetActive(
                    gameObjectsForDisable.ContainsKey(sceneButton.SceneName));
            }
        }

        foreach (var keyValuePair in gameObjectsForDisable)
        {
            foreach (var gameObject in keyValuePair.Value)
            {
                Debug.LogFormat("{0} {1} in {2}.", foundBaseScene ? "Disable" : "Enable",
                    gameObject.name, keyValuePair.Key);
                gameObject.SetActive(!foundBaseScene);
            }
        }
    }
}
