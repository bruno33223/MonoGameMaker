// ==============================================================================
// MonoGameMaker RTS - Unit GPU Instancing Shader
// Suporta 1000+ unidades com UV Atlas Animation e Player Team Color Replacement
// ==============================================================================

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Matriz View-Projection da Câmera (2D ou 3D)
float4x4 ViewProjection;

// Atlas de Sprites das Unidades
Texture2D UnitAtlasTexture;
sampler2D AtlasSampler = sampler_state
{
    Texture = <UnitAtlasTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Textura de Máscara de Facção (Canal R define onde pintar a cor do jogador)
Texture2D TeamMaskTexture;
sampler2D TeamMaskSampler = sampler_state
{
    Texture = <TeamMaskTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Stream 0: Vértices do Quad (tipo canônico VertexPositionTexture)
struct VSInputQuad
{
    float3 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

// Stream 1: Dados por Instância (UnitInstanceData 64 bytes)
struct VSInputInstance
{
    float4 TransformData : TEXCOORD1; // X, Y = WorldPos, Z = Rotation, W = Scale
    float4 AtlasUVRect   : TEXCOORD2; // X = UMin, Y = VMin, Z = UWidth, W = VHeight
    float4 TeamColor     : COLOR1;     // Cor da facção (R, G, B, A)
    float4 VisualFlags   : TEXCOORD3; // X = IsSelected, Y = HealthRatio
};

struct VSOutput
{
    float4 Position     : SV_POSITION;
    float2 TexCoord     : TEXCOORD0;
    float4 TeamColor    : COLOR0;
    float4 VisualFlags  : TEXCOORD1;
};

VSOutput MainVS(VSInputQuad quad, VSInputInstance inst)
{
    VSOutput output;

    float posX = inst.TransformData.x;
    float posY = inst.TransformData.y;
    float angle = inst.TransformData.z;
    float scale = inst.TransformData.w;

    // Rotação 2D do vértice em torno da origem
    float cosA = cos(angle);
    float sinA = sin(angle);
    float2 localPos = float2(
        quad.Position.x * cosA - quad.Position.y * sinA,
        quad.Position.x * sinA + quad.Position.y * cosA
    ) * scale;

    float3 worldPos = float3(localPos.x + posX, localPos.y + posY, quad.Position.z);
    output.Position = mul(float4(worldPos, 1.0f), ViewProjection);

    // Mapeamento de UV para o sub-quadro do Atlas
    output.TexCoord = inst.AtlasUVRect.xy + (quad.TexCoord * inst.AtlasUVRect.zw);
    output.TeamColor = inst.TeamColor;
    output.VisualFlags = inst.VisualFlags;

    return output;
}

float4 MainPS(VSOutput input) : COLOR0
{
    float4 spriteColor = tex2D(AtlasSampler, input.TexCoord);

    // Alpha test para descarte de pixels transparentes
    if (spriteColor.a < 0.05f)
    {
        discard;
    }

    // Amostra máscara de uniforme
    float teamMask = tex2D(TeamMaskSampler, input.TexCoord).r;

    // Substitui tons pela cor do time do jogador onde a máscara for branca
    float3 finalRgb = lerp(spriteColor.rgb, spriteColor.rgb * input.TeamColor.rgb, teamMask);

    // Se selecionada, aplica um leve realce de brilho nas bordas
    if (input.VisualFlags.x > 0.5f)
    {
        finalRgb += float3(0.12f, 0.18f, 0.25f);
    }

    return float4(finalRgb, spriteColor.a);
}

technique UnitInstancingTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
