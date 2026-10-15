// ==============================================================================
// Space_v7 - Pixel Art Post-Processing & Sobel Outlines HLSL Shader (Dead Cells Style)
// Features:
// 1. Sobel edge detection for crisp, dark actor and geometric silhouettes.
// 2. High-fidelity color passthrough or optional retro quantization steps.
// 3. Gamma preservation for rich emissive gradients and normal-mapped lighting.
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
float2 TexelSize = float2(1.0f / 640.0f, 1.0f / 360.0f);
float OutlineThickness = 1.0f;
float OutlineThreshold = 0.10f;
float OutlineIntensity = 0.80f;
float ColorSteps = 256.0f;       // 256.0 = continuous modern color gradients (Dead Cells aesthetic)
float Gamma = 1.0f;

// -----------------------------------------------------------------------------
// Textures & Samplers
// -----------------------------------------------------------------------------
Texture2D SpriteTexture;
sampler2D SpriteSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

// -----------------------------------------------------------------------------
// Vertex Shader (Fullscreen Quad / SpriteBatch)
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
    float4 centerCol = tex2D(SpriteSampler, input.TexCoord) * input.Color;

    // Fast exit for transparent pixels
    if (centerCol.a <= 0.001f)
    {
        return centerCol;
    }

    // --- 1. Sobel Edge Detection for Silhouette Outlining ---
    float2 tx = TexelSize * OutlineThickness;

    float4 cTL = tex2D(SpriteSampler, input.TexCoord + float2(-tx.x, -tx.y));
    float4 cTC = tex2D(SpriteSampler, input.TexCoord + float2( 0.0f, -tx.y));
    float4 cTR = tex2D(SpriteSampler, input.TexCoord + float2( tx.x, -tx.y));

    float4 cML = tex2D(SpriteSampler, input.TexCoord + float2(-tx.x,  0.0f));
    float4 cMR = tex2D(SpriteSampler, input.TexCoord + float2( tx.x,  0.0f));

    float4 cBL = tex2D(SpriteSampler, input.TexCoord + float2(-tx.x,  tx.y));
    float4 cBC = tex2D(SpriteSampler, input.TexCoord + float2( 0.0f,  tx.y));
    float4 cBR = tex2D(SpriteSampler, input.TexCoord + float2( tx.x,  tx.y));

    // Weighted luminance + alpha for robust edge boundaries
    float lTL = dot(cTL.rgb, float3(0.299f, 0.587f, 0.114f)) + cTL.a * 1.5f;
    float lTC = dot(cTC.rgb, float3(0.299f, 0.587f, 0.114f)) + cTC.a * 1.5f;
    float lTR = dot(cTR.rgb, float3(0.299f, 0.587f, 0.114f)) + cTR.a * 1.5f;

    float lML = dot(cML.rgb, float3(0.299f, 0.587f, 0.114f)) + cML.a * 1.5f;
    float lMR = dot(cMR.rgb, float3(0.299f, 0.587f, 0.114f)) + cMR.a * 1.5f;

    float lBL = dot(cBL.rgb, float3(0.299f, 0.587f, 0.114f)) + cBL.a * 1.5f;
    float lBC = dot(cBC.rgb, float3(0.299f, 0.587f, 0.114f)) + cBC.a * 1.5f;
    float lBR = dot(cBR.rgb, float3(0.299f, 0.587f, 0.114f)) + cBR.a * 1.5f;

    float gx = (lTR + 2.0f * lMR + lBR) - (lTL + 2.0f * lML + lBL);
    float gy = (lBL + 2.0f * lBC + lBR) - (lTL + 2.0f * lTC + lTR);
    float edgeMagnitude = sqrt(gx * gx + gy * gy);

    // Compute outline factor
    float outline = smoothstep(OutlineThreshold, OutlineThreshold + 0.25f, edgeMagnitude) * OutlineIntensity;

    // Apply dark outline to RGB
    float3 rgb = centerCol.rgb / centerCol.a;
    rgb = lerp(rgb, rgb * 0.15f, outline);

    // --- 2. Optional Color Quantization (Bypassed when ColorSteps >= 128 for Dead Cells gradients) ---
    if (ColorSteps < 128.0f)
    {
        if (Gamma != 1.0f && Gamma > 0.01f)
        {
            rgb = pow(max(rgb, 0.0f), 1.0f / Gamma);
        }

        float steps = max(2.0f, ColorSteps);
        float interval = steps - 1.0f;
        rgb = floor(rgb * interval + 0.5f) / interval;
        rgb = clamp(rgb, 0.0f, 1.0f);

        if (Gamma != 1.0f && Gamma > 0.01f)
        {
            rgb = pow(rgb, Gamma);
        }
    }

    return float4(rgb * centerCol.a, centerCol.a);
}

technique ColorQuantization
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
