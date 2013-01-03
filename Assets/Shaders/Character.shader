Shader "Custom/Character" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_FresnelPower ("Fresnel Power", Float) = 1
		_FresnelContrast ("Fresnel Contrast", Float) = 1
		_FresnelTint ("Fresnel Tint", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		float _FresnelPower;
		float _FresnelContrast;
		float4 _FresnelTint;

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
			float3 worldNormal;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			float facing = saturate(1.0-max(dot(normalize(IN.viewDir.xyz), normalize(IN.worldNormal)), 0.0));
			facing = pow(facing,_FresnelContrast);
			float3 fresnelAdd = _FresnelTint.rgb*facing*_FresnelPower;
			fresnelAdd *= c.rgb;
			o.Albedo = c.rgb+fresnelAdd;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
