//-----------------------------------------------------------------------
// <copyright file="DepthMeshCollider.cs" company="Google LLC">
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

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
    [FormerlySerializedAs("projectiles")] public GameObject[] Projectiles;

    /// <summary>
    /// Life time in seconds of the thrown game object.
    /// </summary>
    [FormerlySerializedAs("projectileLifetimeS")]
    public int ProjectileLifetimeS = _projectileLifetimeS;

    /// <summary>
    /// Whether to enable the renderer.
    /// </summary>
    [FormerlySerializedAs("render")] public bool Render = false;

    /// <summary>
    /// Flag to enable sparse depth.
    /// </summary>
    public bool UseRawDepth;

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
    private const int _numThreadsX = 8;
    private const int _numThreadsY = 8;
    private const int _kDepthPixelSkippingX = 2;
    private const int _kDepthPixelSkippingY = 2;
    private const int _normalSamplingOffset = 1;
    private const int _projectileLifetimeS = 180;
    private const float _edgeExtensionOffset = 0.5f;
    private const float _edgeExtensionDepthOffset = -0.5f;

    // Holds the vertex and index data of the depth template mesh.
    private Mesh _mesh;
    private bool _initialized = false;
    private MeshCollider _meshCollider;
    private System.Random _random = new System.Random();
    private int _vertexFromDepthHandle;
    private int _normalFromVertexHandle;
    private int _numElements;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _normalBuffer;
    private Vector3[] _vertices;
    private Vector3[] _normals;
    private int _getDataCountdown = -1;
    private int _depthPixelSkippingX = _kDepthPixelSkippingX;
    private int _depthPixelSkippingY = _kDepthPixelSkippingY;
    private int _meshWidth;
    private int _meshHeight;
    private GameObject _root = null;
    private List<GameObject> _gameObjects = new List<GameObject>();
    private bool _cachedUseRawDepth = false;
    private AROcclusionManager _occlusionManager;
    private Texture2D _depthTexture;

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
        if (_root == null)
        {
            _root = new GameObject("Projectiles");
        }

        GameObject bullet = Instantiate(
            Projectiles[_random.Next(Projectiles.Length)],
            DepthSource.ARCamera.transform.position +
            (DepthSource.ARCamera.transform.forward * ForwardOffset),
            Quaternion.identity);

        Vector3 forceVector = DepthSource.ARCamera.transform.forward * ProjectileThrust;
        bullet.GetComponent<Rigidbody>().velocity = forceVector;
        bullet.transform.parent = _root.transform;
        _gameObjects.Add(bullet);
    }

    /// <summary>
    /// Clears all the instantiated projectiles.
    /// </summary>
    public void Clear()
    {
        foreach (GameObject go in _gameObjects)
        {
            Destroy(go);
        }

        _gameObjects.Clear();

        if (_root != null)
        {
            Debug.Log("Destroy all projectiles " + _root.transform.childCount);
            foreach (Transform child in _root.transform)
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
        ARSession.stateChanged -= OnSessionStateChanged;

        Clear();
        if (_root != null)
        {
            Destroy(_root);
        }

        _root = null;
        _vertexBuffer.Dispose();
        _normalBuffer.Dispose();
    }

    private void Start()
    {
        _meshCollider = GetComponent<MeshCollider>();
        GetComponent<MeshRenderer>().enabled = Render;

        _occlusionManager = FindObjectOfType<AROcclusionManager>();
        Debug.Assert(_occlusionManager);

        ARSession.stateChanged += OnSessionStateChanged;
    }

    private void Update()
    {
        if (_initialized)
        {
            if (_getDataCountdown > 0)
            {
                _getDataCountdown--;
            }
            else if (_getDataCountdown == 0)
            {
                UpdateDepthTexture();
                UpdateCollider();
            }
        }
        else
        {
            if (DepthSource.Initialized)
            {
                if (_cachedUseRawDepth != UseRawDepth)
                {
                    DepthSource.SwitchToRawDepth(UseRawDepth);
                    _cachedUseRawDepth = UseRawDepth;
                }

                _meshWidth = DepthSource.DepthWidth / _depthPixelSkippingX;
                _meshHeight = DepthSource.DepthHeight / _depthPixelSkippingY;
                _numElements = _meshWidth * _meshHeight;

                UpdateDepthTexture();
                InitializeComputeShader();
                InitializeMesh();
                _initialized = true;
            }
        }
    }

    private void InitializeMesh()
    {
        // Creates template vertices.
        _vertices = new Vector3[_numElements];
        _normals = new Vector3[_numElements];

        // Creates template vertices for the mesh object.
        for (int y = 0; y < _meshHeight; y++)
        {
            for (int x = 0; x < _meshWidth; x++)
            {
                int index = (y * _meshWidth) + x;
                Vector3 v = new Vector3(x * 0.01f, -y * 0.01f, 0);
                _vertices[index] = v;
                _normals[index] = Vector3.back;
            }
        }

        // Creates template triangle list.
        int[] triangles = GenerateTriangles(_meshWidth, _meshHeight);

        // Creates the mesh object and set all template data.
        _mesh = new Mesh();
        _mesh.MarkDynamic();
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _mesh.vertices = _vertices;
        _mesh.normals = _normals;
        _mesh.triangles = triangles;
        _mesh.bounds = new Bounds(Vector3.zero, new Vector3(20, 20, 20));
        _mesh.UploadMeshData(false);

        if (Render)
        {
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }
    }

    private void InitializeComputeShader()
    {
        _vertexFromDepthHandle = DepthProcessingCS.FindKernel("VertexFromDepth");
        _normalFromVertexHandle = DepthProcessingCS.FindKernel("NormalFromVertex");

        _vertexBuffer = new ComputeBuffer(_numElements, sizeof(float) * 3);
        _normalBuffer = new ComputeBuffer(_numElements, sizeof(float) * 3);

        // Sets general compute shader variables.
        DepthProcessingCS.SetInt("DepthWidth", DepthSource.ImageDimensions.x);
        DepthProcessingCS.SetInt("DepthHeight", DepthSource.ImageDimensions.y);
        DepthProcessingCS.SetFloat("PrincipalX", DepthSource.PrincipalPoint.x);
        DepthProcessingCS.SetFloat("PrincipalY", DepthSource.PrincipalPoint.y);
        DepthProcessingCS.SetFloat("FocalLengthX", DepthSource.FocalLength.x);
        DepthProcessingCS.SetFloat("FocalLengthY", DepthSource.FocalLength.y);
        DepthProcessingCS.SetInt("NormalSamplingOffset", _normalSamplingOffset);
        DepthProcessingCS.SetInt("DepthPixelSkippingX", _depthPixelSkippingX);
        DepthProcessingCS.SetInt("DepthPixelSkippingY", _depthPixelSkippingY);
        DepthProcessingCS.SetInt("MeshWidth", _meshWidth);
        DepthProcessingCS.SetInt("MeshHeight", _meshHeight);
        DepthProcessingCS.SetBool("ExtendEdges", ExtendMeshEdges);
        DepthProcessingCS.SetFloat("EdgeExtensionOffset", _edgeExtensionOffset);
        DepthProcessingCS.SetFloat("EdgeExtensionDepthOffset", _edgeExtensionDepthOffset);

        // Sets shader resources for the vertex function.
        DepthProcessingCS.SetBuffer(_vertexFromDepthHandle, "vertexBuffer", _vertexBuffer);

        // Sets shader resources for the normal function.
        DepthProcessingCS.SetBuffer(_normalFromVertexHandle, "vertexBuffer", _vertexBuffer);
        DepthProcessingCS.SetBuffer(_normalFromVertexHandle, "normalBuffer", _normalBuffer);
    }

    private void UpdateMesh()
    {
        if (!_initialized)
        {
            return;
        }

        UpdateComputeShaderVariables();

        DepthProcessingCS.Dispatch(_vertexFromDepthHandle, _meshWidth / _numThreadsX,
            (_meshHeight / _numThreadsY) + 1, 1);

        _getDataCountdown = 2;

        if (Render)
        {
            _vertexBuffer.GetData(_vertices);
            _mesh.vertices = _vertices;
            _mesh.RecalculateNormals();
            _mesh.UploadMeshData(false);
        }
    }

    private void UpdateCollider()
    {
        _getDataCountdown = -1;

        _vertexBuffer.GetData(_vertices);
        _mesh.vertices = _vertices;
        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;

        OnColliderMeshReady();
    }

    private void OnColliderMeshReady()
    {
        ColliderMeshReadyEvent?.Invoke();
    }

    private void UpdateComputeShaderVariables()
    {
        DepthProcessingCS.SetTexture(_vertexFromDepthHandle, "depthTex", _depthTexture);
        DepthProcessingCS.SetMatrix("ModelTransform", DepthSource.LocalToWorldMatrix);
    }

    private void OnSessionStateChanged(ARSessionStateChangedEventArgs eventArgs)
    {
        if (eventArgs.state == ARSessionState.SessionInitializing)
        {
            // Clear all projectiles for a new session.
            Clear();
        }
    }

    private void UpdateDepthTexture()
    {
        if (_occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image))
        {
            UpdateRawImage(ref _depthTexture, image);
        }

        image.Dispose();
    }

    private void UpdateRawImage(ref Texture2D texture, XRCpuImage cpuImage)
    {
        if (texture == null || texture.width != cpuImage.width || texture.height != cpuImage.height)
        {
            texture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.RGB565, false);
        }

        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, TextureFormat.R16);
        var rawTextureData = texture.GetRawTextureData<byte>();
        cpuImage.Convert(conversionParams, rawTextureData);
        texture.Apply();
    }
}