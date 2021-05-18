# ARCore Depth Lab - Depth API Samples for Unity

Copyright 2020 Google LLC

**Depth Lab** is a set of ARCore Depth API samples that provides assets using
depth for advanced geometry-aware features in AR interaction and rendering. Some
of these features have been used in this
[Depth API overview](https://www.youtube.com/watch?v=VOVhCTb-1io) video.

[![DepthLab examples](depthlab.gif)](https://augmentedperception.github.io/depthlab)

[**ARCore Depth API**](https://developers.google.com/ar/develop/unity/depth/overview)
is enabled on a subset of ARCore-certified Android devices. **iOS devices
(iPhone, iPad) are not supported**. Find the list of devices with Depth API
support (marked with **Supports Depth API**) here:
[https://developers.google.com/ar/devices](https://developers.google.com/ar/discover/supported-devices).
See the [ARCore developer documentation](https://developers.google.com/ar) for
more information.

Download the pre-built ARCore Depth Lab app on
[Google Play Store](https://play.google.com/store/apps/details?id=com.google.ar.unity.arcore_depth_lab)
today.

[<img alt="Get ARCore Depth Lab on Google Play" height="50px" src="https://play.google.com/intl/en_us/badges/images/apps/en-play-badge-border.png" />](https://play.google.com/store/apps/details?id=com.google.ar.unity.arcore_depth_lab)

## Sample features

The sample scenes demonstrate three different ways to access depth:

1.  **Localized depth**: Sample single depth values at certain texture
    coordinates (CPU).
    *   Character locomotion on uneven terrain
    *   Collision checking for AR object placement
    *   Laser beam reflections
    *   Oriented 3D reticles
    *   Rain and snow particle collision
2.  **Surface depth**: Create a connected mesh representation of the depth data
    (CPU/GPU).
    *   AR shadow receiver
    *   Paint splat
    *   Physics simulation
    *   Surface retexturing
    *   Point cloud fusion
3.  **Dense depth**: Process depth data at every screen pixel (GPU).
    *   AR fog
    *   Occlusions
    *   Depth-of-field blur
    *   Environment relighting
    *   False-color depth map
    *   3D photo

## Unity project setup

These samples target [**Unity 2021.1.3f**](https://unity3d.com) and require
[**ARCore SDK for Unity**](https://github.com/google-ar/arcore-unity-sdk)
**v1.24.0** or newer. Download and import
[`arcore-unity-sdk-1.24.0.unitypackage`](https://github.com/google-ar/arcore-unity-sdk/releases)
or newer into the sample project. For the first time, please use Unity 2019.4 to
open the project and install the following packages in `Window` -> `Packages
Manager`: Multiplayer HLAPI 1.0.8, TextMeshPro 3.0.4, Unity Collaborate 1.3.9,
and XR Legacy Input Helpers 2.1.7. Note that newer versions are not tested and
may not work. Close and reopen the project with Unity 2021 and reimport all demo
shaders to resolve any dependency issues in the Unity editor. This project only
builds with the Build Platform **Android**. **Instant Preview** is not enabled
for Depth API yet. Build the project to an Android device instead of using the
**Play** button in the Unity editor.

## Building samples

Individual scenes can be built and run by just enabling a particular scene, e.g.
`FogEffect` to try out the depth-aware fog filter.

We also provide a demo user interface that allows users to seamlessly switch
between examples. Please make sure to set the **Build Platform** to **Android**
and verify that the main `DemoCarousel` scene is the first enabled scene in the
**Scenes In Build** list under **Build Settings**. Enable all scenes that are
part of the demo user interface.

`Assets/ARRealismDemos/DemoCarousel/Scenes/DemoCarousel.unity
Assets/ARRealismDemos/OrientedReticle/Scenes/OrientedReticle.unity
Assets/ARRealismDemos/DepthEffects/Scenes/DepthEffects.unity
Assets/ARRealismDemos/MaterialWrap/Scenes/MaterialWrap.unity
Assets/ARRealismDemos/Splat/Scenes/OrientedSplat.unity
Assets/ARRealismDemos/Collider/Scenes/Collider.unity
Assets/ARRealismDemos/LaserBeam/Scenes/LaserBeam.unity
Assets/ARRealismDemos/AvatarLocomotion/Scenes/AvatarLocomotion.unity
Assets/ARRealismDemos/Relighting/Scenes/PointsRelighting.unity
Assets/ARRealismDemos/DepthEffects/Scenes/FogEffect.unity
Assets/ARRealismDemos/SnowParticles/Scenes/ArCoreSnowParticles.unity
Assets/ARRealismDemos/RainParticles/Scenes/RainParticlesScene.unity
Assets/ARRealismDemos/DepthEffects/Scenes/DepthOfFieldEffect.unity
Assets/ARRealismDemos/Water/Scenes/Water.unity
Assets/ARRealismDemos/CollisionDetection/Scenes/CollisionAwareObjectPlacement.unity
Assets/ARRealismDemos/PointCloud/Scenes/RawPointClouds.unity
Assets/ARRealismDemos/ScreenSpaceDepthMesh/Scenes/ScreenSpaceDepthMesh.unity
Assets/ARRealismDemos/ScreenSpaceDepthMesh/Scenes/StereoPhoto.unity`

## Upcoming breaking change affecting 32-bit-only apps

The project is set up to use the `IL2CPP` scripting backend instead of `Mono` to
build an `ARM64` app. You may be prompted to locate the Android NDK folder. You
can download the `NDK` by navigating to `Unity > Preferences > External Tools >
NDK` and clicking the `Download` button.

In **August 2020**, **Google Play Services for AR** (ARCore) will remove support
for 32-bit-only ARCore-enabled apps running on 64-bit devices. Support for
32-bit apps running on 32-bit devices is unaffected.

If you have published a 32-bit-only (`armeabi-v7a`) version of your
ARCore-enabled app without publishing a corresponding 64-bit (`arm64-v8a`)
version, you must update your app to include 64-bit native libraries before
August 2020. 32-bit-only ARCore-enabled apps that are not updated by this time
may crash when attempting to start an augmented reality (AR) session.

To learn more about this breaking change, and for instructions on how to update
your app, see https://developers.google.com/ar/64bit.

## Sample project structure

The main sample assets are placed inside the `Assets/ARRealismDemos` folder.
Each subfolder contains sample features or helper components.

### `AvatarLocomotion`

The AR character in this scene follows user-set waypoints while staying close to
the surface of an uneven terrain. This scene uses raycasting and depth lookups
on the CPU to calculate a 3D point on the surface of the terrain.

### `Collider`

This physics simulation playground uses screen-space depth meshes to enable
collisions between Unity's rigid-body objects and the physical environment.

After pressing an on-screen button, a `Mesh` object is procedurally generated
from the latest depth map. This is used to update the `sharedMesh` parameter of
the `MeshCollider` object. A randomly selected primitive rigid-body object is
then thrown into the environment.

### `CollisionDetection`

This AR object placement scene uses depth lookups on the CPU to test collisions
between the vertices of virtual objects and the physical environment.

### `Common`

This folder contains scripts and prefabs that are shared between the feature
samples. For more details, see the [`Helper Classes`](#helper-classes) section
below.

### `DemoCarousel`

This folder contains the main scene, which provides a carousel user interface.
This scene allows the user to seamlessly switch between different features. A
scene can be selected by directly touching a preview thumbnail or dragging the
carousel UI to the desired position.

### `DepthEffects`

This folder contains three dense depth shader processing examples.

The `DepthEffects` scene contains a fragment-shader effect that can transition
from the AR camera view to a false-color depth map. Warm colors indicate closer
regions in the depth map. Cold colors indicate further regions.

The `DepthOfFieldEffect` scene contains a simulated **Bokeh** fragment-shader
effect. This blurs the regions of the AR view that are not at the user-defined
focus distance. The focus anchor is set in the physical environment by touching
the screen. The focus anchor is a 3D point that is locked to the environment and
always in focus.

The `FogEffect` scene contains a fragment-shader effect that adds a virtual fog
layer on the physical environment. Close objects will be more visible than
objects further away. A slider controls the density of the fog.

### `LaserBeam`

This laser reflection scene allows the user to shoot a slowly moving laser beam
by touching anywhere on the screen.

This uses:

*   The `DepthSource.GetVertexInWorldSpaceFromScreenXY(..)` function to look up
    a raycasted 3D point
*   The `ComputeNormalMapFromDepthWeightedMeanGradient(..)` function to look up
    the surface normal based on a provided 2D screen position.

### `MaterialWrap`

This experience allows the user to change the material of real-world surfaces
through touch. This uses depth meshes.

### `OrientedReticle`

This sample uses depth hit testing to obtain the raycasted 3D position and
surface normal of a raycasted screen point.

### `PointCloud`

This sample computes a point cloud on the CPU using the depth array. Press the
**Update** button to compute a point cloud based on the latest depth data.

### `RawPointClouds`

This sample fuses point clouds with the raw depth maps on the CPU using the
depth array. Drag the **confidence** slider to change the visibility of each
point based on the confidence value of the corresponding raw depth.

### `RainParticles`

This sample uses the GPU depth texture to compute collisions between rain
particles and the physical environment.

### `Relighting`

This sample uses the GPU depth texture to computationally re-light the physical
environment through the AR camera. Areas of the physical environment close to
the artificial light sources are lit, while areas farther away are darkened.

### `ScreenSpaceDepthMesh`

This sample uses depth meshes. A template mesh containing a regular grid of
triangles is created once on the CPU. The GPU shader displaces each vertex of
the regular grid based on the reprojection of the depth values provided by the
GPU depth texture. Press **Freeze** to take a snapshot of the mesh and press
**Unfreeze** to revert back to the live updating mesh.

### `StereoPhoto`

This sample uses depth meshes and
[`ScreenSpaceDepthMesh`](#ScreenSpaceDepthMesh). After freezing the mesh, we
cache the current camera's projection and view matrices, circulate the camera
around a circle, and perform projection mapping onto the depth mesh with the
cached camera image. Press **Capture** to create the animated 3D photo and press
**Preview** to go back to camera preview mode.

### `SnowParticles`

This sample uses the GPU depth texture to compute collisions between snow
particles, the physical environment, and the orientation of each snowflake.

### `Splat`

This sample uses the [`Oriented Reticle`](#orientedreticle) and the depth mesh
in placing a surface-aligned texture decal within the physical environment.

### `Water`

This sample uses a modified GPU occlusion shader to create a flooding effect
with artificial water in the physical environment.

## Developing your own ARCore Depth-enabled Unity experiences

Please make sure that the Unity scene is properly set up to run ARCore. Provide
depth data by attaching the `ARCoreSession` to the appropriate configuration.
Please see the example provided in the
[ARCore SDK for Unity](https://developers.google.com/ar/develop/unity) package
to correctly set up an ARCore Depth-enabled Unity scene.

Please follow the steps below to utilize the depth utilities provided in this
**ARCore Depth Lab** sample package:

1.  Attach at least one `DepthTarget` component to the scene. This makes sure
    that the `DepthSource` class provides depth data to the scene.

2.  A `DepthSource` component can be explicitly placed within the scene.
    Otherwise an instance will be created automatically. A few parameters can be
    customized in the editor when `DepthSource` is explicitly placed in the
    scene.

3.  The depth texture is directly set to the material of a `MeshRenderer` when
    the `DepthTarget` script is attached to a `GameObject` with a `Meshrenderer`
    component.

## Helper classes

### `DepthSource`

A singleton instance of this class contains references to the CPU array and GPU
texture of the depth map, camera intrinsics, and many other depth look up and
coordinate transformation utilities. This class acts as a high-level wrapper for
the [`MotionStereoDepthDataSource`](#motionstereodepthdatasource) class.

### `DepthTarget`

Each `GameObject` containing a `DepthTarget` becomes a subscriber to the GPU
depth data. `DepthSource` will automatically update the depth data for each
`DepthTarget`. At least one instance of `DepthTarget` has to be present in the
scene in order for `DepthSource` to provide depth data.

### `MotionStereoDepthDataSource`

This class contains low-level operations and direct access to the depth data. It
should only be use by advanced developers.

## User privacy requirements

You must prominently disclose the use of Google Play Services for AR (ARCore)
and how it collects and processes data in your application. This information
must be easily accessible to end users. You can do this by adding the following
text on your main menu or notice screen: "This application runs on
[Google Play Services for AR](//play.google.com/store/apps/details?id=com.google.ar.core)
(ARCore), which is provided by Google LLC and governed by the
[Google Privacy Policy](//policies.google.com/privacy)".

## Related publication

Please refer to https://augmentedperception.github.io/depthlab/ for our paper,
supplementary material, and presentation published in ACM UIST 2020: "DepthLab:
Real-Time 3D Interaction With Depth Maps for Mobile Augmented Reality".

## References

If you use ARCore Depth Lab in your research, please reference it as:

```
@inproceedings{Du2020DepthLab,
  title = {{DepthLab: Real-time 3D Interaction with Depth Maps for Mobile Augmented Reality}},
  author = {Du, Ruofei and Turner, Eric and Dzitsiuk, Maksym and Prasso, Luca and Duarte, Ivo and Dourgarian, Jason and Afonso, Joao and Pascoal, Jose and Gladstone, Josh and Cruces, Nuno and Izadi, Shahram and Kowdle, Adarsh and Tsotsos, Konstantine and Kim, David},
  booktitle = {Proceedings of the 33rd Annual ACM Symposium on User Interface Software and Technology},
  year = {2020},
  publisher = {ACM},
  pages = {829--843},
  series = {UIST '20}
  doi = {10.1145/3379337.3415881}
}
```

or

```
Ruofei Du, Eric Turner, Maksym Dzitsiuk, Luca Prasso, Ivo Duarte, Jason Dourgarian, Joao Afonso, Jose Pascoal, Josh Gladstone, Nuno Cruces, Shahram Izadi, Adarsh Kowdle, Konstantine Tsotsos, and David Kim. 2020. DepthLab: Real-Time 3D Interaction With Depth Maps for Mobile Augmented Reality. Proceedings of the 33rd Annual ACM Symposium on User Interface Software and Technology (UIST '20), 829-843. DOI: http://dx.doi.org/10.1145/3379337.3415881.
```

## Additional information

You may use this software under the
[Apache 2.0 License](https://github.com/googlesamples/arcore-depth-lab/blob/master/LICENSE).
