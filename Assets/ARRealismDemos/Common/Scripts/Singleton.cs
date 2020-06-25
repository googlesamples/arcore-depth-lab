//-----------------------------------------------------------------------
// <copyright file="Singleton.cs" company="Google LLC">
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
/// Simple singleton class.
/// </summary>
/// <typeparam name="T">A MonoBehaviour type.</param>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T s_Instance;
    private static object s_Lock = new object();
    private static bool s_QuittingApplication = false;

    /// <summary>
    /// Gets the instance of this class.
    /// </summary>
    public static T Instance
    {
        get
        {
            return TryGetInstance();
        }
    }

    private static T TryGetInstance()
    {
        if (s_QuittingApplication)
        {
            return null;
        }

        lock (s_Lock)
        {
            if (s_Instance == null)
            {
                s_Instance = (T)FindObjectOfType(typeof(T));

                if (FindObjectsOfType(typeof(T)).Length > 1)
                {
                    return s_Instance;
                }

                if (s_Instance == null)
                {
                    GameObject singleton = new GameObject("Singleton");
                    s_Instance = singleton.AddComponent<T>();
                    singleton.name = "(singleton) " + typeof(T).ToString();
                    DontDestroyOnLoad(singleton);
                }
            }

            return s_Instance;
        }
    }

    private void OnDestroy()
    {
        s_QuittingApplication = true;
    }
}
