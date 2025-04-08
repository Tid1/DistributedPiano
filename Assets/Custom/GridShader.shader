Shader "Unlit/GridShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        numberOctaves ("Nr octaves", int) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            int numberOctaves;

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float widthPerOctave = 1.0 / (float)numberOctaves;
                float gridSize = 0.002;
                // Line between octaves
                for(int index = 0; index<=numberOctaves;index++){
                    if(i.uv.x > index*widthPerOctave-gridSize/2 && i.uv.x < index*widthPerOctave + gridSize/2 ){
                        return float4(0.4,0.4,0.4,1);
                    } else if(i.uv.x > index*widthPerOctave-gridSize/4+widthPerOctave/7*3 && i.uv.x < index*widthPerOctave + gridSize/4+widthPerOctave/7*3 ){
                        return float4(0.3,0.3,0.3,1);
                    }  else if(i.uv.x > index*widthPerOctave-gridSize/4+widthPerOctave/7*1 && i.uv.x < index*widthPerOctave + gridSize/4+widthPerOctave/7*1 ){
                        return float4(0.25,0.25,0.25,1);
                    }    else if(i.uv.x > index*widthPerOctave-gridSize/4+widthPerOctave/7*2 && i.uv.x < index*widthPerOctave + gridSize/4+widthPerOctave/7*2 ){
                        return float4(0.25,0.25,0.25,1);
                    }     else if(i.uv.x > index*widthPerOctave-gridSize/4+widthPerOctave/7*4 && i.uv.x < index*widthPerOctave + gridSize/4+widthPerOctave/7*4 ){
                        return float4(0.25,0.25,0.25,1);
                    }  else if(i.uv.x > index*widthPerOctave-gridSize/4+widthPerOctave/7*5 && i.uv.x < index*widthPerOctave + gridSize/4+widthPerOctave/7*5 ){
                        return float4(0.25,0.25,0.25,1);
                    }  else if(i.uv.x > index*widthPerOctave-gridSize/4+widthPerOctave/7*6 && i.uv.x < index*widthPerOctave + gridSize/4+widthPerOctave/7*6 ){
                        return float4(0.25,0.25,0.25,1);
                    } 
                }


                return float4(0.2, 0.2, 0.2,1);
            }
            ENDCG
        }
    }
}
