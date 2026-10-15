// ==============================================================================
// MonoGameMaker RTS - Dynamic Fog of War Composition Shader
// Interpolação suave de bordas via Hermite Smoothstep e tonalização de penumbra
// ==============================================================================

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Textura do mundo renderizado (albedo da cena do jogo)
Texture2D SceneTexture;
sampler2D SceneSampler = sampler_state
{
    Texture = <SceneTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Textura da máscara da névoa (gerada dinamicamente pelo FogOfWarGrid)
Texture2D FogMaskTexture;
sampler2D FogMaskSampler = sampler_state
{
    Texture = <FogMaskTexture>;
    MinFilter = Linear; // Filtro bilinear de hardware suaviza as células
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Parâmetros de iluminação e controle
float ExploredDarkness = 0.40f;      // Escurecimento na área explorada (40% do brilho)
float ExploredDesaturation = 0.65f;  // Fator de desaturação na área explorada
float4 FogColor = float4(0.02f, 0.03f, 0.06f, 1.0f); // Azul escuro profundo (em vez de preto lavado)

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

VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = input.Position;
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 sceneColor = tex2D(SceneSampler, input.TexCoord);
    float fogValue = tex2D(FogMaskSampler, input.TexCoord).a; // 0.0 (Unexplored), 0.5 (Explored), 1.0 (Visible)

    // Curva Hermite para suavizar as fronteiras entre estados
    // Transição 1: Não explorado (0.0) -> Explorado (0.5)
    float unexploredToExplored = smoothstep(0.05f, 0.45f, fogValue);

    // Transição 2: Explorado (0.5) -> Visível (1.0)
    float exploredToVisible = smoothstep(0.55f, 0.95f, fogValue);

    // 1. Caso Breu Absoluto (Totalmente não explorado)
    if (fogValue <= 0.02f)
    {
        return FogColor;
    }

    // 2. Cálculo da Cor Desaturada para áreas em penumbra (Explored)
    float gray = dot(sceneColor.rgb, float3(0.299f, 0.587f, 0.114f));
    float3 desaturatedColor = lerp(sceneColor.rgb, float3(gray, gray, gray), ExploredDesaturation) * ExploredDarkness;
    float4 shroudedColor = float4(desaturatedColor, sceneColor.a);

    // 3. Mistura entre Breu e Penumbra
    float4 baseColor = lerp(FogColor, shroudedColor, unexploredToExplored);

    // 4. Mistura final entre Penumbra e Luz Plena (Visible)
    float4 finalColor = lerp(baseColor, sceneColor, exploredToVisible);

    return finalColor;
}

technique FogOfWarComposite
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
