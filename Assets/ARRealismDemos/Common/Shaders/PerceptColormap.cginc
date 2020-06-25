//-----------------------------------------------------------------------
// <copyright file="PerceptColormap.cginc" company="Google LLC">
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

// A perceptually-correct depth map visualization inspired by Turbo colormap.
fixed3 GetPolynomialColor(float x,
    float4 kRedVec4, float4 kGreenVec4, float4 kBlueVec4,
    float2 kRedVec2, float2 kGreenVec2, float2 kBlueVec2) {
    x = saturate(x * 0.94 + 0.03);
    float4 v4 = float4(1.0, x, x * x, x * x * x);
    float2 v2 = v4.zw * v4.z;
    return fixed3(
        dot(v4, kRedVec4) + dot(v2, kRedVec2),
        dot(v4, kGreenVec4) + dot(v2, kGreenVec2),
        dot(v4, kBlueVec4) + dot(v2, kBlueVec2)
    );
}

fixed3 PerceptColormap(in float x) {
    const float4 kRedVec4 = float4(0.55305649, 3.00913185, -5.46192616, -11.11819092);
    const float4 kGreenVec4 = float4(0.16207513, 0.17712472, 15.24091500, -36.50657960);
    const float4 kBlueVec4 = float4(-0.05195877, 5.18000081, -30.94853351, 81.96403246);
    const float2 kRedVec2 = float2(27.81927491, -14.87899417);
    const float2 kGreenVec2 = float2(25.95549545, -5.02738237);
    const float2 kBlueVec2 = float2(-86.53476570, 30.23299484);

    return GetPolynomialColor(x, kRedVec4, kGreenVec4, kBlueVec4, kRedVec2, kGreenVec2, kBlueVec2);
}
