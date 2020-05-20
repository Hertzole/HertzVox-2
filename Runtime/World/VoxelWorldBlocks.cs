using System.Collections.Generic;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public partial class VoxelWorld
    {
        private List<Chunk> ghostChunks = new List<Chunk>();

        private void LateUpdateBlocks()
        {
            UpdateGhostChunks();
        }

        private void UpdateGhostChunks()
        {
            if (ghostChunks.Count > 0)
            {
                for (int i = 0; i < ghostChunks.Count; i++)
                {
                    Serialization.SaveChunk(ghostChunks[i], true);
                }

                ghostChunks.Clear();
            }
        }

        public Block GetBlock(int3 position)
        {
            int3 chunkPos = Helpers.ContainingChunkPosition(position);

            if (chunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                int xx = Helpers.Mod(position.x, Chunk.CHUNK_SIZE);
                int yy = Helpers.Mod(position.y, Chunk.CHUNK_SIZE);
                int zz = Helpers.Mod(position.z, Chunk.CHUNK_SIZE);

                return chunk.GetBlock(xx, yy, zz);
            }

            return BlockProvider.GetBlock("air");
        }

        public void SetBlock(int3 position, Block block, bool urgent = true)
        {
            int3 chunkPos = Helpers.ContainingChunkPosition(position);

            if (chunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                int xx = Helpers.Mod(position.x, Chunk.CHUNK_SIZE);
                int yy = Helpers.Mod(position.y, Chunk.CHUNK_SIZE);
                int zz = Helpers.Mod(position.z, Chunk.CHUNK_SIZE);

                chunk.SetBlock(xx, yy, zz, block, urgent);
                bool updateNorth = zz == Chunk.CHUNK_SIZE - 1;
                bool updateSouth = zz == 0;
                bool updateEast = xx == Chunk.CHUNK_SIZE - 1;
                bool updateWest = xx == 0;
                bool updateTop = yy == Chunk.CHUNK_SIZE - 1;
                bool updateBottom = yy == 0;
                UpdateChunkNeighbors(chunkPos, updateNorth, updateSouth, updateEast, updateWest, updateTop, updateBottom);
            }
        }

        public void SetBlockRaw(int3 position, Block block)
        {
            int3 chunkPos = Helpers.ContainingChunkPosition(position);

            if (chunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                int xx = Helpers.Mod(position.x, Chunk.CHUNK_SIZE);
                int yy = Helpers.Mod(position.y, Chunk.CHUNK_SIZE);
                int zz = Helpers.Mod(position.z, Chunk.CHUNK_SIZE);

                chunk.SetBlockRaw(xx, yy, zz, block);
            }
        }

        public void SetBlocks(int3 start, int3 end, Block block)
        {
            SetBlocks(start.x, start.y, start.z, end.x, end.y, end.z, block);
        }

        public void SetBlocks(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, Block block)
        {
            VoxLogger.Log("VoxelWorld : SetBlocks " + block);

            FixValues(ref fromX, ref toX);
            FixValues(ref fromY, ref toY);
            FixValues(ref fromZ, ref toZ);

            int3 chunkFrom = Helpers.ContainingChunkPosition(fromX, fromY, fromZ);
            int3 chunkTo = Helpers.ContainingChunkPosition(toX, toY, toZ);

            int minY = Helpers.Mod(fromY, Chunk.CHUNK_SIZE);

            for (int cy = chunkFrom.y; cy <= chunkTo.y; cy += Chunk.CHUNK_SIZE, minY = 0)
            {
                int maxY = math.min(toY - cy, Chunk.CHUNK_SIZE - 1);
                int minZ = Helpers.Mod(fromZ, Chunk.CHUNK_SIZE);

                for (int cz = chunkFrom.z; cz <= chunkTo.z; cz += Chunk.CHUNK_SIZE, minZ = 0)
                {
                    int maxZ = math.min(toZ - cz, Chunk.CHUNK_SIZE - 1);
                    int minX = Helpers.Mod(fromX, Chunk.CHUNK_SIZE);

                    for (int cx = chunkFrom.x; cx <= chunkTo.x; cx += Chunk.CHUNK_SIZE, minX = 0)
                    {
                        bool ghostChunk = false;
                        int3 chunkPosition = new int3(cx, cy, cz);
                        VoxLogger.Log("VoxelWorld : SetBlocks " + block + " | Update chunk at " + chunkPosition + ".");
                        Chunk chunk = GetChunk(chunkPosition);
                        if (chunk == null)
                        {
                            VoxLogger.Log("VoxelWorld : SetBlocks " + block + " | Chunk is ghost chunk.");
                            ghostChunk = true;
                            chunk = CreateChunk(chunkPosition);
                            chunk.render = false;
                            chunks.Add(chunkPosition, chunk);
                            chunk.NeedsTerrain = !Serialization.LoadChunk(chunk, true);
                            if (chunk.NeedsTerrain)
                            {
                                AddToQueue(generateQueue, chunkPosition, 0);
                            }
                            ghostChunks.Add(chunk);
                        }

                        int maxX = math.min(toX - cx, Chunk.CHUNK_SIZE - 1);

                        int3 from = new int3(minX, minY, minZ);
                        int3 to = new int3(maxX, maxY, maxZ);

                        // Only update if it's placing blocks on the edges of the chunk and
                        // if they are the last/first ones in the loop. We don't need the chunks
                        // to update each other.
                        bool updateNorth = (from.z == Chunk.CHUNK_SIZE - 1 || to.z == Chunk.CHUNK_SIZE - 1) && cz == chunkTo.z;
                        bool updateSouth = (from.z == 0 || to.z == 0) && cz == chunkFrom.z;
                        bool updateEast = (from.x == Chunk.CHUNK_SIZE - 1 || to.x == Chunk.CHUNK_SIZE - 1) && cx == chunkTo.x;
                        bool updateWest = (from.x == 0 || to.x == 0) && cx == chunkFrom.x;
                        bool updateTop = (from.y == Chunk.CHUNK_SIZE - 1 || to.y == Chunk.CHUNK_SIZE - 1) && cy == chunkTo.y;
                        bool updateBottom = (from.y == 0 || to.y == 0) && cy == chunkFrom.y;

                        chunk.SetRangeRaw(from, to, block);
                        if (!ghostChunk)
                        {
                            // Only update this chunk.
                            chunk.UpdateChunk();

                            UpdateChunkNeighbors(chunkPosition, updateNorth, updateSouth, updateEast, updateWest, updateTop, updateBottom);
                        }
                        else if (!chunk.NeedsTerrain)
                        {
                            VoxLogger.Log("VoxelWorld : SetBlocks " + block + " | Saving ghost chunk " + chunk + ".");
                            Serialization.SaveChunk(chunk, true);
                            DestroyChunk(chunk);
                        }
                    }
                }
            }
        }
    }
}
