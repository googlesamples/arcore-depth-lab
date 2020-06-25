//-----------------------------------------------------------------------
// <copyright file="QuaternionMath.cginc" company="Google LLC">
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

// Largely taken from:
// https://www.geeks3d.com/20141201/how-to-rotate-a-vertex-by-a-quaternion-in-glsl/

float4 quat_conj(float4 q)
{
    return float4(-q.x, -q.y, -q.z, q.w);
}

float4 quat_mult(float4 q1, float4 q2)
{
    float4 qr;
    qr.x = (q1.w * q2.x) + (q1.x * q2.w) + (q1.y * q2.z) - (q1.z * q2.y);
    qr.y = (q1.w * q2.y) - (q1.x * q2.z) + (q1.y * q2.w) + (q1.z * q2.x);
    qr.z = (q1.w * q2.z) + (q1.x * q2.y) - (q1.y * q2.x) + (q1.z * q2.w);
    qr.w = (q1.w * q2.w) - (q1.x * q2.x) - (q1.y * q2.y) - (q1.z * q2.z);
    return qr;
}

float4 quat_from_axis_angle(float3 axis, float angle)
{
    float4 qr;
    float half_angle = (angle * 0.5) * 3.14159 / 180.0;
    qr.x = axis.x * sin(half_angle);
    qr.y = axis.y * sin(half_angle);
    qr.z = axis.z * sin(half_angle);
    qr.w = cos(half_angle);
    return qr;
}

float4 rotation_from_to_vector(float3 start, float3 dest) {
    start = normalize(start);
    dest = normalize(dest);
    float3 rotation_axis;

    float cos_theta = dot(start, dest);

    // Special case when vector is in opposite directions.
    if (cos_theta < -1 + 0.001){
        // Picks perpendicular vector to start.
        rotation_axis = cross(float3(0.0, 0.0, 1.0), start);
        // Picks another axis if parallel.
        if ( length(rotation_axis) < 0.01 )
            rotation_axis = cross(float3(1.0, 0.0, 0.0), start);

        rotation_axis = normalize(rotation_axis);
        return quat_from_axis_angle(radians(180.0), rotation_axis);
    }

    rotation_axis = cross(start, dest);
    float s = sqrt( (1+cos_theta)*2 );
    float invs = 1 / s;
    return float4(
        rotation_axis.x * invs,
        rotation_axis.y * invs,
        rotation_axis.z * invs,
        s * 0.5f
    );
}

float3 rotate_vertex_by_axis_angle(float3 position, float3 axis, float angle)
{
    float4 qr = quat_from_axis_angle(axis, angle);
    float4 qr_conj = quat_conj(qr);
    float4 q_pos = float4(position.x, position.y, position.z, 0);
    float4 q_tmp = quat_mult(qr, q_pos);
    qr = quat_mult(q_tmp, qr_conj);
    return float3(qr.x, qr.y, qr.z);
}

float3 rotate_vertex_by_quaternion(float3 position, float4 qr)
{
    float4 qr_conj = quat_conj(qr);
    float4 q_pos = float4(position.x, position.y, position.z, 0);
    float4 q_tmp = quat_mult(qr, q_pos);
    qr = quat_mult(q_tmp, qr_conj);
    return float3(qr.x, qr.y, qr.z);
}
