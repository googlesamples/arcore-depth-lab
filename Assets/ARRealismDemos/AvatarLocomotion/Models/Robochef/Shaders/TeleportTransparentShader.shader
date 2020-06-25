//-----------------------------------------------------------------------
// <copyright file="TeleportTransparentShader.shader" company="Google LLC">
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

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X
Shader "Google Occlusion/Robot Teleport Transparent"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "white" {}
        _Smoothness("Smoothness", Range( 0 , 1)) = 0.85
        _TeleportPhase("Teleport Phase", Range( 0 , 1)) = 0
        [HideInInspector] _texcoord( "", 2D ) = "white" {}
        [HideInInspector] __dirty( "", Int ) = 1
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
        Cull Back
        CGINCLUDE
        #include "UnityPBSLighting.cginc"
        #include "Lighting.cginc"
        #pragma target 3.0
        struct Input
        {
            float2 uv_texcoord;
        };

        uniform sampler2D _Albedo;
        uniform float4 _Albedo_ST;
        uniform float _Smoothness;
        uniform float _TeleportPhase;

        void surf( Input i , inout SurfaceOutputStandard o )
        {
            float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
            float4 tex2DNode1 = tex2D( _Albedo, uv_Albedo );
            o.Albedo = tex2DNode1.rgb;
            o.Smoothness = _Smoothness;
            o.Alpha = ( tex2DNode1.a * saturate( (1.0 + (_TeleportPhase - 0.2) * (0.0 - 1.0) / (0.3 - 0.2)) ) );
        }

        ENDCG
        CGPROGRAM
        #pragma surface surf Standard alpha:fade keepalpha fullforwardshadows

        ENDCG
        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_shadowcaster
            #pragma multi_compile UNITY_PASS_SHADOWCASTER
            #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
            #include "HLSLSupport.cginc"
            #if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
                #define CAN_SKIP_VPOS
            #endif
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"
            sampler3D _DitherMaskLOD;
            struct v2f
            {
                V2F_SHADOW_CASTER;
                float2 customPack1 : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            v2f vert( appdata_full v )
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID( v );
                UNITY_INITIALIZE_OUTPUT( v2f, o );
                UNITY_TRANSFER_INSTANCE_ID( v, o );
                Input customInputData;
                float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
                half3 worldNormal = UnityObjectToWorldNormal( v.normal );
                o.customPack1.xy = customInputData.uv_texcoord;
                o.customPack1.xy = v.texcoord;
                o.worldPos = worldPos;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
                return o;
            }
            half4 frag( v2f IN
            #if !defined( CAN_SKIP_VPOS )
            , UNITY_VPOS_TYPE vpos : VPOS
            #endif
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID( IN );
                Input surfIN;
                UNITY_INITIALIZE_OUTPUT( Input, surfIN );
                surfIN.uv_texcoord = IN.customPack1.xy;
                float3 worldPos = IN.worldPos;
                half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
                SurfaceOutputStandard o;
                UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
                surf( surfIN, o );
                #if defined( CAN_SKIP_VPOS )
                float2 vpos = IN.pos;
                #endif
                half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
                clip( alphaRef - 0.01 );
                SHADOW_CASTER_FRAGMENT( IN )
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
    CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15900
2416;1;1369;1051;798.6282;704.4045;1.3;True;True
Node;AmplifyShaderEditor.RangedFloatNode;3;-413.6998,102.9;Float;False;Property;_TeleportPhase;Teleport Phase;2;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;6;-118.7282,97.6957;Float;False;5;0;FLOAT;0;False;1;FLOAT;0.2;False;2;FLOAT;0.3;False;3;FLOAT;1;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-426,-225.8;Float;True;Property;_Albedo;Albedo;0;0;Create;True;0;0;False;0;None;80eef4761b3b5ae4f9f3fce921abedb0;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;7;74.97184,97.69563;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;2;-420.6001,0.7999964;Float;False;Property;_Smoothness;Smoothness;1;0;Create;True;0;0;False;0;0.85;0.9;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;272.9,-27.80001;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;464.2,-264.5;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;Google Occlusion/Robot Teleport Transparent;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;6;0;3;0
WireConnection;7;0;6;0
WireConnection;4;0;1;4
WireConnection;4;1;7;0
WireConnection;0;0;1;0
WireConnection;0;4;2;0
WireConnection;0;9;4;0
ASEEND*/
//CHKSM=E287C42066B239ECBE6DED3E3E96820E41E48B5B
