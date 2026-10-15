// ==============================================================================
// Space_v7 - Vortex HLSL Shader
// Spiral Portal / Black Hole Singularity with Center UV Twirl Distortion,
// Continuous Rotating Fractal Noise, Accretion Disk Glow, and Radial Edge Fade.
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
// Transformation Matrices
// -----------------------------------------------------------------------------
float4x4 World;
float4x4 View;
float4x4 Projection;

// -----------------------------------------------------------------------------
// Vortex Parameters
// -----------------------------------------------------------------------------
float Time = 0.0f;
float4 PortalColor = float4(0.7f, 0.2f, 1.0f, 1.0f);     // Deep cosmic violet
float4 SecondaryColor = float4(0.1f, 0.85f, 1.0f, 1.0f); // Radiant electric cyan
float4 CoreColor = float4(0.02f, 0.0f, 0.05f, 1.0f);     // Event horizon singularity
float TwistSpeed = 2.2f;
float TwistIntensity = 3.5f;
float AccretionGlow = 2.2f;

// -----------------------------------------------------------------------------
// Textures & Samplers
// -----------------------------------------------------------------------------
Texture2D VortexTexture;
sampler2D VortexSampler = sampler_state
{
    Texture = <VortexTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
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
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// -----------------------------------------------------------------------------
// Vertex Shader
// -----------------------------------------------------------------------------
VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    float4 worldPos = mul(input.Position, World);
    float4 viewPos = mul(worldPos, View);
    output.Position = mul(viewPos, Projection);

    output.WorldPos = worldPos.xyz;
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}

// -----------------------------------------------------------------------------
// Pixel Shader
// -----------------------------------------------------------------------------
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    // 1. Center UV coordinates to (0, 0)
    float2 uvCent = input.TexCoord - float2(0.5f, 0.5f);
    float radius = length(uvCent);

    // Discard outer corners beyond circular radius
    if (radius > 0.5f)
    {
        return float4(0.0f, 0.0f, 0.0f, 0.0f);
    }

    // 2. UV Twirl / Angular Rotation Inversely Proportional to Distance
    float baseAngle = atan2(uvCent.y, uvCent.x);
    float twistOffset = (TwistIntensity / (radius + 0.08f)) + Time * TwistSpeed;
    float angle = baseAngle + twistOffset;

    // Rotated spiral UV coordinates
    float2 rotatedUV = float2(cos(angle), sin(angle)) * radius + float2(0.5f, 0.5f);

    // 3. Multi-Arm Spiral Arms & Rotating Fractal Interference
    float arms = 3.0f;
    float spiral1 = sin(angle * arms - (1.5f / (radius + 0.06f)) + Time * TwistSpeed * 2.0f);
    float spiral2 = cos(angle * (arms * 2.0f) + (2.0f / (radius + 0.1f)) - Time * TwistSpeed * 1.5f);
    float spiralFractal = saturate(spiral1 * 0.55f + spiral2 * 0.45f + 0.35f);

    // Fine swirling energy bands
    float energyBands = sin(radius * 35.0f - Time * (TwistSpeed * 4.0f) + angle * 2.0f) * 0.5f + 0.5f;

    // 4. Accretion Disk Ring & Singularity Core (Event Horizon)
    // Accretion disk is hyper-luminous between r=0.12 and r=0.28
    float ringDist = abs(radius - 0.18f);
    float ringGlow = exp(-ringDist * 22.0f) * AccretionGlow;

    // Dark singularity core at the very center (r < 0.10)
    float coreMask = smoothstep(0.06f, 0.14f, radius);

    // Soft outer edge fade out (r between 0.36 and 0.50)
    float outerFade = smoothstep(0.50f, 0.36f, radius);

    // 5. Color Composite
    float3 armColor = lerp(PortalColor.rgb, SecondaryColor.rgb, spiralFractal);
    float3 accretionColor = SecondaryColor.rgb * ringGlow * 1.8f;
    float3 combinedRgb = (armColor * (spiralFractal * 0.8f + energyBands * 0.4f) + accretionColor) * coreMask;
    
    // Subtle dark void tint at center
    combinedRgb += CoreColor.rgb * (1.0f - coreMask);

    if (UseTexture)
    {
        float4 sampledTex = tex2D(VortexSampler, rotatedUV);
        combinedRgb *= sampledTex.rgb;
    }

    // Alpha channel
    float alpha = outerFade * saturate((spiralFractal * 0.85f + ringGlow * 1.2f + (1.0f - coreMask) * 0.9f) * PortalColor.a * input.Color.a);

    return float4(combinedRgb * alpha, alpha);
}

// -----------------------------------------------------------------------------
// Techniques
// -----------------------------------------------------------------------------
technique VortexTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}

technique VortexEffect
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
