sampler uImage0 : register(s0);
float3 uColor;
float3 uSecondaryColor; 
float uOpacity; //Opacity is used for alpha quantization count. Set to 0 to disable alpha quantization
float uSaturation; //Saturation is used for color quantization count. Total color number is uSaturation^3. Set to 0 to disable color quantization
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float4 uShaderSpecificData;

float4 QuantizeRGB(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float4 col = tex2D(uImage0, coords);
	if (uOpacity > 0)
		col.a = floor(col.a * uOpacity) / uOpacity;
	if (uSaturation > 0)
		col.rgb = floor(col.rgb * uSaturation) / uSaturation;
	return col;
}

technique Technique1
{
	pass QuantizePass
	{
		PixelShader = compile ps_2_0 QuantizeRGB();
	}
}