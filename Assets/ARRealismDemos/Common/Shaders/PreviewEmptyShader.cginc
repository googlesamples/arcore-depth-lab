//-----------------------------------------------------------------------
// <copyright file="PreviewEmptyShader.cginc" company="Google LLC">
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

// For instant preview in Unity. Only shows if the uv coordinate is correct.
#pragma exclude_renderers gles3 gles
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

uniform sampler2D _MainTex;
uniform float4 _UvTopLeftRight;
uniform float4 _UvBottomLeftRight;

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};

v2f vert(appdata v)
{
    float2 uvTop = lerp(_UvTopLeftRight.xy, _UvTopLeftRight.zw, v.uv.x);
    float2 uvBottom = lerp(_UvBottomLeftRight.xy, _UvBottomLeftRight.zw, v.uv.x);

    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = lerp(uvTop, uvBottom, v.uv.y);

    // Instant preview's texture is transformed differently.
    o.uv = o.uv.yx;
    o.uv.x = 1.0 - o.uv.x;

    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    return fixed4(i.uv.xyy, 1.0);
}
