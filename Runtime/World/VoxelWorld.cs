using Priority_Queue;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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

            if (generatingChunks)
            {
                FinishGeneratingChunks();
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
            UpdateChunks();
        }

        private void LateUpdate()
        {
            LateUpdateChunks();
            LateUpdateBlocks();
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
