//-----------------------------------------------------------------------
// <copyright file="RainVelocity.shader" company="Google LLC">
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

Shader "GPUParticles/RainVelocity"
{
    Properties
    {
        _PositionTex ("Position", 2D) = "white" {}
        _VelocityTex ("Velocity", 2D) = "white" {}
        _ParticleLifetime ("Particle Lifetime", Float) = 10
        _EmitParticles ("Emit Particles", Float) = 0
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

            #include "UnityCG.cginc"

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

            sampler2D _PositionTex;
            float4 _PositionTex_ST;

            sampler2D _VelocityTex;
            float4 _VelocityTex_ST;

            float _ParticleLifetime;
            float _EmitParticles;

            float nrand(in float2 n)
            {
                return frac(sin(dot(n.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            float n8rand(in float2 n)
            {
                float t = frac( _Time );
                float nrnd0 = nrand( n + 0.07 * t );
                float nrnd1 = nrand( n + 0.11 * t );
                float nrnd2 = nrand( n + 0.13 * t );
                float nrnd3 = nrand( n + 0.17 * t );

                float nrnd4 = nrand( n + 0.19 * t );
                float nrnd5 = nrand( n + 0.23 * t );
                float nrnd6 = nrand( n + 0.29 * t );
                float nrnd7 = nrand( n + 0.31 * t );

                return (nrnd0+nrnd1+nrnd2+nrnd3 +nrnd4+nrnd5+nrnd6+nrnd7) / 8.0;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _VelocityTex);
                return o;
            }

            // _PositionTex texture.
            // xyz holds particle position.
            // w holds collision state.

            // _VelocityTex texture.
            // xy holds direction.
            // z holds a timer used to do sprite animation.
            // w holds particle lifetime.

            float4 frag (v2f i) : SV_Target
            {
                float4 result = tex2D(_VelocityTex, i.uv);
                float4 position = tex2D(_PositionTex, i.uv);

                // Iterates current active sprite index.
                float sprite_timer = result.z;

                // Rain direction is constant.
                // We have two free channels we could use for something else.
                result.xy = float2(0.48, 0);

                // If particle lifetime is zero, generate new velocity/direction and lifetime.
                if (result.w <= 0 && _EmitParticles == 1) {
                    result.w = n8rand(i.uv);
                    result.z = 0;
                }

                // Updates timer for sprite animation if particle has collided.
                if(position.w == 1.0) {
                    result.z = clamp(sprite_timer + unity_DeltaTime.x, 0.0, 1.0);
                }

                // Updates particle lifetime.
                const float lifetime_time_step = unity_DeltaTime.x * (1.0 / _ParticleLifetime);
                result.w = clamp(result.w - lifetime_time_step, 0.0, 1.0);
                return result;
            }
            ENDCG
        }
    }
}
