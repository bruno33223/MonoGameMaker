// ==============================================================================
// MonoGameMaker Runtime - Bloom & ACES Filmic Tone Mapping Shader
// Unreal-style multi-pass bloom extraction, separable Gaussian blur, and
// ACES Filmic Tone Mapping composite.
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
// Parameters
// -----------------------------------------------------------------------------
float BloomThreshold = 0.75f;
float BloomIntensity = 1.2f;
float BloomRadius = 1.0f;
float Exposure = 1.0f;
float2 TexelSize = float2(1.0f / 1280.0f, 1.0f / 720.0f);

// -----------------------------------------------------------------------------
// Textures & Samplers
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

Texture2D BloomTexture;
sampler2D BloomSampler = sampler_state
{
    Texture = <BloomTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
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
// Pixel Shader 1: Bright Pass Filter (Smooth-Knee Threshold Extraction)
// -----------------------------------------------------------------------------
float4 BrightPassPS(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(SceneSampler, input.TexCoord) * input.Color;
    
    // Calculate standard Rec.709 relative luminance
    float luminance = dot(color.rgb, float3(0.2126f, 0.7152f, 0.0722f));

    // Smooth-knee curve to avoid hard cutoffs and flickering
    float knee = max(BloomThreshold * 0.5f, 0.0001f);
    float soft = luminance - BloomThreshold + knee;
    soft = clamp(soft, 0.0f, 2.0f * knee);
    soft = (soft * soft) / (4.0f * knee + 0.00001f);

    float contribution = max(soft, luminance - BloomThreshold);
    contribution /= max(luminance, 0.00001f);

    return float4(color.rgb * contribution, color.a);
}

// -----------------------------------------------------------------------------
// Pixel Shader 2: Separable 9-Tap Gaussian Blur (Horizontal & Vertical)
// -----------------------------------------------------------------------------
static const float GaussianWeights[5] = { 0.2270270270f, 0.1945945946f, 0.1216216216f, 0.0540540541f, 0.0162162162f };

float4 BlurHorizontalPS(VertexShaderOutput input) : COLOR0
{
    float2 offset = float2(TexelSize.x * BloomRadius, 0.0f);
    float4 result = tex2D(SceneSampler, input.TexCoord) * GaussianWeights[0];

    for (int i = 1; i < 5; ++i)
    {
        result += tex2D(SceneSampler, input.TexCoord + offset * (float)i) * GaussianWeights[i];
        result += tex2D(SceneSampler, input.TexCoord - offset * (float)i) * GaussianWeights[i];
    }

    return result * input.Color;
}

float4 BlurVerticalPS(VertexShaderOutput input) : COLOR0
{
    float2 offset = float2(0.0f, TexelSize.y * BloomRadius);
    float4 result = tex2D(SceneSampler, input.TexCoord) * GaussianWeights[0];

    for (int i = 1; i < 5; ++i)
    {
        result += tex2D(SceneSampler, input.TexCoord + offset * (float)i) * GaussianWeights[i];
        result += tex2D(SceneSampler, input.TexCoord - offset * (float)i) * GaussianWeights[i];
    }

    return result * input.Color;
}

// -----------------------------------------------------------------------------
// Pixel Shader 3: Tone Mapping & Composite (ACES Filmic Tone Mapping)
// -----------------------------------------------------------------------------
float3 ACESFilm(float3 x)
{
    // Narkowicz 2015, "ACES Filmic Tone Mapping Curve"
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

float4 CompositePS(VertexShaderOutput input) : COLOR0
{
    float4 sceneColor = tex2D(SceneSampler, input.TexCoord);
    float4 bloomColor = tex2D(BloomSampler, input.TexCoord);

    // Combine original HDR scene and blurred bloom buffer
    float3 hdrColor = sceneColor.rgb + (bloomColor.rgb * BloomIntensity);

    // Apply exposure compensation
    hdrColor *= Exposure;

    // ACES Filmic Tone Mapping Curve
    float3 ldrColor = ACESFilm(hdrColor);

    return float4(ldrColor, sceneColor.a * input.Color.a);
}

// -----------------------------------------------------------------------------
// Techniques
// -----------------------------------------------------------------------------
technique BrightPassTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL BrightPassPS();
    }
}

technique BlurHorizontalTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL BlurHorizontalPS();
    }
}

technique BlurVerticalTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL BlurVerticalPS();
    }
}

technique CompositeTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL CompositePS();
    }
}

technique BloomEffect
{
    pass BrightPass
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL BrightPassPS();
    }
    pass BlurH
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL BlurHorizontalPS();
    }
    pass BlurV
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL BlurVerticalPS();
    }
    pass Composite
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL CompositePS();
    }
}
