using Priority_Queue;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Hertzole.HertzVox
{
    [DefaultExecutionOrder(-50)]
    public partial class VoxelWorld : MonoBehaviour
    {
        [SerializeField]
        private BlockCollection blockCollection = null;
        [SerializeField]
        private bool clearTempOnDestroy = true;
        [SerializeField]
        private bool dontDestroyOnLoad = true;

        [Header("Chunks")]
        [SerializeField]
        private float chunkGenerateDelay = 0.2f;
        [SerializeField]
        private Material chunkMaterial = null;
        [SerializeField]
        private MeshCollider chunkColliderPrefab = null;
        [SerializeField]
        private bool useRendererPrefab = false;
        [SerializeField]
        private MeshRenderer rendererPrefab = null;

        [Header("World Size")]
        [SerializeField]
        private int minX = -10;
        [SerializeField]
        private int maxX = 10;
        [SerializeField]
        private bool infiniteX = true;
        [SerializeField]
        private int minZ = -10;
        [SerializeField]
        private int maxZ = 10;
        [SerializeField]
        private bool infiniteZ = true;
        [SerializeField]
        private int maxY = 8;

        [Header("Queue Sizes")]
        [SerializeField]
        [Range(0, 3)]
        private int maxJobFrames = 3;
        [SerializeField]
        private int maxGenerateJobs = 40;
        [SerializeField]
        private int maxRenderJobs = 20;
        [SerializeField]
        private int maxColliderJobs = 20;

        private float nextChunkGenerate;

        private Material mat;

        private IVoxGeneration generator;

        private FastPriorityQueue<ChunkNode> generateQueue = new FastPriorityQueue<ChunkNode>(64);
        private FastPriorityQueue<ChunkNode> renderQueue = new FastPriorityQueue<ChunkNode>(64);
        private FastPriorityQueue<ChunkNode> colliderQueue = new FastPriorityQueue<ChunkNode>(64);

        private NativeHashMap<int3, ChunkJobData> generateJobs;
        private NativeHashMap<int3, ChunkJobData> renderJobs;
        private NativeHashMap<int3, ChunkJobData> colliderJobs;

        private NativeList<int3> renderChunks;
        private NativeList<int3> chunksToRemove;

        private List<VoxelLoader> loaders = new List<VoxelLoader>();

        public static VoxelWorld Main { get; private set; }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Main = null;
        }
#endif

        private void OnEnable()
        {
            if (Main != null)
            {
                if (!dontDestroyOnLoad && Main != this)
                {
                    Debug.LogError("Multiple Voxel Worlds! There can only be one.", gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }

                return;
            }

            if (Main == null)
            {
                Main = this;
            }
        }

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            // There's a chance there are multiple worlds active currently.
            // If there is, stop here.
            if (Main != null && Main != this)
            {
                return;
            }

            if (!BlockProvider.IsInitialized)
            {
                BlockProvider.Initialize(blockCollection);
            }

            if (!TextureProvider.IsInitialized)
            {
                TextureProvider.Initialize(blockCollection);
            }

            if (!Serialization.IsInitialized)
            {
                Serialization.Initialize(Application.persistentDataPath + "/HertzVox/");
            }

            Texture2D atlas = TextureProvider.GetAtlas();

            mat = new Material(chunkMaterial)
            {
                mainTexture = atlas
            };

            int xSize = atlas.width / blockCollection.TextureSize;
            int ySize = atlas.height / blockCollection.TextureSize;

            mat.SetInt("_AtlasX", xSize);
            mat.SetInt("_AtlasY", ySize);
            mat.SetVector("_AtlasRec", new Vector4(1.0f / xSize, 1.0f / ySize));

            generateJobs = new NativeHashMap<int3, ChunkJobData>(0, Allocator.Persistent);
            renderJobs = new NativeHashMap<int3, ChunkJobData>(0, Allocator.Persistent);
            colliderJobs = new NativeHashMap<int3, ChunkJobData>(0, Allocator.Persistent);

            renderChunks = new NativeList<int3>(Allocator.Persistent);
            chunksToRemove = new NativeList<int3>(Allocator.Persistent);

            generator = GetComponent<IVoxGeneration>();

            if (generator == null)
            {
                Debug.LogWarning("There's no voxel generator (IVoxGenerator) attached to Voxel World " + gameObject.name + ". No chunks will show up.");
            }
        }

        private void OnDestroy()
        {
            // Make sure only the active world disposes of things.
            if (Main != null && Main != this)
            {
                return;
            }

            DisposeJobList(generateJobs);
            DisposeJobList(renderJobs);
            DisposeJobList(colliderJobs);

            foreach (Chunk chunk in chunks.Values)
            {
                chunk.Dispose(true);
            }

            TextureProvider.Dispose();
            BlockProvider.Dispose();

            Destroy(mat);

            generateJobs.Dispose();
            renderJobs.Dispose();
            colliderJobs.Dispose();

            renderChunks.Dispose();
            chunksToRemove.Dispose();

            if (clearTempOnDestroy)
            {
                Serialization.ClearTemp();
            }
        }

        private void DisposeJobList(NativeHashMap<int3, ChunkJobData> list)
        {
            NativeArray<ChunkJobData> jobs = list.GetValueArray(Allocator.Temp);
            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i].job.Complete();
            }

            jobs.Dispose();
        }

        private void Update()
        {
            if (!useRendererPrefab)
            {
                for (int i = 0; i < renderChunks.Length; i++)
                {
                    chunks[renderChunks[i]].Draw(mat);
                }
            }

            if (Time.unscaledTime >= nextChunkGenerate)
            {
                nextChunkGenerate = Time.unscaledTime + chunkGenerateDelay;
                GenerateChunksAroundTargets();
            }

            ProcessChunks();

            NativeList<int3> jobsToRemove = new NativeList<int3>(Allocator.Temp);

            ProcessGeneratorJobs(jobsToRemove);
            jobsToRemove.Clear();
            ProcessRenderJobs(jobsToRemove);
            jobsToRemove.Clear();
            ProcessColliderJobs(jobsToRemove);
            jobsToRemove.Dispose();
            ProcessChunkRemoval();
        }

        private void LateUpdate()
        {
            int numChunks = 0;

            while (generateQueue.Count > 0)
            {
                if (generator == null)
                {
                    break;
                }

                if (numChunks > maxGenerateJobs)
                {
                    break;
                }

                ChunkNode node = generateQueue.Dequeue();

                if (chunks.TryGetValue(node.position, out Chunk chunk))
                {
                    if (generateJobs.ContainsKey(chunk.position) || !chunk.NeedsTerrain || chunk.RequestedRemoval || chunk.GeneratingTerrain)
                    {
                        continue;
                    }

                    chunk.StartGenerating(new NativeArray<int>(Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE, Allocator.TempJob));

                    JobHandle job = generator.GenerateChunk(chunk.temporaryBlocks, chunk.position);
                    ChunkJobData data = new ChunkJobData(node.position, job, node.Priority, false);
                    generateJobs.Add(chunk.position, data);

                    numChunks++;
                }
            }

            numChunks = 0;

            while (renderQueue.Count > 0)
            {
                if (numChunks > maxRenderJobs)
                {
                    break;
                }

                ChunkNode node = renderQueue.Dequeue();
                if (chunks.TryGetValue(node.position, out Chunk chunk))
                {
                    if (AddChunkToRenderList(chunk, node.Priority))
                    {
                        numChunks++;
                    }
                }
            }

            numChunks = 0;

            while (colliderQueue.Count > 0)
            {
                if (numChunks > maxColliderJobs)
                {
                    break;
                }

                ChunkNode node = colliderQueue.Dequeue();
                if (chunks.TryGetValue(node.position, out Chunk chunk))
                {
                    if (colliderJobs.ContainsKey(node.position) || chunk.RequestedRemoval)
                    {
                        continue;
                    }

                    JobHandle job = chunk.ScheduleColliderJob();
                    colliderJobs.Add(node.position, new ChunkJobData(node.position, job, node.Priority, false));
                    numChunks++;
                }
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
                            chunks.Add(chunkPosition, chunk);
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
                        else
                        {
                            VoxLogger.Log("VoxelWorld : SetBlocks " + block + " | Saving ghost chunk " + chunk + ".");
                            Serialization.SaveChunk(chunk, true);
                            DestroyChunk(chunk);
                        }
                    }
                }
            }
        }

        public void UpdateChunkNeighbors(int3 chunkPosition, bool updateNorth, bool updateSouth, bool updateEast, bool updateWest, bool updateTop, bool updateBottom)
        {
            // Only update neighbor chunks if they need to be updated.
            if (updateNorth)
            {
                int3 northPos = new int3(chunkPosition.x, chunkPosition.y, chunkPosition.z + Chunk.CHUNK_SIZE);
                // Use TryGetValue instead because then we can get the chunk and also check if it exists, 
                // instead of getting the chunk again when calling update.
                if (chunks.TryGetValue(northPos, out Chunk northChunk) && northChunk.HasTerrain)
                {
                    northChunk.UpdateChunk();
                }
            }
            if (updateSouth)
            {
                int3 southPos = new int3(chunkPosition.x, chunkPosition.y, chunkPosition.z - Chunk.CHUNK_SIZE);
                if (chunks.TryGetValue(southPos, out Chunk southChunk) && southChunk.HasTerrain)
                {
                    southChunk.UpdateChunk();
                }
            }
            if (updateEast)
            {
                int3 eastPos = new int3(chunkPosition.x + Chunk.CHUNK_SIZE, chunkPosition.y, chunkPosition.z);
                if (chunks.TryGetValue(eastPos, out Chunk eastChunk) && eastChunk.HasTerrain)
                {
                    eastChunk.UpdateChunk();
                }
            }
            if (updateWest)
            {
                int3 westPos = new int3(chunkPosition.x - Chunk.CHUNK_SIZE, chunkPosition.y, chunkPosition.z);
                if (chunks.TryGetValue(westPos, out Chunk westChunk) && westChunk.HasTerrain)
                {
                    westChunk.UpdateChunk();
                }
            }
            if (updateTop)
            {
                int3 topPos = new int3(chunkPosition.x, chunkPosition.y + Chunk.CHUNK_SIZE, chunkPosition.z);
                if (chunks.TryGetValue(topPos, out Chunk topChunk) && topChunk.HasTerrain)
                {
                    topChunk.UpdateChunk();
                }
            }
            if (updateBottom)
            {
                int3 bottomPos = new int3(chunkPosition.x, chunkPosition.y - Chunk.CHUNK_SIZE, chunkPosition.z);
                if (chunks.TryGetValue(bottomPos, out Chunk bottomChunk) && bottomChunk.HasTerrain)
                {
                    bottomChunk.UpdateChunk();
                }
            }
        }

        private void FixValues(ref int start, ref int end)
        {
            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }
        }

        public Chunk GetChunk(int3 position)
        {
            chunks.TryGetValue(position, out Chunk chunk);
            return chunk;
        }

        public bool TryGetChunk(int3 position, out Chunk chunk)
        {
            return chunks.TryGetValue(position, out chunk);
        }

        /// <summary>
        /// Unloads all the chunks in the world and loads new ones in the temp folder.
        /// </summary>
        public void RefreshWorld()
        {
            generateQueue.Clear();
            renderQueue.Clear();
            colliderQueue.Clear();

            FinishJobsInList(generateJobs);
            FinishJobsInList(renderJobs);
            FinishJobsInList(colliderJobs);

            foreach (Chunk chunk in Chunks)
            {
                chunk.Dispose(true);
            }

            chunks.Clear();
        }

        private void GenerateChunksAroundTargets()
        {
            if (loaders.Count == 0)
            {
                return;
            }

            renderChunks.Clear();

            for (int i = 0; i < loaders.Count; i++)
            {
                VoxelLoader loader = loaders[i];

                if (loader == null)
                {
                    continue;
                }

                int3 targetPosition = Helpers.WorldToChunk(loader.transform.position, Chunk.CHUNK_SIZE);

                int xMin = (loader.SingleChunk ? 0 : -loader.ChunkDistanceX) + targetPosition.x - 1;
                int zMin = (loader.SingleChunk ? 0 : -loader.ChunkDistanceZ) + targetPosition.z - 1;
                int xMax = (loader.SingleChunk ? 1 : loader.ChunkDistanceX) + targetPosition.x + 1;
                int zMax = (loader.SingleChunk ? 1 : loader.ChunkDistanceZ) + targetPosition.z + 1;

                Profiler.BeginSample("Create chunk region");
                for (int x = xMin; x < xMax; x++)
                {
                    for (int z = zMin; z < zMax; z++)
                    {
                        for (int y = 0; y < maxY; y++)
                        {
                            if ((!infiniteX && (x < minX || x > maxX)) || (!infiniteZ && (z < minZ || z > maxZ)))
                            {
                                continue;
                            }

                            int3 chunkPosition = new int3(x * Chunk.CHUNK_SIZE, y * Chunk.CHUNK_SIZE, z * Chunk.CHUNK_SIZE);

                            if (renderChunks.Contains(chunkPosition))
                            {
                                continue;
                            }

                            bool shouldRender = false;
                            if (x != xMin && z != zMin && x != xMax - 1 && z != zMax - 1)
                            {
                                shouldRender = true;
                            }

                            float priority = math.distancesq(chunkPosition, targetPosition);
                            if (!chunks.TryGetValue(chunkPosition, out Chunk chunk))
                            {
                                chunk = CreateChunk(chunkPosition);
                                chunks.Add(chunkPosition, chunk);
                                chunk.NeedsTerrain = !Serialization.LoadChunk(chunk, true);
                                if (chunk.NeedsTerrain)
                                {
                                    AddToQueue(generateQueue, chunkPosition, priority);
                                }
                                else
                                {
                                    if (shouldRender)
                                    {
                                        TryToQueueChunkRender(chunk, priority);
                                    }
                                }
                            }
                            else if (chunk.HasTerrain && !chunk.UpdatingRenderer && !chunk.HasRender && shouldRender)
                            {
                                TryToQueueChunkRender(chunk, priority);
                            }

                            renderChunks.Add(chunkPosition);
                            chunk.render = shouldRender;
                        }
                    }
                }
                Profiler.EndSample();
            }

            Profiler.BeginSample("Remove chunks stage 1");
            chunksToRemove.Clear();
            if (chunks.Count != renderChunks.Length)
            {
                foreach (int3 chunk in chunks.Keys)
                {
                    if (!renderChunks.Contains(chunk))
                    {
                        DestroyChunk(chunks[chunk]);
                    }
                }
            }
            Profiler.EndSample();
        }

        public void RegisterLoader(VoxelLoader loader)
        {
            loaders.Add(loader);
        }

        public void UnregisterLoader(VoxelLoader loader)
        {
            loaders.Remove(loader);
        }

        private void FinishJobsInList(NativeHashMap<int3, ChunkJobData> list)
        {
            NativeArray<ChunkJobData> jobs = list.GetValueArray(Allocator.Temp);

            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i].job.Complete();
            }

            jobs.Dispose();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            foreach (KeyValuePair<int3, Chunk> chunk in chunks)
            {
                Gizmos.DrawWireCube(new Vector3(chunk.Key.x + (Chunk.CHUNK_SIZE / 2), chunk.Key.y + (Chunk.CHUNK_SIZE / 2), chunk.Key.z + (Chunk.CHUNK_SIZE / 2)), Vector3.one * Chunk.CHUNK_SIZE);
            }
        }
#endif
    }
}
