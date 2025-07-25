﻿#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

matrix WorldViewProjection;

float2 GridSize;
float CellSize;
float2 GroundTilesTextureSize;

float2 BlueNoiseTextureSize;
int BlueNoiseDitherChannel;
float2 BlueNoiseDitherOffset;
int BlueNoiseVariantChannel;
float2 BlueNoiseVariantOffset;
int BlueNoiseSineXChannel;
float2 BlueNoiseSineXOffset;
int BlueNoiseSineYChannel;
float2 BlueNoiseSineYOffset;
int BlurHalfSize;

float3 AmbientLightColor;
float4 Lights[8];
float2 LightPositions[8];
int NumLights;

float4 ShadowMapBounds;
float2 ShadowMapTileSize;
float ShadowStrength;

sampler2D GroundTilesTextureSampler = sampler_state
{
    Texture = <GroundTilesTexture>;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

sampler2D TerrainGridTextureSampler = sampler_state
{
    Texture = <TerrainGridTexture>;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

sampler2D BlueNoiseTextureSampler = sampler_state
{
    Texture = <BlueNoiseTexture>;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = WRAP;
    AddressV = WRAP;
};

sampler2D ShadowMapSampler = sampler_state
{
    Texture = <ShadowMap>;
    MinFilter = POINT;
    MagFilter = POINT;
    MipFilter = POINT;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 Tex : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 Tex : TEXCOORD0;
    float2 WorldPos : TEXCOORD1;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    output.Tex = input.Tex;
    output.WorldPos = input.Position.xy;

	return output;
}

float getRandomFromBlueNoise(float2 uv, float2 offset, int channel)
{
    float2 bluenoiseUV = int2(uv * GridSize * CellSize) / BlueNoiseTextureSize;
    float bluenoise = tex2D(BlueNoiseTextureSampler, bluenoiseUV + offset)[channel];
    return frac(bluenoise);
}

int getGroundType(float2 uv)
{
    float2 gridTexelSize = 1.0 / (GridSize * CellSize);
    uv = int2(uv / gridTexelSize) * gridTexelSize;
    
    float bluenoise1 = getRandomFromBlueNoise(uv / CellSize, BlueNoiseSineXOffset, BlueNoiseSineXChannel);
    float bluenoise2 = getRandomFromBlueNoise(uv / CellSize, BlueNoiseSineYOffset, BlueNoiseSineYChannel);

    float wavy_x = uv.x + (sin(uv.y * GridSize.x * 3.1415) / GridSize.x) * 0.25 * bluenoise1;
    float wavy_y = uv.y + (sin(uv.x * GridSize.y * 3.1415) / GridSize.y) * 0.25 * bluenoise2;

    int gridX = int(wavy_x * GridSize.x);
    int gridY = int(wavy_y * GridSize.y);

    float2 gridTextureUV = float2(gridX / GridSize.x, gridY / GridSize.y);
    int groundType = int(tex2D(TerrainGridTextureSampler, gridTextureUV).r / 256.0 + 0.5);
    
    return groundType;
}

float3 getGroundTypeColor(float2 uv, int groundType)
{
    float x = frac(uv.x * GridSize.x) * CellSize / GroundTilesTextureSize.x;
    float y = (1.0 - frac(uv.y * GridSize.y)) * CellSize / GroundTilesTextureSize.y;

    // TODO: proper variants
    int numVariants = 4;

    float bluenoise = getRandomFromBlueNoise(uv / CellSize, BlueNoiseVariantOffset, BlueNoiseVariantChannel);
    int variantIndex = int(bluenoise * numVariants);

    float groundTypeColumn = float(groundType) / (GroundTilesTextureSize.x / CellSize);
    float groundTypeVariantRow = float(variantIndex) / (GroundTilesTextureSize.y / CellSize);
    float3 color = tex2D(GroundTilesTextureSampler, float2(x, y) + float2(groundTypeColumn, groundTypeVariantRow)).rgb;

    return color;
}

float calcDistSqrd(float2 p1, float2 p2)
{
    return (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y);
}

float3 applyLights(float2 pos, float3 color)
{
    float3 light = AmbientLightColor;

    for (int i = 0; i < NumLights; i++)
    {
        float distSqrd = calcDistSqrd(pos, LightPositions[i]);
        float maxDistSqrd = Lights[i].a * Lights[i].a;
        float strength = clamp(1.0 - distSqrd / maxDistSqrd, 0.0, 1.0);
        light += Lights[i].rgb * strength;
    }

    return color * light;
}

float3 applyShadows(float2 pos, float3 color)
{
    if (pos.x < ShadowMapBounds.x || pos.x > ShadowMapBounds.z
        || pos.y < ShadowMapBounds.y || pos.y > ShadowMapBounds.w)
    {
        return color;
    }

    float2 shadowMapUV = (pos - ShadowMapBounds.xy) / ShadowMapTileSize;
    float shadow = tex2D(ShadowMapSampler, float2(shadowMapUV.x, 1.0 - shadowMapUV.y)).r;

    if (!shadow)
    {
        return color;
    }

    return color * (1.0 - ShadowStrength);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 gridTexelSize = 1.0 / (GridSize * CellSize);

    int blurFullSize = BlurHalfSize * 2 + 1;
    
    float bluenoise = getRandomFromBlueNoise(input.Tex, BlueNoiseDitherOffset, BlueNoiseDitherChannel);
    int pixelIndex = int(bluenoise * (blurFullSize * blurFullSize));

    int pixelX = pixelIndex / blurFullSize;
    int pixelY = pixelIndex % blurFullSize;

    int groundType = getGroundType(input.Tex + float2(pixelX - BlurHalfSize, pixelY - BlurHalfSize) * gridTexelSize);
    float3 color = getGroundTypeColor(input.Tex, groundType);

    float3 litColor = applyLights(input.WorldPos, color);
    float3 shadowedLitColor = applyShadows(input.WorldPos, litColor);
    return float4(shadowedLitColor, 1.0);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};