//-----------------------------------------------------------------------
// <copyright file="OccludedTransparentShadow.shader" company="Google LLC">
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
Shader "ARRealism/OccludedTransparentShadow"
{
    Properties
    {
        _ShadowIntensity("Shadow Intensity", Range(0, 1)) = 0.24
        _OcclusionBias("Occlusion Bias", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "DisableBatching"="True" }

        Blend SrcAlpha OneMinusSrcAlpha
        LOD 200
        ZWrite off

        Pass
        {
            Name "SHADOW_ONLY"
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Assets/GoogleARCore/SDK/Materials/ARCoreDepth.cginc"

            uniform float _ShadowIntensity;
            uniform float _OcclusionBias;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float2 uvDepth : TEXCOORD2;
                LIGHTING_COORDS(2,3)
                UNITY_FOG_COORDS(4)
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.texcoord.xy;
                float4 screenPos = ComputeScreenPos(o.pos);
                float2 screenUV = screenPos.xy / screenPos.w;
                o.uvDepth = ArCoreDepth_GetUv(screenUV);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                fixed alpha = 1 - atten;
                if (alpha == 0.0)
                {
                    // Pixel is not in shadow.
                    return 0.0;
                }

                const fixed3 _ShadowColor = fixed3(0.0, 0.0, 0.0);
                const fixed _FadeRadius = 0;
                fixed ratio = distance(i.uv, fixed2(0.5, 0.5)) * 2;
                if (ratio >= 1.0)
                {
                    // Pixel is out of shadow range.
                    return 0.0;
                }
                else if (ratio > _FadeRadius)
                {
                    // Pixel is in fade-out range.
                    alpha *= ((1 - ratio) / (1 - _FadeRadius));
                }

                float realDepth = ArCoreDepth_GetMeters(i.uvDepth);
                float virtualDepth = -UnityWorldToViewPos(i.worldPos).z;
                float signedDiffMeters = realDepth - virtualDepth;
                float visibility = 1.0;
                // We actually need to discard small differences, due to to noise between the plane and depth map.
                if (abs(signedDiffMeters) > _OcclusionBias) {
                    visibility = saturate(0.0);
                }
                alpha *= visibility;

                fixed4 col;
                col.rgb = _ShadowColor.rgb;
                col.a = _ShadowIntensity * alpha;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        Pass {
            Tags {
                "LightMode" = "ShadowCaster"
            }

            CGPROGRAM

            #pragma target 3.0

            #pragma multi_compile_shadowcaster

            #pragma vertex MyShadowVertexProgram
            #pragma fragment MyShadowFragmentProgram

            #include "UnityCG.cginc"

            struct VertexData {
                float4 position : POSITION;
                float3 normal : NORMAL;
            };

            float4 MyShadowVertexProgram (VertexData v) : SV_POSITION {
                float4 position =
                UnityClipSpaceShadowCasterPos(v.position.xyz, v.normal);
                return UnityApplyLinearShadowBias(position);
            }

            half4 MyShadowFragmentProgram () : SV_TARGET {
                return 0;
            }

            ENDCG
        }
    }
}
