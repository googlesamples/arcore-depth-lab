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

## Branches

ARCore Depth Lab has two branches: `master` and `arcore_unity_sdk`.

The `master` branch contains a subset of Depth Lab features in v1.1.0 and is
built upon the recommended
[AR Foundation 4.2.0 (preview 7)](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.2/manual/index.html)
or newer. The `master` branch supports features including oriented 3D reticles,
depth map visualization, collider with depth mesh, avatar locomotion, raw point
cloud visualization, recording and playback.

The `arcore_unity_sdk` branch contains the full features of Depth Lab and is
built upon
[ARCore SDK for Unity v1.24.0](https://github.com/google-ar/arcore-unity-sdk/releases)
or newer. We recommend using the `master` branch to build new projects with the
AR Foundation SDK and refer to this branch when necessary.

## Sample features

The sample scenes demonstrate three different ways to access depth. Supported
features in the `master` branch is labeled with :star:, while the rest features
can be found in the `arcore_unity_sdk` branch.

1.  **Localized depth**: Sample single depth values at certain texture
    coordinates (CPU).
    *   Oriented 3D reticles :star:
    *   Character locomotion on uneven terrain :star:
    *   Collision checking for AR object placement
    *   Laser beam reflections
    *   Rain and snow particle collision
2.  **Surface depth**: Create a connected mesh representation of the depth data
    (CPU/GPU).
    *   Point cloud fusion :star:
    *   AR shadow receiver
    *   Paint splat
    *   Physics simulation
    *   Surface retexturing
3.  **Dense depth**: Process depth data at every screen pixel (GPU).
    *   False-color depth map :star:
    *   AR fog
    *   Occlusions
    *   Depth-of-field blur
    *   Environment relighting
    *   3D photo

## Unity project setup

These samples target
[**Unity 2020.3.6f1**](https://unity3d.com/get-unity/download/archive) and
require
[**AR Foundation 4.2.0-pre.7**](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.2/manual/index.html)
 or newer,
[ARCore Extensions](https://developers.google.com/ar/develop/unity-arf) **1.24**
or newer. Download `arcore-unity-extensions-1.24.0.tgz` from the ARCore
Extensions for AR Foundation
[releases page on GitHub](https://github.com/google-ar/arcore-unity-extensions/releases).
Use Unity 2020.3 to open the project and import the package in `Window` ->
`Package Manager`, click the plus button, and choose the `Add package from
tarball...` option from the drop-down menu. Then locate
`arcore-unity-extensions-1.24.0.tgz` and click Open.

This project only builds with the Build Platform
**Android**. Build the project to an Android device instead of using the
**Play** button in the Unity editor.

## Building samples

Individual scenes can be built and run by enabling a particular scene (e.g.,
`OrientedReticle` to try out the oriented 3D reticle.) and the
`ARFDepthComponents` object in the scene. Remember to disable the
`ARFDepthComponents` object in individual scenes when building all demos with
the `DemoCarousel` scene.

We also provide a demo user interface that allows users to seamlessly switch
between examples. Please make sure to set the **Build Platform** to **Android**
and verify that the main `DemoCarousel` scene is the first enabled scene in the
**Scenes In Build** list under **Build Settings**. Enable all scenes that are
part of the demo user interface.

`Assets/ARRealismDemos/DemoCarousel/Scenes/DemoCarousel.unity
Assets/ARRealismDemos/OrientedReticle/Scenes/OrientedReticle.unity
Assets/ARRealismDemos/DepthEffects/Scenes/DepthEffects.unity
Assets/ARRealismDemos/Collider/Scenes/Collider.unity
Assets/ARRealismDemos/AvatarLocomotion/Scenes/AvatarLocomotion.unity
Assets/ARRealismDemos/PointCloud/Scenes/RawPointClouds.unity`

The following scenes can be found in the `arcore_unity_sdk` branch, but are not
yet available with the AR Foundation SDK.

`Assets/ARRealismDemos/MaterialWrap/Scenes/MaterialWrap.unity
Assets/ARRealismDemos/Splat/Scenes/OrientedSplat.unity
Assets/ARRealismDemos/LaserBeam/Scenes/LaserBeam.unity
Assets/ARRealismDemos/Relighting/Scenes/PointsRelighting.unity
Assets/ARRealismDemos/DepthEffects/Scenes/FogEffect.unity
Assets/ARRealismDemos/SnowParticles/Scenes/ArCoreSnowParticles.unity
Assets/ARRealismDemos/RainParticles/Scenes/RainParticlesScene.unity
Assets/ARRealismDemos/DepthEffects/Scenes/DepthOfFieldEffect.unity
Assets/ARRealismDemos/Water/Scenes/Water.unity
Assets/ARRealismDemos/CollisionDetection/Scenes/CollisionAwareObjectPlacement.unity
Assets/ARRealismDemos/ScreenSpaceDepthMesh/Scenes/ScreenSpaceDepthMesh.unity
Assets/ARRealismDemos/ScreenSpaceDepthMesh/Scenes/StereoPhoto.unity`

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

We would like to also thank Levana Chen, Phoenix Huang, and Ted Bisson for
integrating DepthLab with AR Foundation.

## Additional information

You may use this software under the
[Apache 2.0 License](https://github.com/googlesamples/arcore-depth-lab/blob/master/LICENSE).
