//-----------------------------------------------------------------------
// <copyright file="RenderParticles.shader" company="Google LLC">
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

Shader "GPUParticles/RenderParticles"
{
    Properties
    {
        _PositionTex ("Position Texture", 2D) = "white" {}
        _VelocityTex ("Velocity Texture", 2D) = "white" {}
        _ParticleTex ("Particle Texture", 2D) = "white" {}
        _ParticleNormalTex ("Particle Normal Texture", 2D) = "bump" {}
        _CurrentDepthTexture ("Depth Texture", 2D) = "black" {}
        _ParticleSize ("Particle size", Float) = 0.5
        _WorldSize ("WorldSize", Vector) = (50,30,20,0)
        _UseNormalizedDepth("Use normalized depth", Float) = 0
        _LightDir("Light Dir", Vector) = (-1,0,0,1)
        _DepthIntrinsics ("Depth texture intrinsics", Vector) = (160 , 90, 127, 127)
        _OrientParticles ("Orient particles according to surface normal (0 or 1)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparency" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature USE_STATIC_DEPTH

            #include "UnityCG.cginc"
            #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"
            #include "Assets/ARRealismDemos/SnowParticles/Shaders/QuaternionMath.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
                float4 tangent: TANGENT;
            };

            struct v2f
            {
                float2 particle_uv : TEXCOORD0;
                float2 texture_space_uv : TEXCOORD1;
                float2 screen_space_uv : TEXCOORD2;
                float depth: TEXCOORD4;
                float4 vertex : SV_POSITION;

                half3 tspace0 : TEXCOORD5; // tangent.x, bitangent.x, normal.x
                half3 tspace1 : TEXCOORD6; // tangent.y, bitangent.y, normal.y
                half3 tspace2 : TEXCOORD7; // tangent.z, bitangent.z, normal.z

                half3 viewDir : TEXCOORD8;
                half3 normalDir : TEXCOORD9;
            };

            sampler2D _PositionTex;
            float4 _PositionTex_ST;

            sampler2D _VelocityTex;
            float4 _VelocityTex_ST;

            sampler2D _ParticleTex;
            float4 _ParticleTex_ST;

            sampler2D _ParticleNormalTex;
            float4 _ParticleNormalTex_ST;

            float4 _WorldSize;
            float _ParticleSize;
            float _UseNormalizedDepth;
            float _OrientParticles;

            float4 _LightDir;

            float4 _DepthIntrinsics;

            // Estimates the normal vector for each point based on neighborhood depth data.
            //
            // Normals are computed as unit-length vectors, pointing TOWARD the camera,
            // which means a negative z-value.
            // If the z-component of any pixel's normal is zero, that indicates the normal
            // was not computed.  Otherwise, all normal vectors have unit length.
            float3 GetNormal(float2 uv, float depth)
            {
                if (depth == 0.0) {
                    return float3(0.0, 0.0, 0.0);
                }

                const float kOutlierDepthRatio = 0.2;
                const int kWindowwindow_sizePixels = 2;

                // Iterates over neighbors to compute normal vector.
                float neighbor_corr_x = 0.0;
                float neighbor_corr_y = 0.0;
                float outlier_distance = kOutlierDepthRatio * depth;
                int window_size = kWindowwindow_sizePixels;
                float neighbor_sum_confidences_x = 0.0f;
                float neighbor_sum_confidences_y = 0.0f;

                float2 texel_size = float2(1.0 / _DepthIntrinsics.x, 1.0 / _DepthIntrinsics.y);
                float2 focal_length = float2(_DepthIntrinsics.z, _DepthIntrinsics.w);

                // Searches neighbors.
                for (int dy = -window_size; dy <= window_size; ++dy) {
                    for (int dx = -window_size; dx <= window_size; ++dx) {
                        // Self isn't a neighbor.
                        if (dx == 0 && dy == 0) {
                            continue;
                        }
                        float2 dir = float2(dx, dy) * texel_size;
                        float neighbor_depth = ArCoreDepth_GetMeters(uv + dir);

                        if (neighbor_depth == 0.0) {
                            continue;  // Neighbor does not exist.
                        }

                        float neighbor_distance = neighbor_depth - depth;

                        // Checks for outliers.
                        if (abs(neighbor_distance) > outlier_distance) {
                            continue;
                        }

                        // Updates correlations in each dimension.
                        if (dx != 0) {
                            ++neighbor_sum_confidences_x;
                            neighbor_corr_x += neighbor_distance / float(dx);
                        }
                        if (dy != 0) {
                            ++neighbor_sum_confidences_y;
                            neighbor_corr_y += neighbor_distance / float(dy);
                        }
                    }
                }

                if (neighbor_sum_confidences_x == 0.0 || neighbor_sum_confidences_y == 0.0) {
                    return float3(0.0, 0.0, 0.0);
                }

                // Computes estimate of normal vector by finding weighted averages of
                // the surface gradient in x and y.
                float pixel_width  = depth / focal_length.x;
                float pixel_height = depth / focal_length.y;

                float slope_x = neighbor_corr_x / (pixel_width * neighbor_sum_confidences_x);
                float slope_y = neighbor_corr_y / (pixel_height * neighbor_sum_confidences_y);

                return normalize(float3(-slope_y, -slope_x, 1.0f));
            }

            v2f vert (appdata v)
            {
                const float3 world_size = _WorldSize.xyz;
                const float3 position_offset = float3(0.5, 0.5, 0.5);
                const float3 particle_forward_direction = float3(0, 0, 1);

                float4 uv = float4(v.uv.xy,0,0);
                float4 current_position = tex2Dlod (_PositionTex, uv);
                float4 position = v.vertex;

                float3 particle_position_world_space = (current_position.xyz - position_offset) * world_size;
                float3 view_dir =  UnityWorldSpaceViewDir(particle_position_world_space);

                if ((current_position.w * _OrientParticles) == 0)
                {
                    // Calculates correct rotation in order to have the particle always facing the camera.
                    float4 rotation_quat = rotation_from_to_vector(particle_forward_direction, view_dir);
                    position.xyz = rotate_vertex_by_quaternion(position.xyz, rotation_quat);
                }
                else
                {
                    // Makes particle quad face face up to the collision normal.
                    float4 clip_space_pos = UnityObjectToClipPos(particle_position_world_space);
                    float2 screen_space_uv = ArCoreDepth_GetUv(((clip_space_pos.xy/clip_space_pos.w) + float2(1, 1)) * 0.5);
                    float depth = ArCoreDepth_GetMeters(screen_space_uv);

                    float3 particle_facing_direction = GetNormal(screen_space_uv, depth);
                    // Give the collision normal a bias towards facing the camera.
                    particle_facing_direction = normalize(particle_facing_direction + (view_dir * 0.25));

                    float4 rotation_quat = rotation_from_to_vector(particle_forward_direction, particle_facing_direction);
                    position.xyz = rotate_vertex_by_quaternion(position.xyz, rotation_quat);
                }

                // Translates particle to current world space position.
                position.xyz += particle_position_world_space;

                float4 clip_space_pos = UnityObjectToClipPos(position);
                float half_particle_size = _ParticleSize * 0.5;

                v2f o;
                o.vertex = clip_space_pos;
                o.particle_uv = uv.xy;
                o.texture_space_uv = ((v.vertex.xy / float2(half_particle_size, half_particle_size)) + 1.0) * 0.5;
                o.screen_space_uv = ((clip_space_pos.xy/clip_space_pos.w) + float2(1, 1)) * 0.5;

                // Sets up the normal map.
                o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
                o.normalDir = v.normal;

                half3 wNormal = UnityObjectToWorldNormal(v.normal);
                half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);

                // Computes bitangent from cross product of normal and tangent.
                half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                half3 wBitangent = cross(wNormal, wTangent) * tangentSign;

                // Outputs the tangent space matrix.
                o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
                o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);

                if (_UseNormalizedDepth == 1)
                {
                    float depth = -mul(UNITY_MATRIX_V, position).z;
                    o.depth = (depth - _ProjectionParams.y) /
                      (_ProjectionParams.z - _ProjectionParams.y);
                }
                else
                {
                    o.screen_space_uv = ArCoreDepth_GetUv(o.screen_space_uv);
                    o.depth = -UnityWorldToViewPos(position).z;
                }

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 position = tex2D(_PositionTex, i.particle_uv);
                float4 velocity = tex2D(_VelocityTex, i.particle_uv);
                float4 color = tex2D(_ParticleTex, i.texture_space_uv);

                half2 uv_normal = TRANSFORM_TEX (i.texture_space_uv, _ParticleNormalTex);
                half3 normal = UnpackNormal(tex2D(_ParticleNormalTex, uv_normal));

                // transform normal from tangent to world space
                half3 worldNormal;
                worldNormal.x = dot(i.tspace0, normal);
                worldNormal.y = dot(i.tspace1, normal);
                worldNormal.z = dot(i.tspace2, normal);

                half diffuse = saturate(dot(_LightDir.xyz, worldNormal) * 1.2);

                float vertex_depth = i.depth;
                float real_depth = 0.0;

                if (_UseNormalizedDepth == 1)
                {
                    real_depth = 1.0 - tex2D(_CurrentDepthTexture, i.screen_space_uv.xy).x;
                }
                else
                {
                    real_depth = ArCoreDepth_GetMeters(i.screen_space_uv);
                }

                // Fades particle according to how far it is from depth.
                // If particle is colliding increase fade tolerance.
                float tolerance = 0.25 + (0.25 * position.w);
                float occlusion_alpha = smoothstep(vertex_depth - tolerance, vertex_depth + tolerance, real_depth);

                // Fades particle if too close to camera.
                float camera_distance_alpha = smoothstep(0.2, 0.4, vertex_depth);

                // Fades particle according to lifetime.
                float lifetime_alpha = (1.0 - smoothstep(0.05, 0, velocity.w));

                float4 result;
                result.rgb = pow(color.rgb * diffuse, float3(1, 1, 1) / 2.2);
                result.a = color.a * lifetime_alpha * occlusion_alpha * camera_distance_alpha;
                return result;
            }
            ENDCG
        }
    }
}
