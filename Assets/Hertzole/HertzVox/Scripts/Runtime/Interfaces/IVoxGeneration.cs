using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public interface IVoxGeneration
    {
        void GenerateChunk(Chunk chunk, int3 position);
    }
}