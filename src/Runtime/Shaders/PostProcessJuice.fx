// ==============================================================================
// Space_v7 - Post-Process Visual Juice & Tactical Feedback Shader
// Features:
// 1. Spatial Shockwaves (Multi-Epicenter Displacement & Refraction in Deep Space Vacuum).
// 2. Tactical Chromatic Aberration (Radial/Directional RGB Channel Separation on Stress Spikes).
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
// Uniform Parameters
// -----------------------------------------------------------------------------
float2 TexelSize = float2(1.0f / 1920.0f, 1.0f / 1080.0f);
float AspectRatio = 1.77778f; // 16:9

// Aberração Cromática Tática (0.0 = sem aberração, pico momentâneo = 0.015 a 0.05)
float TacticalAberrationAmount = 0.0f;
float TacticalAberrationAngle = 0.0f;

// Ondas de Choque (Máximo de 8 ondas ativas simultâneas)
// Formato por vetor float4:
// X: Center.x (UV), Y: Center.y (UV), Z: Radius (UV), W: Força Efetiva de Refração
float4 Shockwaves[8];
int ActiveShockwaveCount = 0;
float ShockwaveThickness = 0.045f;

// -----------------------------------------------------------------------------
// Texture Sampler
// -----------------------------------------------------------------------------
Texture2D SceneTexture;
sampler2D SceneSampler = sampler_state
{
    Texture = <SceneTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// -----------------------------------------------------------------------------
// Vertex Shader (Fullscreen Quad)
// -----------------------------------------------------------------------------
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = input.Position;
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    return output;
}

// -----------------------------------------------------------------------------
// Pixel Shader
// -----------------------------------------------------------------------------
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoord;
    float2 totalDisplacement = float2(0.0f, 0.0f);
    float totalChromaticFringe = 0.0f;

    // --- 1. Calcular Distorção de Refração de Ondas de Choque ---
    [unroll(8)]
    for (int i = 0; i < 8; i++)
    {
        if (i >= ActiveShockwaveCount) break;

        float4 sw = Shockwaves[i];
        float strength = sw.w;
        if (strength <= 0.0001f) continue;

        float2 center = sw.xy;
        float radius = sw.z;

        float2 diff = uv - center;
        diff.x *= AspectRatio; // Correção de aspect ratio para onda perfeitamente circular

        float dist = length(diff);
        float delta = abs(dist - radius);

        if (delta < ShockwaveThickness)
        {
            // Curva Hermite suave de onda senoidal para perfil de refração de crista
            float t = delta / ShockwaveThickness;
            float waveProfile = sin((1.0f - t) * 3.14159265f);

            float2 normDir = normalize(diff);
            normDir.x /= AspectRatio;

            // Vetor de refração radial empurrando pixels para fora
            float2 disp = normDir * (waveProfile * strength);
            totalDisplacement += disp;
            totalChromaticFringe += waveProfile * strength * 0.45f;
        }
    }

    float2 refractedUv = clamp(uv - totalDisplacement, 0.0f, 1.0f);

    // --- 2. Calcular Aberração Cromática Tática (RGB Channel Shift) ---
    float aberration = TacticalAberrationAmount + totalChromaticFringe;

    if (aberration > 0.0005f)
    {
        float2 screenCenter = float2(0.5f, 0.5f);
        float2 radialDir = refractedUv - screenCenter;
        radialDir.x *= AspectRatio;

        float distCenter = length(radialDir);
        if (distCenter > 0.001f) radialDir = normalize(radialDir);
        else radialDir = float2(1.0f, 0.0f);

        radialDir.x /= AspectRatio;

        // Deslocamento direcional e radial combinado
        float2 offset = radialDir * (aberration * (0.6f + distCenter * 0.8f));

        float r = tex2D(SceneSampler, clamp(refractedUv + offset, 0.0f, 1.0f)).r;
        float g = tex2D(SceneSampler, refractedUv).g;
        float b = tex2D(SceneSampler, clamp(refractedUv - offset, 0.0f, 1.0f)).b;
        float a = tex2D(SceneSampler, refractedUv).a;

        return float4(r, g, b, a) * input.Color;
    }

    return tex2D(SceneSampler, refractedUv) * input.Color;
}

technique PostProcessJuice
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
