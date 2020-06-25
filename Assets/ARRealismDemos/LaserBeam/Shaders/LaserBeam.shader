//-----------------------------------------------------------------------
// <copyright file="LaserBeam.shader" company="Google LLC">
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
Shader "ARRealism/Relighting/LaserBeam"
{
    Properties {
        _MainTex ("Laser Color", 2D) = "red"
        _NoiseTex ("2D Perlin Noise", 2D) = "white"
        _GammaValue ("Gamma Value", Float) = 0.25
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    sampler2D _NoiseTex;

    half4 _MainTex_ST;
    half4 _NoiseTex_ST;
    float _GammaValue;

    fixed4 _TintColor;

    struct v2f
    {
        half4 pos : SV_POSITION;
        half4 uv : TEXCOORD0;
    };

    v2f vert(appdata_full v)
    {
        v2f o;

        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
        o.uv.zw = TRANSFORM_TEX(v.texcoord, _NoiseTex);

        return o;
    }

    fixed4 frag( v2f i ) : COLOR
    {
        // Masks the laser color with 2D Perlin noise.
        float4 result = tex2D(_MainTex, i.uv.xy) * tex2D(_NoiseTex, i.uv.zw);

        // Strengthens the central color.
        result.rgb = pow(result.rgb, _GammaValue);

        return result;
    }

    ENDCG

    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

            ENDCG
        }

    }
    FallBack Off
}
