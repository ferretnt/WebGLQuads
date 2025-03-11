using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using UnityEngine;

public class ComputeShaderWebGLBug : MonoBehaviour
{
    public int maxBytesInFrame = 10000000; 
    public int maxBytesInWrite = 1000000;
    public int framesToRun = 500;

    async Awaitable Start()
    {
        Debug.Log("Start()");

        for (int i = 0; i < framesToRun; i++)
        {
            List<GraphicsBuffer> buffers = new List<GraphicsBuffer>();
            int bytesInFrame = 0;
            while (bytesInFrame < maxBytesInFrame)
            {
                int elementSize = Random.Range(1, 4) * 4;

                int thisBufferElementCount = Random.Range(1, maxBytesInWrite / elementSize);

                GraphicsBuffer buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, thisBufferElementCount, elementSize);
                buffers.Add(buffer);

                using (var bufferData = new NativeArray<byte>(thisBufferElementCount * elementSize, Allocator.Persistent))
                {
                    Assert.AreEqual(thisBufferElementCount * elementSize, bufferData.Length); 
                    buffer.SetData(bufferData);
                }   

                bytesInFrame += thisBufferElementCount * elementSize; 
            }
            await Awaitable.NextFrameAsync();

            Debug.Log($"End of frame {i}. Freeing {buffers.Count} buffers");

            // Free all buffers. Comment this in and remove the unloadUnusedAssets to expose bug. 
            foreach (var buffer in buffers)
            {
                buffer.Dispose(); // Have also tried buffer.Release()
            }

            // Alternative path letting Unity and GC handle disposal
            // buffers.Clear();
            // await Resources.UnloadUnusedAssets();
        }
        Debug.Log("Done");   
    }
}
