Shader "SpriteEngine/Sprite_AlphaBlend" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Saturation ("Saturation", Float) = 1
		_Brightness ("Brightness", Float) = 1
		_Contrast ("Contrast", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert alpha

		sampler2D _MainTex;
		float4 _Color;
		float _Saturation;
		float _Brightness;
		float _Contrast;

		struct Input {
			float2 uv_MainTex;
			float4 color:COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			float avgRGB = (c.r+c.g+c.b)/3.0f;
			
			float3 outColor = lerp((float3)avgRGB,c,_Saturation);
			outColor = lerp(float3(0.5,0.5,0.5),outColor,_Contrast);
			outColor *= _Color.rgb;
			outColor *= IN.color.rgb;
			outColor *= _Brightness;
			
			o.Emission = outColor;
			o.Alpha = c.a*_Color.a*IN.color.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
