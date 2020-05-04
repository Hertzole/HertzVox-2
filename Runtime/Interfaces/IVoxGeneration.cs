using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public interface IVoxGeneration
    {
        /// <summary>
        /// Returns a job that generates the chunk.
        /// </summary>
        /// <param name="blocks">The array of blocks for the chunk that needs to be filled.</param>
        /// <param name="position">The position of the chunk in the world.</param>
        /// <returns></returns>
        JobHandle GenerateChunk(NativeArray<int> blocks, int3 position);
    }
}
