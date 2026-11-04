Shader "AAH/TreatmentOutline"
{
 Properties { _Color ("Outline Color", Color) = (0.15,0.75,1,1) _Width ("World Width", Float) = 0.06 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Cull Front ZWrite Off ZTest LEqual
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Color;
   float _Width;
   CBUFFER_END
   struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
   struct Varyings { float4 positionCS : SV_POSITION; };
   Varyings Vert(Attributes input)
   {
    Varyings output;
    float3 position = TransformObjectToWorld(input.positionOS.xyz);
    position += TransformObjectToWorldNormal(input.normalOS) * _Width;
    output.positionCS = TransformWorldToHClip(position);
    return output;
   }
   half4 Frag(Varyings input) : SV_Target { return _Color; }
   ENDHLSL
  }
 }
}
