
Shader "Unlit/PointCloud"
{
    Properties
    {
        //_MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        CGINCLUDE
        #define POINTCLOUD_CHUNK_SIZE 16

        #include "UnityCG.cginc"

        struct v2f
        {
            float4 position : SV_POSITION;
            half4 color : COLOR;
        };

        struct CloudVertex
        {
            uint2 posAndRgb;
        };

        float4 DecodeColor565FromHigh16Bits(uint2 data)
        {
            int rgbData = data.x & 0xffff;

            float r = ((rgbData >> 11) & 0x1F) << 3;
            float g = ((rgbData >> 5) & 0x3F) << 2;
            float b = (rgbData & 0x1F) << 3;
            return float4(r, g, b, 1.0f) * (1.0f / 255.0f);            
        }

        int3 DecodePositonFrom16Bits(uint2 data)
        {
            int3 pos;
            pos.x = data.y & 0xFFFF;
            pos.y = (data.x >> 16) & 0xFFFF;
            pos.z = (data.y >> 16) & 0xFFFF;
            return pos;
        }

        StructuredBuffer<CloudVertex> _Positions;
        float _PointSize;
        float3 _CellOrigin;

        float _ChunkSpaceZeroTo65535ToLocalMeters;

        v2f vert(uint id : SV_VertexID) 
        {
            uint vertexID = id / 4;
            v2f o;
            float3 pos = DecodePositonFrom16Bits(_Positions[vertexID].posAndRgb);
            pos = pos * _ChunkSpaceZeroTo65535ToLocalMeters;

            pos += _CellOrigin ;
            float4 col = DecodeColor565FromHigh16Bits(_Positions[vertexID].posAndRgb);

            col.rgb = GammaToLinearSpace(col.rgb);

            float4 clipPos = UnityObjectToClipPos(pos);

            int quadVtxIdx = id % 4;

            float pointSize = _PointSize;
            pointSize *= 1.3;
            float2 extent = abs(UNITY_MATRIX_P._11_22 * pointSize);            
            float2 offsets[4] = {
                float2(-0.5, -0.5),
                float2(0.5, -0.5),
                float2(0.5, 0.5),
                float2(-0.5, 0.5), // float2(-0.5, -0.5),
                // float2(0.5, 0.5),
                // float2(-0.5, 0.5)
            };
            float2 offset = offsets[quadVtxIdx];
   
            clipPos.xy += offset * extent;
    
            o.position = clipPos;

            o.color = col;   

            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            // sample the texture
            fixed4 col = i.color; 
            i.color;
            // apply fog
            return float4(col.rgb, 1);
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            ENDCG
        }
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}
