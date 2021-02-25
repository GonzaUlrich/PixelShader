Shader "Unlit/ToonLitSprite"
{
	Properties
	{
		[NoScaleOffset] _MainTex ("Diffuse Map", 2D) = "white" {}
        [NoScaleOffset] _NormalTex ("Normal Map", 2D) = "bump" {}
		_MinColorImpact("MinColorImpact", Range(0,1))=0.3
		[Toggle]_BackColor("BackColor", float)=0
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
        Cull Off
    
		Pass
		{
            Tags{ "LightMode" = "ForwardBase" }
            
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma target 3.0
			
			#include "UnityCG.cginc"
            #include "Lighting.cginc"

            
			struct appdata
			{
				float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
                float3 wNormal : TEXCOORD1;
				float3 wTangent : TEXCOORD2;
				float3 wBitangent : TEXCOORD3;
			};

			sampler2D _MainTex;
			sampler2D _NormalTex;
			fixed _MinColorImpact;
			float _BackColor;
			float4 _Color;
            
			v2f vert (appdata v)
			{
				v2f o;
                
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				//Pasar los normal tangent space a world
				//Son orthogonales entre ellos
				o.wNormal = UnityObjectToWorldNormal(v.normal);
				o.wTangent = UnityObjectToWorldNormal(v.tangent);
				o.wBitangent = cross(-o.wTangent, o.wNormal) * v.tangent.w;
                
				return o;
			}
			
			fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
			{
				fixed4 diffuseTex = tex2D(_MainTex, i.uv);
				//discards the current pixel if the specified value is less than zero.
                clip(diffuseTex.a > 0 ? 1 : -1);
                
				//Tiene valores de 0 a 1 y los pasamos a valores de -1 a 1
                float3 normalTex = normalize(tex2D(_NormalTex, i.uv) * 2 - 1);
				//Por si se rota el objeto
                normalTex.z *= facing;
				//Multiplicando los normal tang con los colores rgb nos indica que porcentaje se esta utilizando en el normal map
				//Red Tangent ( iquierda derecha )
				//Green Bitangent (Arriba y abajo )
				//Blue Normal (Adelante y atras)
				float3 N = normalize(i.wTangent) * normalTex.r + normalize(i.wBitangent) * normalTex.g + normalize(i.wNormal) * normalTex.b;
                
				//Dependiendo del resultado como le de la luz al normal map es como va a actuar
                half3 toonLight;
				if(_BackColor==0)
					toonLight = saturate(dot(N, _WorldSpaceLightPos0)) > _MinColorImpact.x ? _LightColor0 : (unity_AmbientSky);
				else
					toonLight = saturate(dot(N, _WorldSpaceLightPos0)) > _MinColorImpact.x ? _LightColor0 : _Color;

				half3 diffuse = diffuseTex * (toonLight);
                
                return half4(diffuse,0);
			}
			ENDCG
		}
	}
}
