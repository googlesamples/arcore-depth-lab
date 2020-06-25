//-----------------------------------------------------------------------
// <copyright file="DepthMeshCollider.cs" company="Google LLC">
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Manages the collision physics and events when the user throws a virtual game object (projectile)
/// in the physical environment.
/// </summary>
public class DepthMeshCollider : MonoBehaviour
{
    /// <summary>
    /// Depth processing script.
    /// </summary>
    [FormerlySerializedAs("depthProcessingCS")]
    public ComputeShader DepthProcessingCS;

    /// <summary>
    /// Whether to compute the normal vector.
    /// </summary>
    [FormerlySerializedAs("calculateNormals")]
    public bool CalculateNormals = false;

    /// <summary>
    /// An array of game object thrown by the user.
    /// </summary>
    [FormerlySerializedAs("projectiles")]
    public GameObject[] Projectiles;

    /// <summary>
    /// Camera of the scene.
    /// </summary>
    [FormerlySerializedAs("sceneCamera")]
    public GameObject SceneCamera;

    /// <summary>
    /// Life time in seconds of the thrown game object.
    /// </summary>
    [FormerlySerializedAs("projectileLifetimeS")]
    public int ProjectileLifetimeS = k_ProjectileLifetimeS;

    /// <summary>
    /// Whether to enable the renderer.
    /// </summary>
    [FormerlySerializedAs("render")]
    public bool Render = false;

    /// <summary>
    /// Makes sure physics objects don't fall through.
    /// </summary>
    public bool ExtendMeshEdges = true;

    /// <summary>
    /// Offset from where the projectile starts.
    /// </summary>
    public float ForwardOffset = 0.25f;

    /// <summary>
    /// Triggers when the ColliderMesh is ready.
    /// </summary>
    [FormerlySerializedAs("colliderMeshReadyEvent")]
    public UnityEvent ColliderMeshReadyEvent;

    /// <summary>
    /// How much thrust are we giving to the projectile.
    /// </summary>
    public float ProjectileThrust = 5;

    // Number of threads used by the compute shader.
    private const int k_NumThreadsX = 8;
    private const int k_NumThreadsY = 8;
    private const int k_DepthPixelSkippingX = 2;
    private const int k_DepthPixelSkippingY = 2;
    private const int k_NormalSamplingOffset = 1;
    private const int k_ProjectileLifetimeS = 180;
    private const float k_EdgeExtensionOffset = 0.5f;
    private const float k_EdgeExtensionDepthOffset = -0.5f;

    // Holds the vertex and index data of the depth template mesh.
    private Mesh m_Mesh;
    private bool m_Initialized = false;
    private MeshCollider m_MeshCollider;
    private System.Random m_Random = new System.Random();
    private int m_VertexFromDepthHandle;
    private int m_NormalFromVertexHandle;
    private int m_NumElements;
    private ComputeBuffer m_VertexBuffer;
    private ComputeBuffer m_NormalBuffer;
    private Vector3[] m_Vertices;
    private Vector3[] m_Normals;
    private int m_GetDataCountdown = -1;
    private int m_DepthPixelSkippingX = k_DepthPixelSkippingX;
    private int m_DepthPixelSkippingY = k_DepthPixelSkippingY;
    private int m_MeshWidth;
    private int m_MeshHeight;
    private GameObject m_Root = null;
    private List<GameObject> m_GameObjects = new List<GameObject>();

    /// <summary>
    /// Throws a game object for the collision test.
    /// </summary>
    public void ShootProjectile()
    {
        UpdateMesh();
    }

    /// <summary>
    /// Instantiates a game object from prefab and throws it.
    /// </summary>
    public void ShootPrefab()
    {
        if (m_Root == null)
        {
            m_Root = new GameObject("Projectiles");
        }

        GameObject bullet = Instantiate(
            Projectiles[m_Random.Next(Projectiles.Length)],
            SceneCamera.transform.position + (SceneCamera.transform.forward * ForwardOffset),
            Quaternion.identity) as GameObject;

        Vector3 forceVector = SceneCamera.transform.forward * ProjectileThrust;
        bullet.GetComponent<Rigidbody>().velocity = forceVector;
        bullet.transform.parent = m_Root.transform;
        m_GameObjects.Add(bullet);
    }

    /// <summary>
    /// Clears all the instantiated projectiles.
    /// </summary>
    public void Clear()
    {
        foreach (GameObject go in m_GameObjects)
        {
            Destroy(go);
        }

        m_GameObjects.Clear();

        if (m_Root != null)
        {
            Debug.Log("Destroy all projectiles " + m_Root.transform.childCount);
            foreach (Transform child in m_Root.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static int[] GenerateTriangles(int width, int height)
    {
        int[] indices = new int[(height - 1) * (width - 1) * 6];
        int idx = 0;
        for (int y = 0; y < (height - 1); y++)
        {
            for (int x = 0; x < (width - 1); x++)
            {
                //// Unity has a clockwise triangle winding order.
                //// Upper quad triangle
                //// Top left
                int idx0 = (y * width) + x;
                //// Top right
                int idx1 = idx0 + 1;
                //// Bottom left
                int idx2 = idx0 + width;

                //// Lower quad triangle
                //// Top right
                int idx3 = idx1;
                //// Bottom right
                int idx4 = idx2 + 1;
                //// Bottom left
                int idx5 = idx2;

                indices[idx++] = idx0;
                indices[idx++] = idx1;
                indices[idx++] = idx2;
                indices[idx++] = idx3;
                indices[idx++] = idx4;
                indices[idx++] = idx5;
            }
        }

        return indices;
    }

    private void OnDestroy()
    {
        Clear();
        if (m_Root != null)
        {
            Destroy(m_Root);
        }

        m_Root = null;
        m_VertexBuffer.Dispose();
        m_NormalBuffer.Dispose();
    }

    private void Start()
    {
        m_MeshCollider = GetComponent<MeshCollider>();
        GetComponent<MeshRenderer>().enabled = Render;

        if (SceneCamera == null)
        {
            SceneCamera = Camera.main.gameObject;
        }
    }

    private void Update()
    {
        if (m_Initialized)
        {
            if (m_GetDataCountdown > 0)
            {
                m_GetDataCountdown--;
            }
            else if (m_GetDataCountdown == 0)
            {
                UpdateCollider();
            }
        }
        else
        {
            if (DepthSource.Initialized)
            {
                m_MeshWidth = DepthSource.DepthWidth / m_DepthPixelSkippingX;
                m_MeshHeight = DepthSource.DepthHeight / m_DepthPixelSkippingY;
                m_NumElements = m_MeshWidth * m_MeshHeight;

                InitializeComputeShader();
                InitializeMesh();
                m_Initialized = true;
            }
        }
    }

    private void InitializeMesh()
    {
        // Creates template vertices.
        m_Vertices = new Vector3[m_NumElements];
        m_Normals = new Vector3[m_NumElements];

        // Creates template vertices for the mesh object.
        for (int y = 0; y < m_MeshHeight; y++)
        {
            for (int x = 0; x < m_MeshWidth; x++)
            {
                int index = (y * m_MeshWidth) + x;
                Vector3 v = new Vector3(x * 0.01f, -y * 0.01f, 0);
                m_Vertices[index] = v;
                m_Normals[index] = Vector3.back;
            }
        }

        // Creates template triangle list.
        int[] triangles = GenerateTriangles(m_MeshWidth, m_MeshHeight);

        // Creates the mesh object and set all template data.
        m_Mesh = new Mesh();
        m_Mesh.MarkDynamic();
        m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_Mesh.vertices = m_Vertices;
        m_Mesh.normals = m_Normals;
        m_Mesh.triangles = triangles;
        m_Mesh.bounds = new Bounds(Vector3.zero, new Vector3(20, 20, 20));
        m_Mesh.UploadMeshData(false);

        if (Render)
        {
            GetComponent<MeshFilter>().sharedMesh = m_Mesh;
        }
    }

    private void InitializeComputeShader()
    {
        m_VertexFromDepthHandle = DepthProcessingCS.FindKernel("VertexFromDepth");
        m_NormalFromVertexHandle = DepthProcessingCS.FindKernel("NormalFromVertex");

        m_VertexBuffer = new ComputeBuffer(m_NumElements, sizeof(float) * 3);
        m_NormalBuffer = new ComputeBuffer(m_NumElements, sizeof(float) * 3);

        Texture2D depthTexture =
            DepthSource.DepthTexture;

        // Sets general compute shader variables.
        DepthProcessingCS.SetInt("DepthWidth", DepthSource.ImageDimensions.x);
        DepthProcessingCS.SetInt("DepthHeight", DepthSource.ImageDimensions.y);
        DepthProcessingCS.SetFloat("PrincipalX", DepthSource.PrincipalPoint.x);
        DepthProcessingCS.SetFloat("PrincipalY", DepthSource.PrincipalPoint.y);
        DepthProcessingCS.SetFloat("FocalLengthX", DepthSource.FocalLength.x);
        DepthProcessingCS.SetFloat("FocalLengthY", DepthSource.FocalLength.y);
        DepthProcessingCS.SetInt("NormalSamplingOffset", k_NormalSamplingOffset);
        DepthProcessingCS.SetInt("DepthPixelSkippingX", m_DepthPixelSkippingX);
        DepthProcessingCS.SetInt("DepthPixelSkippingY", m_DepthPixelSkippingY);
        DepthProcessingCS.SetInt("MeshWidth", m_MeshWidth);
        DepthProcessingCS.SetInt("MeshHeight", m_MeshHeight);
        DepthProcessingCS.SetBool("ExtendEdges", ExtendMeshEdges);
        DepthProcessingCS.SetFloat("EdgeExtensionOffset", k_EdgeExtensionOffset);
        DepthProcessingCS.SetFloat("EdgeExtensionDepthOffset", k_EdgeExtensionDepthOffset);

        // Sets shader resources for the vertex function.
        DepthProcessingCS.SetTexture(m_VertexFromDepthHandle, "depthTex", depthTexture);
        DepthProcessingCS.SetBuffer(m_VertexFromDepthHandle, "vertexBuffer", m_VertexBuffer);

        // Sets shader resources for the normal function.
        DepthProcessingCS.SetBuffer(m_NormalFromVertexHandle, "vertexBuffer", m_VertexBuffer);
        DepthProcessingCS.SetBuffer(m_NormalFromVertexHandle, "normalBuffer", m_NormalBuffer);
    }

    private void UpdateMesh()
    {
        if (!m_Initialized)
        {
            return;
        }

        UpdateComputeShaderVariables();

        DepthProcessingCS.Dispatch(m_VertexFromDepthHandle, m_MeshWidth / k_NumThreadsX,
            (m_MeshHeight / k_NumThreadsY) + 1, 1);

        m_GetDataCountdown = 2;

        if (Render)
        {
            m_VertexBuffer.GetData(m_Vertices);
            m_Mesh.vertices = m_Vertices;
            m_Mesh.RecalculateNormals();
            m_Mesh.UploadMeshData(false);
        }
    }

    private void UpdateCollider()
    {
        m_GetDataCountdown = -1;

        m_VertexBuffer.GetData(m_Vertices);
        m_Mesh.vertices = m_Vertices;
        m_MeshCollider.sharedMesh = null;
        m_MeshCollider.sharedMesh = m_Mesh;

        OnColliderMeshReady();
    }

    private void OnColliderMeshReady()
    {
        ColliderMeshReadyEvent?.Invoke();
    }

    private void UpdateComputeShaderVariables()
    {
        Texture2D depthTexture = DepthSource.DepthTexture;

        DepthProcessingCS.SetTexture(m_VertexFromDepthHandle, "depthTex", depthTexture);
        DepthProcessingCS.SetMatrix("ModelTransform", DepthSource.LocalToWorldMatrix);
    }
}
