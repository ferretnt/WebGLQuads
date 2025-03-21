using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;    
using System.IO;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.IO.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using Unity.Mathematics;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct CloudVertexRepro
{
    [FieldOffset(0)]
    public ushort rgb565;
    [FieldOffset(2)]
    public ushort z;
    [FieldOffset(4)]
    public ushort x;
    [FieldOffset(6)]
    public ushort y;

    
    static ushort PackRgb565(byte r, byte g, byte b)
    {
        // Convert RGB to RGB565 format
        ushort r5 = (ushort)((r >> 3) & 0x1F); // 5 bits for red
        ushort g6 = (ushort)((g >> 2) & 0x3F); // 6 bits for green
        ushort b5 = (ushort)((b >> 3) & 0x1F); // 5 bits for blue

        // Pack RGB565 into a single ushort
        ushort rgb565 = (ushort)((r5 << 11) | (g6 << 5) | b5);

        return rgb565;
    }

    public int3 Pos
    {
        get
        {
            return new int3(x, y, z);
        }
    }

    public int3 Rgb
    {
        get
        {
            byte r5 = (byte)((rgb565 >> 11) & 0x1F);
            byte g6 = (byte)((rgb565 >> 5) & 0x3F);
            byte b5 = (byte)(rgb565 & 0x1F);

            byte r = (byte)(r5 << 3);
            byte g = (byte)(g6 << 2);
            byte b = (byte)(b5 << 3);

            return new int3(r, g, b);
        }
    }

    public static CloudVertexRepro FromData(int3 posZeroTo65535, byte r, byte g, byte b)
    {
        return new CloudVertexRepro()
        {
            x = (ushort)posZeroTo65535.x,
            y = (ushort)posZeroTo65535.y,
            z = (ushort)posZeroTo65535.z,
            rgb565 = PackRgb565(r, g, b)
        };
    }
}


public class PointRendererRepro : MonoBehaviour
{
    public GraphicsBuffer pointBuffer;

    public Bounds bounds;
    public static bool StaticsCreated = false;
    public static Material m_Material;
    
    public static int s_PositionsId = Shader.PropertyToID("_Positions");
    public static int s_PointSizeId = Shader.PropertyToID("_PointSize");
    public static int s_ObjectToWorldId = Shader.PropertyToID("_ObjectToWorld");


    const float Radius = 5.0f;
    public int PointCount = 2560000;
    const float pointSize = 0.01f;


    void SetupRendererConstants()
    {
        // PointCount = UnityEngine.Random.Range(2560000, 4*2560000);
        Debug.Log($"For gameObject {this.gameObject.name}, PointCount = {PointCount}");

        float3 size = Radius * Vector3.one;

        bounds = new Bounds(size / 2, size);    

        m_Material = new Material(Shader.Find("Unlit/PointCloudRepro"));

        const float ChunkSpaceZeroTo65535ToLocalMeters = Radius / 65535.0f;
        m_Material.SetFloat("_ChunkSpaceZeroTo65535ToLocalMeters", ChunkSpaceZeroTo65535ToLocalMeters);
    }

    void Start()
    {
        SetupRendererConstants();

        pointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, PointCount, UnsafeUtility.SizeOf<CloudVertexRepro>());

        NativeArray<CloudVertexRepro> pointArray = new NativeArray<CloudVertexRepro>(PointCount, Allocator.Persistent);  
        for (int i = 0; i < pointArray.Length; i++)
        {
            // float3 pos = UnityEngine.Random.insideUnitSphere * 65535;
            float3 pos = 65535.0f * new float3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            int3 posQuantized = (int3)pos;

            byte r = (byte)UnityEngine.Random.Range(0, 255);
            byte g = (byte)UnityEngine.Random.Range(0, 255);
            byte b = (byte)UnityEngine.Random.Range(0, 255);

            CloudVertexRepro thisPoint = CloudVertexRepro.FromData(posQuantized, r, g, b);  

            pointArray[i] = thisPoint;
        }

        pointBuffer.SetData(pointArray);

        pointArray.Dispose();   
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    
    // Update is called once per frame
    void Update()
    {
        if (pointBuffer == null)
        {
            return;
        }

        // Render.
        m_Material.SetBuffer(s_PositionsId, pointBuffer);
        m_Material.SetFloat(s_PointSizeId, pointSize);
        m_Material.SetMatrix(s_ObjectToWorldId, transform.localToWorldMatrix);

        RenderParams rp = new RenderParams(m_Material);
        rp.camera = null; 
        rp.worldBounds = bounds;
        rp.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        Graphics.RenderPrimitives(rp, MeshTopology.Quads, pointBuffer.count * 4);
    }
}
