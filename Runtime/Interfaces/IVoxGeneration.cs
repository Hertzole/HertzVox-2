using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public interface IVoxGeneration
    {
        JobHandle GenerateChunk(NativeArray<ushort> blocks, int3 position);
    }
}