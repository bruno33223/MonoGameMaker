// ==============================================================================
// MonoGameMaker Runtime - PBR Surface Shader (2.5D / 3D)
// Physically Based Rendering shader supporting Albedo, Normal Maps, Emissive,
// Alpha Testing, Directional + Point Lighting, and Schlick-Fresnel Specular.
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
float4x4 WorldInverseTranspose;
float3 CameraPosition;

// -----------------------------------------------------------------------------
// Lighting Parameters
// -----------------------------------------------------------------------------
float3 LightDirection;          // Direction pointing toward the directional light
float4 LightColor;              // Directional light color & intensity (HDR RGB)
float4 AmbientColor;            // Ambient light color & base irradiance

float3 PointLightPosition;      // World-space point light center
float4 PointLightColor;         // Point light color & intensity
float PointLightRadius;         // Maximum influence distance (falloff radius)
float PointLightIntensity;      // Point light multiplier

// -----------------------------------------------------------------------------
// Material Properties
// -----------------------------------------------------------------------------
float Roughness = 0.5f;
float Metallic = 0.0f;
float EmissiveIntensity = 1.0f;
float4 EmissiveColor = float4(0.0f, 0.0f, 0.0f, 1.0f);
float AlphaCutoff = 0.5f;

// -----------------------------------------------------------------------------
// Textures & Samplers
// -----------------------------------------------------------------------------
Texture2D AlbedoTexture;
sampler2D AlbedoSampler = sampler_state
{
    Texture = <AlbedoTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

Texture2D NormalMapTexture;
sampler2D NormalSampler = sampler_state
{
    Texture = <NormalMapTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

Texture2D EmissiveTexture;
sampler2D EmissiveSampler = sampler_state
{
    Texture = <EmissiveTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

// -----------------------------------------------------------------------------
// Vertex Shader Structures
// -----------------------------------------------------------------------------
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float3 Tangent : TANGENT0;
    float3 Binormal : BINORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 WorldPos : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float3 Tangent : TEXCOORD2;
    float3 Bitangent : TEXCOORD3;
    float2 TexCoord : TEXCOORD4;
};

// -----------------------------------------------------------------------------
// Vertex Shader
// -----------------------------------------------------------------------------
VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    output.WorldPos = worldPosition;

    // Transform vectors to world space
    output.Normal = normalize(mul(input.Normal, (float3x3)WorldInverseTranspose));
    output.Tangent = normalize(mul(input.Tangent, (float3x3)World));
    output.Bitangent = normalize(mul(input.Binormal, (float3x3)World));
    output.TexCoord = input.TexCoord;

    return output;
}

// -----------------------------------------------------------------------------
// Pixel Shader
// -----------------------------------------------------------------------------
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    // Sample Albedo & perform Alpha Cutoff test
    float4 albedo = tex2D(AlbedoSampler, input.TexCoord);
    clip(albedo.a - AlphaCutoff);

    // Tangent Space to World Space Normal Mapping
    float3 normalSample = tex2D(NormalSampler, input.TexCoord).rgb;
    float3 normalTangent = normalSample * 2.0f - 1.0f;

    float3 N = normalize(input.Normal);
    float3 T = normalize(input.Tangent);
    float3 B = normalize(input.Bitangent);

    // Gram-Schmidt orthogonalization in case of interpolation distortion
    T = normalize(T - dot(T, N) * N);
    B = cross(N, T);

    float3x3 TBN = float3x3(T, B, N);
    float3 worldNormal = normalize(mul(normalTangent, TBN));

    // View direction
    float3 V = normalize(CameraPosition - input.WorldPos.xyz);

    // Base Reflectivity F0 (Dielectrics ~ 0.04, Conductors/Metals use Albedo)
    float3 F0 = lerp(float3(0.04f, 0.04f, 0.04f), albedo.rgb, saturate(Metallic));

    // Roughness to Blinn-Phong Shininess / Microfacet conversion
    float clampedRoughness = max(Roughness, 0.04f);
    float alphaRoughness = clampedRoughness * clampedRoughness;
    float shininess = max(2.0f / (alphaRoughness * alphaRoughness + 0.0001f) - 2.0f, 1.0f);

    // -------------------------------------------------------------------------
    // Directional Lighting
    // -------------------------------------------------------------------------
    float3 L_dir = normalize(-LightDirection);
    float3 H_dir = normalize(V + L_dir);

    float NdotL_dir = max(dot(worldNormal, L_dir), 0.0f);
    float NdotH_dir = max(dot(worldNormal, H_dir), 0.0f);
    float VdotH_dir = max(dot(V, H_dir), 0.0f);

    // Schlick Fresnel approximation
    float3 F_dir = F0 + (1.0f - F0) * pow(saturate(1.0f - VdotH_dir), 5.0f);

    // Specular factor
    float spec_dir = pow(NdotH_dir, shininess) * ((shininess + 8.0f) / (8.0f * 3.14159265f));
    float3 specularDir = F_dir * spec_dir * LightColor.rgb * NdotL_dir;

    // Diffuse factor (energy conservation: 1 - F and 1 - Metallic)
    float3 kD_dir = (1.0f - F_dir) * (1.0f - saturate(Metallic));
    float3 diffuseDir = kD_dir * albedo.rgb * LightColor.rgb * NdotL_dir;

    // -------------------------------------------------------------------------
    // Point Lighting
    // -------------------------------------------------------------------------
    float3 pointVec = PointLightPosition - input.WorldPos.xyz;
    float pointDist = length(pointVec);
    float3 L_point = normalize(pointVec);
    float3 H_point = normalize(V + L_point);

    // Distance Attenuation (smooth windowed falloff)
    float radius = max(PointLightRadius, 0.001f);
    float distFactor = saturate(1.0f - (pointDist / radius));
    float pointAtten = distFactor * distFactor * PointLightIntensity;

    float NdotL_point = max(dot(worldNormal, L_point), 0.0f);
    float NdotH_point = max(dot(worldNormal, H_point), 0.0f);
    float VdotH_point = max(dot(V, H_point), 0.0f);

    float3 F_point = F0 + (1.0f - F0) * pow(saturate(1.0f - VdotH_point), 5.0f);
    float spec_point = pow(NdotH_point, shininess) * ((shininess + 8.0f) / (8.0f * 3.14159265f));
    float3 specularPoint = F_point * spec_point * PointLightColor.rgb * NdotL_point * pointAtten;

    float3 kD_point = (1.0f - F_point) * (1.0f - saturate(Metallic));
    float3 diffusePoint = kD_point * albedo.rgb * PointLightColor.rgb * NdotL_point * pointAtten;

    // -------------------------------------------------------------------------
    // Ambient & Emissive Terms
    // -------------------------------------------------------------------------
    float3 ambient = AmbientColor.rgb * albedo.rgb;

    float4 emissiveTex = tex2D(EmissiveSampler, input.TexCoord);
    float3 emissive = emissiveTex.rgb * EmissiveColor.rgb * EmissiveIntensity;

    // Combine HDR Color (Leaves dynamic range unclamped for post-process Bloom & Tone Mapping)
    float3 finalHdrColor = ambient + diffuseDir + specularDir + diffusePoint + specularPoint + emissive;

    return float4(finalHdrColor, albedo.a);
}

// -----------------------------------------------------------------------------
// Techniques
// -----------------------------------------------------------------------------
technique PbrTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
