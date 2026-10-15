// ==============================================================================
// Space_v7 - UberShield HLSL Shader
// 3D Spherical/Dome Energy Shield with Edge Fresnel Glow, Panning Fractal
// Electric Veins, Hex Shield Grid, and DamageType Hit Pulse Flashes.
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
// Shield Parameters
// -----------------------------------------------------------------------------
float Time = 0.0f;
float FresnelPower = 3.0f;
float4 ShieldColor = float4(0.1f, 0.65f, 1.0f, 0.85f);
float4 HitColor = float4(1.0f, 0.35f, 0.1f, 1.0f);
float HitPulse = 0.0f;
float HitIntensity = 1.0f;
float NoiseScale = 8.0f;
float NoiseSpeed = 1.6f;

// -----------------------------------------------------------------------------
// Textures & Samplers
// -----------------------------------------------------------------------------
Texture2D ShieldTexture;
sampler2D ShieldSampler = sampler_state
{
    Texture = <ShieldTexture>;
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

    // Slight vertex pulse on hit impact
    float hitDisplacement = HitPulse * 0.03f * sin(input.Position.y * 10.0f + Time * 20.0f);
    float4 localPos = input.Position + float4(input.Normal * hitDisplacement, 0.0f);

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
    // 1. View Vector & Normal for 3D Fresnel
    float3 viewDir = normalize(CameraPosition - input.WorldPos);
    float3 normal = normalize(input.WorldNormal);

    // Fresnel glow: transparent center, hyper-luminous silhouette edges
    float nDotV = saturate(dot(normal, viewDir));
    float fresnel = pow(1.0f - nDotV, FresnelPower);

    // 2. Dual Panning Electric Vein & Fractal Interference
    float2 uv1 = input.TexCoord * NoiseScale + float2(Time * NoiseSpeed * 0.25f, Time * NoiseSpeed * 0.18f);
    float2 uv2 = input.TexCoord * (NoiseScale * 1.6f) - float2(Time * NoiseSpeed * 0.15f, Time * NoiseSpeed * 0.28f);

    float wave1 = sin(uv1.x * 6.2831f + sin(uv1.y * 3.1415f + Time * 2.5f));
    float wave2 = cos(uv2.y * 6.2831f + cos(uv2.x * 3.1415f - Time * 1.8f));
    float electric = pow(abs(wave1 * wave2), 4.5f) * 1.4f;

    // 3. Hexagonal Forcefield Lattice Grid
    float hexPattern = abs(sin(input.TexCoord.x * 24.0f + input.TexCoord.y * 12.0f + Time * 0.8f));
    float energyLattice = smoothstep(0.86f, 0.98f, hexPattern) * 0.45f;

    // 4. Hit Flash Impact Modulation
    float hitFactor = saturate(HitPulse * HitIntensity);
    float hitRipples = sin(length(input.TexCoord - 0.5f) * 40.0f - Time * 25.0f) * 0.5f + 0.5f;
    float hitGlow = hitFactor * (0.6f + 0.4f * hitRipples);

    // 5. Composite Colors
    float3 baseShield = ShieldColor.rgb * (fresnel * 1.8f + electric * 0.9f + energyLattice * 0.7f + 0.1f);
    float3 activeColor = lerp(baseShield, HitColor.rgb * (2.2f + fresnel * 1.5f), hitGlow);

    if (UseTexture)
    {
        float4 texColor = tex2D(ShieldSampler, input.TexCoord);
        activeColor *= texColor.rgb;
    }

    // Alpha calculation: hyper-luminous edges with translucent core
    float totalAlpha = (fresnel * 0.95f + electric * 0.55f + energyLattice * 0.35f + hitGlow * 0.85f + 0.06f) * ShieldColor.a;
    totalAlpha = saturate(totalAlpha);

    return float4(activeColor * totalAlpha, totalAlpha);
}

// -----------------------------------------------------------------------------
// Techniques
// -----------------------------------------------------------------------------
technique ShieldTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}

technique UberShield
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
