// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Kinect/BodyShader" {
//    Properties {
//        _MainTex ("Base (RGB)", 2D) = "black" {}
//    }
    
	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
		
			CGPROGRAM
			#pragma target 5.0
			//#pragma enable_d3d11_debug_symbols

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			//uniform sampler2D _MainTex;
			uniform float _TexResX;
			uniform float _TexResY;
			uniform float4 _BodyIndexColors[100];

			StructuredBuffer<float> _BodyIndexBuffer;

			struct v2f {
				float4 pos : SV_POSITION;
			    float2 uv : TEXCOORD0;
			};

			v2f vert (appdata_base v)
			{
				v2f o;
				
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = v.texcoord;
				
				return o;
			}

			float4 frag (v2f i) : COLOR
			{
				int dx = (int)(i.uv.x * _TexResX);
				int dy = (int)(i.uv.y * _TexResY);
				int di = (int)(dx + dy * _TexResX);
				
				float player = _BodyIndexBuffer[di];
				if (player != 255)
				{
					//float clrPlayer = (240 + player) / 255;
					//return float4(clrPlayer, clrPlayer, clrPlayer, 1);

					//switch(player % 6)
					//{
					//	case 0:
					//		return float4(1, 1, 0, 1);  // yellow
					//	case 1:
					//		return float4(0, 1, 0, 1);  // green
					//	case 2:
					//		return float4(0, 0, 1, 1);  // blue
					//	case 3:
					//		return float4(1, 0, 1, 1);  // magenta
					//	case 4:
					//		return float4(0, 1, 1, 1);  // cyan
					//	case 5:
					//		return float4(1, 0, 0, 1);  // red
					//}

					float4 clrBody = _BodyIndexColors[player];
					return clrBody;
				}
				
				return float4(0, 0, 0, 0);
			}

			ENDCG
		}
	}

	Fallback Off
}