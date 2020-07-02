//-----------------------------------------------------------------------
// <copyright file="PointCloudShader.shader" company="Google LLC">
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

Shader "ARRealism/PointCloudShader"
{
    Properties
    {
        _PointSize("Point size", Range(0, 0.2)) = 0.004
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            #include "UnityCG.cginc"

            float _PointSize;

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            struct g2f
            {
                fixed4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2g vert (appdata_full v)
            {
                v2g o;
                o.vertex = mul(unity_ObjectToWorld, v.vertex);
                o.color = v.color;
                return o;
            }

            [maxvertexcount(4)]
            void geom (point v2g p[1], inout TriangleStream<g2f> triStream)
            {
                float3 up = float3(0, 1, 0);
                float3 look = _WorldSpaceCameraPos - p[0].vertex;
                look.y = 0;
                look = normalize(look);
                float3 right = cross(up, look);

                float halfSize = 0.5f * _PointSize;

                float4 v[4];
                v[0] = float4(p[0].vertex + halfSize * right - halfSize * up, 1.0f);
                v[1] = float4(p[0].vertex + halfSize * right + halfSize * up, 1.0f);
                v[2] = float4(p[0].vertex - halfSize * right - halfSize * up, 1.0f);
                v[3] = float4(p[0].vertex - halfSize * right + halfSize * up, 1.0f);

                g2f pIn;
                pIn.vertex = UnityObjectToClipPos(v[0]);
                pIn.color = p[0].color;
                triStream.Append(pIn);

                pIn.vertex = UnityObjectToClipPos(v[1]);
                pIn.color = p[0].color;
                triStream.Append(pIn);

                pIn.vertex = UnityObjectToClipPos(v[2]);
                pIn.color = p[0].color;
                triStream.Append(pIn);

                pIn.vertex = UnityObjectToClipPos(v[3]);
                pIn.color = p[0].color;
                triStream.Append(pIn);
            }

            fixed4 frag (g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
