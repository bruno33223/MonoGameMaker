// ==============================================================================
// Space_v7 - Hologram HLSL Shader
// Sci-Fi Holographic Projection with Animated Scanlines, Fresnel/Rim Glow,
// Horizontal Glitch/Jitter, and Configurable Tint & Alpha.
// ==============================================================================

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// -----------------------------------------------------------------------------
// Transformation Matrices & Camera
// -----------------------------------------------------------------------------
float4x4 World;
float4x4 View;
float4x4 Projection;
float3 CameraPosition = float3(0.0f, 0.0f, 10.0f);

// -----------------------------------------------------------------------------
// Hologram Parameters
// -----------------------------------------------------------------------------
float Time = 0.0f;
float4 TintColor = float4(0.0f, 0.9f, 1.0f, 1.0f);
float Alpha = 0.85f;
float Frequency = 45.0f;
float Speed = 3.5f;
float RimPower = 2.5f;
float GlitchIntensity = 0.04f;
float GlitchFrequency = 18.0f;

// -----------------------------------------------------------------------------
// Textures & Samplers
// -----------------------------------------------------------------------------
Texture2D MainTexture;
sampler2D MainSampler = sampler_state
{
    Texture = <MainTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
bool UseTexture = false;

// -----------------------------------------------------------------------------
// Vertex Shader Input / Output
// -----------------------------------------------------------------------------
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 WorldPos : TEXCOORD1;
    float3 WorldNormal : NORMAL0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// -----------------------------------------------------------------------------
// Vertex Shader
// -----------------------------------------------------------------------------
VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    // Glitch horizontal jitter based on pseudo-random high-frequency square wave
    float glitchPhase = sin(input.Position.y * 25.0f + Time * GlitchFrequency);
    float glitchStep = step(0.88f, glitchPhase);
    float glitchOffset = sin(Time * 50.0f + input.Position.y * 10.0f) * glitchStep * GlitchIntensity;

    float4 localPos = input.Position;
    localPos.x += glitchOffset;

    float4 worldPos = mul(localPos, World);
    float4 viewPos = mul(worldPos, View);
    output.Position = mul(viewPos, Projection);

    output.WorldPos = worldPos.xyz;
    output.WorldNormal = normalize(mul(input.Normal, (float3x3)World));
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}

// -----------------------------------------------------------------------------
// Pixel Shader
// -----------------------------------------------------------------------------
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    // 1. View Direction and Surface Normal
    float3 viewDir = normalize(CameraPosition - input.WorldPos);
    float3 normal = normalize(input.WorldNormal);

    // 2. Animated Horizontal Scanlines
    float scanline = sin(input.WorldPos.y * Frequency + Time * Speed) * 0.5f + 0.5f;
    float microScanline = sin(input.WorldPos.y * (Frequency * 2.5f) - Time * (Speed * 1.5f)) * 0.5f + 0.5f;
    float combinedScan = scanline * 0.75f + microScanline * 0.25f;

    // 3. Fresnel / Rim Lighting (Glow on silhouette edges)
    float nDotV = saturate(dot(normal, viewDir));
    float rim = pow(1.0f - nDotV, RimPower);

    // 4. Subtle holographic high-speed flicker
    float flicker = 0.92f + 0.08f * sin(Time * 55.0f);

    // 5. Albedo Texture or Vertex Color Base
    float4 baseColor = TintColor * input.Color;
    if (UseTexture)
    {
        baseColor *= tex2D(MainSampler, input.TexCoord);
    }

    // 6. Hologram Color Composition
    float3 holoColor = baseColor.rgb * (0.35f + 0.65f * combinedScan);
    holoColor += TintColor.rgb * (rim * 1.6f);
    holoColor += float3(0.1f, 0.3f, 0.4f) * (1.0f - nDotV);

    float holoAlpha = (baseColor.a * (0.35f + 0.65f * combinedScan) + rim * 0.75f) * Alpha * flicker;

    return float4(holoColor * holoAlpha, saturate(holoAlpha));
}

// -----------------------------------------------------------------------------
// Techniques
// -----------------------------------------------------------------------------
technique HologramTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}

technique HologramEffect
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
