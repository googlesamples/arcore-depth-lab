//-----------------------------------------------------------------------
// <copyright file="Position.shader" company="Google LLC">
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

Shader "GPUParticles/Position"
{
    Properties
    {
        _PositionTex ("Position", 2D) = "white" {}
        _VelocityTex ("Velocity", 2D) = "white" {}
        _CurrentDepthTexture ("Depth Texture", 2D) = "black" {}
        _ParticleVelocity ("ParticleVelocity", Vector) = (0.5,0.5,0,0)
        _WorldSize ("WorldSize", Vector) = (50,30,20,0)
        _UseNormalizedDepth("Use normalized depth", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature USE_STATIC_DEPTH

            #include "UnityCG.cginc"
            #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4x4 _ParticleCameraViewMatrix;
            float4x4 _ParticleCameraViewProjectionMatrix;
            float _ParticleCameraNear;
            float _ParticleCameraFar;

            sampler2D _PositionTex;
            float4 _PositionTex_ST;

            sampler2D _VelocityTex;
            float4 _VelocityTex_ST;

            float4 _ParticleVelocity;
            float4 _WorldSize;
            float _UseNormalizedDepth;

            // Note: uniformly distributed, normalized rand, [0, 1].
            float nrand( float2 n )
            {
                return frac(sin(dot(n.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _PositionTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 current_position = tex2D(_PositionTex, i.uv);
                float4 current_velocity = tex2D(_VelocityTex, i.uv);

                // Calculates the particle directions. By removing 0.5 from each
                // component except z, we can have directions along both the
                // positive and negative parts of the axis.
                float3 direction = normalize(current_velocity.xyz - float3(0.5, 0.5, 0));
                float lifetime = current_velocity.w;

                float4 result;

                // Updates the particle position.
                result.xyz = current_position.xyz + direction * _ParticleVelocity.xyz * unity_DeltaTime.x;

                // W component stores the particle collision state, reset it to "no collision".
                result.w = 0;

                // If lifetime is zero, respawn particle.
                if (lifetime == 0)
                {
                    result.z = nrand(result.xy + i.uv);
                    result.x = nrand(i.uv);
                    result.y = 1;
                }
                else
                {
                    const float3 position_offset = float3(0.5, 0.5, 0.5);
                    const float3 height_offset = float3(0, 0.007, 0);

                    const float3 world_size = _WorldSize.xyz;
                    float4 particle_world_position = float4(((current_position.xyz - position_offset) - height_offset) * world_size, 1);
                    float4 particle_clip_space_pos = mul(_ParticleCameraViewProjectionMatrix, particle_world_position);
                    float2 particle_screen_space_uv = ((particle_clip_space_pos.xy / particle_clip_space_pos.w) + float2(1, 1)) * 0.5;

                    if (_UseNormalizedDepth == 0)
                    {
                        particle_screen_space_uv = ArCoreDepth_GetUv(particle_screen_space_uv);
                    }

                    if (particle_screen_space_uv.x > 0.0 && particle_screen_space_uv.x < 1.0 &&
                        particle_screen_space_uv.y > 0.0 && particle_screen_space_uv.y < 1.0)
                    {

                        // To calculate the vertex depth here, we use the view matrix of the camera
                        // that renders the particles.
                        float vertex_depth = -mul(_ParticleCameraViewMatrix, particle_world_position).z;

                        float real_depth = 0.0;
                        if (_UseNormalizedDepth == 1)
                        {
                            // Normalize vertex depth.
                            vertex_depth = (vertex_depth - _ParticleCameraNear) /
                              (_ParticleCameraFar - _ParticleCameraNear);
                            real_depth = 1.0 - tex2D(_CurrentDepthTexture, particle_screen_space_uv).x;
                        }
                        else
                        {
                            real_depth = ArCoreDepth_GetMeters(particle_screen_space_uv);
                        }

                        if ((vertex_depth - real_depth) > 0.0)
                        {
                            // Particle has collided.
                            result.xyz = current_position.xyz;
                            result.w = 1; // Flags particle as "colliding".
                        }
                    }
                }

                return result;
            }
            ENDCG
        }
    }
}
