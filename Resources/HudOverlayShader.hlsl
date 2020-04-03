Texture2D g_Texture : register(t0);
SamplerState g_Sampler : register(s0);

BlendState SrcAlphaBlendingAdd {
  BlendEnable[0] = TRUE;
  SrcBlend = SRC_ALPHA;
  DestBlend = INV_SRC_ALPHA;
  BlendOp = ADD;
  SrcBlendAlpha = ZERO;
  DestBlendAlpha = ZERO;
  BlendOpAlpha = ADD;
  RenderTargetWriteMask[0] = 0x0F;
};

struct VertexShaderInput {
  float4 vPosition : POSITION;
  float2 vTexCoord : TEXCOORD;
};

struct PixelShaderInput {
  float4 vPosition : SV_POSITION;
  float2 vTexCoord : TEXCOORD;
};

PixelShaderInput VertexShaderMain(VertexShaderInput input) {
  PixelShaderInput output = (PixelShaderInput)0;
  output.vPosition = input.vPosition;
  output.vTexCoord = input.vTexCoord + float4(0.5, 0.5, 0.5, 0.5);

  return output;
}

float4 PixelShaderMain(PixelShaderInput input) : SV_TARGET {
  return g_Texture.Sample(g_Sampler, input.vTexCoord);
}

technique10 Render {
  pass P0 {
    SetGeometryShader(NULL);
    SetVertexShader(CompileShader(vs_4_0, VertexShaderMain()));
    SetPixelShader(CompileShader(ps_4_0, PixelShaderMain()));
    SetBlendState(SrcAlphaBlendingAdd, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xFFFFFFFF);
  }
};