//-----------------------------------------------------------------------
// <copyright file="AnimatedRainParticles.shader" company="Google LLC">
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

Shader "GPUParticles/AnimatedRainParticles"
{
    Properties
    {
        _PositionTex ("Position Texture", 2D) = "white" {}
        _VelocityTex ("Velocity Texture", 2D) = "white" {}
        _ParticleTex ("Particle Texture", 2D) = "white" {}
        _CurrentDepthTexture ("Depth Texture", 2D) = "black" {}
        _ParticleSize ("Particle size", Float) = 0.5
        _WorldSize ("WorldSize", Vector) = (50,30,20,0)
        _UseNormalizedDepth("Use normalized depth", Float) = 0
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
                float2 sprite_uv : TEXCOORD1;
                float2 screen_space_uv : TEXCOORD2;
                float depth: TEXCOORD4;
                float4 vertex : SV_POSITION;
            };

            sampler2D _PositionTex;
            float4 _PositionTex_ST;

            sampler2D _VelocityTex;
            float4 _VelocityTex_ST;

            sampler2D _ParticleTex;
            float4 _ParticleTex_ST;

            float4 _WorldSize;
            float _ParticleSize;
            float _UseNormalizedDepth;

            // _PositionTex texture.
            // xyz holds particle position.
            // w holds collision state.

            // _VelocityTex texture.
            // xy holds direction.
            // z holds a timer used to do sprite animation.
            // w holds particle lifetime.

            v2f vert (appdata v)
            {
                const float3 world_size = _WorldSize.xyz;
                const float3 position_offset = float3(0.5, 0.5, 0.5);
                const float3 particle_forward_direction = float3(0, 0, 1);

                float4 uv = float4(v.uv.xy,0,0);
                float4 current_position = tex2Dlod(_PositionTex, uv);
                float4 position = v.vertex;

                float3 particle_position_world_space =
                  (current_position.xyz - position_offset) * world_size;

                if (current_position.w == 0)
                {
                    // Calculates correct rotation in order to have the particle always facing the camera.
                    float3 particle_facing_direction = UnityWorldSpaceViewDir(particle_position_world_space);

                    // Don't let particles rotate on the y axis.
                    particle_facing_direction.y = 0;
                    particle_facing_direction = normalize(particle_facing_direction);

                    float4 billboard_rotation = rotation_from_to_vector(particle_forward_direction, particle_facing_direction);
                    position.xyz = rotate_vertex_by_quaternion(position.xyz, billboard_rotation);

                    // Orients particles towards the direction of the rain.
                    float3 rain_drop_direction = float3(0, -1, 0);
                    float3 rain_target_direction = normalize(float3(-0.4, -1, 0));
                    float4 rain_rotation = rotation_from_to_vector(rain_drop_direction, rain_target_direction);
                    position.xyz = rotate_vertex_by_quaternion(position.xyz, rain_rotation);

                    // Scale quad while in rain drop state.
                    const float3 rain_drop_scale = float3(1.0, 2.0, 1.0);
                    position.xyz = position.xyz * rain_drop_scale;
                }
                else
                {
                    // Makes particle quad face up, so we can play the rain splash animation.
                    const float3 particle_facing_direction = float3(0, 1, 0);
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
                o.screen_space_uv = ((clip_space_pos.xy/clip_space_pos.w) + float2(1, 1)) * 0.5;

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

                // Calculate sprite uvs for this frame, texture uvs will change when particle has collided so we
                // can play the rain splash animation.
                float4 velocity = tex2Dlod (_VelocityTex, uv);
                float2 texture_space_uv = ((v.vertex.xy / float2(half_particle_size, half_particle_size)) + 1.0) * 0.5;

                // Number of sprites on the sprite sheet (column count, row count).
                const float2 sprite_gride_size = float2(4.0f, 4.0f);
                const float2 sprite_size = float2(1.0, 1.0) / sprite_gride_size;
                const float total_frames = (sprite_gride_size.x * sprite_gride_size.y) - 1.0;

                // This bias fixes the one frame delay in the sprite animation.
                float sprite_uv_collision_bias = current_position.w * 0.1;

                float sprite_index = floor(clamp(velocity.z + sprite_uv_collision_bias, 0.0, 1.0) * total_frames);
                float uv_coords = sprite_index / 4.0;
                float2 sprite_origin = float2(frac(uv_coords), (sprite_gride_size.y - 1.0 - floor(uv_coords)) * sprite_size.y);
                o.sprite_uv = (texture_space_uv * sprite_size) + sprite_origin;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 position = tex2D(_PositionTex, i.particle_uv);
                float4 velocity = tex2D(_VelocityTex, i.particle_uv);
                float4 color = tex2D(_ParticleTex, i.sprite_uv);

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

                // Rain drops have higher transparency than drop splashes on the ground.
                float rain_drop_alpha = 0.25 + (position.w * 0.75);

                float4 result;
                result.rgb = pow(color.rgb, float3(1, 1, 1) / 2.2);
                result.a = color.a * rain_drop_alpha * lifetime_alpha * occlusion_alpha * camera_distance_alpha;
                return result;
            }
            ENDCG
        }
    }
}
