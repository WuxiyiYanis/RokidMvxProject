Shader "Mvx2/MeshPerVertexColoredML" {
    SubShader {
    Pass {
        LOD 200
                 
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"
  
        struct VertexInput {
            float4 v : POSITION;
            float4 color: COLOR;
      
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
         
        struct VertexOutput {
            float4 pos : SV_POSITION;
            float4 col : COLOR;

            UNITY_VERTEX_OUTPUT_STEREO
        };
         
        VertexOutput vert(VertexInput v) {
         
            VertexOutput o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_OUTPUT(VertexOutput, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.pos = UnityObjectToClipPos(v.v);
            o.col = v.color;
             
            return o;
        }
         
        float4 frag(VertexOutput o) : COLOR {
            return o.col;
        }
 
        ENDCG
        } 

        // shadow caster rendering pass
        // using macros from UnityCG.cginc
        Pass
        {
            Tags {"LightMode"="ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f { 
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
 
}
