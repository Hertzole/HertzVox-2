// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "HertzVox/Chunk"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		[NoScaleOffset]_MainTex("Main Texture", 2D) = "white" {}
		[HideInInspector]_AtlasRec("Atlas Rect", Vector) = (0,0,0,0)
		[HideInInspector]_AtlasY("Atlas Y", Float) = 0
		[HideInInspector] _tex4coord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float4 uv_tex4coord;
		};

		uniform sampler2D _MainTex;
		uniform float4 _AtlasRec;
		uniform float _AtlasY;
		uniform float _Cutoff = 0.5;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 appendResult74 = (float2(( ( frac( i.uv_tex4coord.x ) * _AtlasRec.x ) + ( i.uv_tex4coord.z * _AtlasRec.x ) ) , ( ( frac( i.uv_tex4coord.y ) * _AtlasRec.y ) + ( _AtlasRec.y * ( i.uv_tex4coord.w - _AtlasY ) ) )));
			float4 tex2DNode65 = tex2D( _MainTex, appendResult74 );
			o.Albedo = ( i.vertexColor * tex2DNode65 ).rgb;
			o.Alpha = 1;
			clip( tex2DNode65.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=17600
-1597;100;1360;878;181.7327;337.1644;1;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;64;-910.8805,-54.00922;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;67;-930.8805,341.9908;Inherit;False;Property;_AtlasY;Atlas Y;3;1;[HideInInspector];Create;False;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;66;-866.8805,131.9908;Inherit;False;Property;_AtlasRec;Atlas Rect;2;1;[HideInInspector];Create;False;0;0;False;0;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FractNode;68;-626.8805,-143.0092;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;71;-620.8805,269.9908;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;69;-596.8805,105.9908;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;75;-434.201,124.9895;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;-449.201,272.9895;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;70;-501.8805,-37.00922;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;-494.8805,-143.0092;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;73;-353.8805,-98.00922;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;77;-285.201,279.9895;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;74;-161.201,95.9895;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;63;123.8341,-227.9996;Inherit;True;Property;_MainTex;Main Texture;1;1;[NoScaleOffset];Create;False;0;0;False;0;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.VertexColorNode;79;442.2673,-94.16443;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;65;322.834,144.0004;Inherit;True;Property;_TextureSample0;Texture Sample 0;2;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;705.2673,22.83557;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;926.1145,6.809644;Float;False;True;-1;2;;0;0;Standard;HertzVox/Chunk;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;Test;AlphaTest;ForwardOnly;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0.88;0,0,0,0;VertexScale;True;False;Cylindrical;False;Relative;0;;0;0;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;68;0;64;1
WireConnection;71;0;64;4
WireConnection;71;1;67;0
WireConnection;69;0;64;2
WireConnection;75;0;69;0
WireConnection;75;1;66;2
WireConnection;76;0;66;2
WireConnection;76;1;71;0
WireConnection;70;0;64;3
WireConnection;70;1;66;1
WireConnection;72;0;68;0
WireConnection;72;1;66;1
WireConnection;73;0;72;0
WireConnection;73;1;70;0
WireConnection;77;0;75;0
WireConnection;77;1;76;0
WireConnection;74;0;73;0
WireConnection;74;1;77;0
WireConnection;65;0;63;0
WireConnection;65;1;74;0
WireConnection;80;0;79;0
WireConnection;80;1;65;0
WireConnection;0;0;80;0
WireConnection;0;10;65;4
ASEEND*/
//CHKSM=F29C684748CC245B9E95A98D778F25B779C8A956