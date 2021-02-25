Shader "Hidden/ViewSpaceNormal"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
                float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
			};

			//tranformamos el normal en view space
			v2f vert (appdata v)
			{
				v2f o;
				//Transforms a point from object space to the cameras clip space in homogeneous coordinates. 
				o.vertex = UnityObjectToClipPos(v.vertex);
				// Inverse transpose of model * view matrix.
				o.normal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));
				return o;
			}
            
			//pixel normal
			fixed4 frag (v2f i) : SV_Target
			{
				return half4 (i.normal * 0.5 + 0.5, 1.0f);
			}
			ENDCG
		}
	}
}
