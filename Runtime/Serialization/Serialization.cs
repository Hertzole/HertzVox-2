using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace Hertzole.HertzVox
{
    public static class Serialization
    {
        private static string saveLocation;

        private static StringBuilder builder;

        public static bool IsInitialized { get; private set; }

        public static string SaveLocation { get { return saveLocation; } set { saveLocation = value; TempSaveLocation = value + "/temp/"; } }
        public static string TempSaveLocation { get; private set; }

        public const ushort SAVE_VERSION = 1;

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsInitialized = false;
            builder = null;
        }
#endif

        public static void Initialize(string saveLocation)
        {
            SaveLocation = saveLocation;
            builder = new StringBuilder(saveLocation.Length + 20);

            IsInitialized = true;

            if (!Directory.Exists(saveLocation))
            {
                Directory.CreateDirectory(saveLocation);
            }

            if (!Directory.Exists(TempSaveLocation))
            {
                Directory.CreateDirectory(TempSaveLocation);
            }
        }

        public static void SaveChunk(Chunk chunk, bool temporary = false)
        {
            Assert.IsTrue(IsInitialized, "You need to initialize first!");

            if (chunk == null)
            {
                return;
            }

            NativeList<int2> blocks = chunk.CompressBlocks();

            Dictionary<int, string> palette = BlockProvider.GetBlockPalette();

            string path = SaveFile(chunk.position, temporary);

            writing:
            try
            {
                using (BinaryWriter w = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate)))
                {
                    WriteChunkInfo(w, chunk.position, palette, blocks);
                }
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                goto writing;
            }

            blocks.Dispose();
        }

        public static bool LoadChunk(Chunk chunk, bool temporary = false)
        {
            Assert.IsTrue(IsInitialized, "You need to initialize first!");
            Assert.IsNotNull(chunk, "Chunk can't be null.");

            if (chunk == null)
            {
                return false;
            }

            Profiler.BeginSample("Get chunk file name");
            string path = SaveFile(chunk.position, temporary);
            Profiler.EndSample();

            return DeserializeChunk(chunk, path);
        }

        private static bool DeserializeChunk(Chunk chunk, string path)
        {
            if (File.Exists(path))
            {
                NativeList<int2> compressedBlocks = new NativeList<int2>(Allocator.Temp);
                Dictionary<int, string> palette = new Dictionary<int, string>();

                int3 position;
                Profiler.BeginSample("Load chunk binary");
                using (BinaryReader r = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    ushort saveVersion = r.ReadUInt16();

                    int x = r.ReadInt32();
                    int y = r.ReadInt32();
                    int z = r.ReadInt32();

                    position = new int3(x, y, z);

                    int paletteLength = r.ReadInt32();
                    for (int i = 0; i < paletteLength; i++)
                    {
                        palette.Add(r.ReadInt32(), r.ReadString());
                    }

                    int blockLength = r.ReadInt32();

                    for (int i = 0; i < blockLength; i++)
                    {
                        int id = r.ReadInt32();
                        int length = r.ReadInt32();
                        compressedBlocks.Add(new int2(id, length));
                    }
                }
                Profiler.EndSample();

                chunk.position = position;
                chunk.DecompressAndApply(compressedBlocks, palette);

                return true;
            }
            else
            {
                return false;
            }
        }

        private static void WriteChunkInfo(BinaryWriter w, int3 position, Dictionary<int, string> palette, NativeList<int2> blocks)
        {
            w.Write(SAVE_VERSION);

            w.Write(position.x);
            w.Write(position.y);
            w.Write(position.z);

            w.Write(palette.Count);
            foreach (KeyValuePair<int, string> block in palette)
            {
                w.Write(block.Key);
                w.Write(block.Value);
            }

            w.Write(blocks.Length);
            for (int i = 0; i < blocks.Length; i++)
            {
                w.Write(blocks[i].x);
                w.Write(blocks[i].y);
            }
        }

        public static void SaveAllChunksToLocation(VoxelWorld world, string location)
        {
            List<Chunk> chunks = LoadAllChunks(world, true);
            string originalPath = SaveLocation;
            SaveLocation = location;

            for (int i = 0; i < chunks.Count; i++)
            {
                SaveChunk(chunks[i], false);
                chunks[i].Dispose();
            }

            SaveLocation = originalPath;
        }

        //public static void SaveAllJson(VoxelWorld world, string saveLocation = null)
        //{
        //    VoxelJsonData data = GetJsonData(world);

        //    string json = JsonUtility.ToJson(data, false);

        //    Debug.Log(json);
        //}

        public static string ToJson(VoxelWorld world, bool ignoreEmptyChunks = false, bool prettyPrint = false)
        {
            VoxelJsonData data = GetJsonData(world, ignoreEmptyChunks);
            return ToJson(data, prettyPrint);
        }

        public static string ToJson(VoxelJsonData data, bool prettyPrint = false)
        {
            string json = JsonUtility.ToJson(data, prettyPrint);
            return json;
        }

        public static VoxelJsonData GetJsonData(VoxelWorld world, bool ignoreEmptyChunks = false)
        {
            VoxelJsonData data = new VoxelJsonData(BlockProvider.GetBlockPalette());

            List<Chunk> chunks = LoadAllChunks(world, ignoreEmptyChunks);
            VoxelJsonChunkData[] chunkData = new VoxelJsonChunkData[chunks.Count];

            for (int i = 0; i < chunks.Count; i++)
            {
                chunkData[i] = new VoxelJsonChunkData(chunks[i]);
            }

            data.chunks = chunkData;

            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].Dispose(true);
            }

            return data;
        }

        public static void LoadAllJson(VoxelWorld world, string json, bool clearTemp = true)
        {
            LoadFromJsonData(world, JsonUtility.FromJson<VoxelJsonData>(json), clearTemp);
        }

        public static void LoadFromJsonData(VoxelWorld world, VoxelJsonData data, bool clearTemp = true)
        {
            if (clearTemp)
            {
                ClearTemp();
            }

            Dictionary<int, string> palette = new Dictionary<int, string>();
            for (int i = 0; i < data.palette.Length; i++)
            {
                palette.Add(data.palette[i].index, data.palette[i].id);
            }

            for (int i = 0; i < data.chunks.Length; i++)
            {
                int3 chunkPosition = new int3(data.chunks[i].position.x, data.chunks[i].position.y, data.chunks[i].position.z);

                NativeList<int2> blocks = new NativeList<int2>(Allocator.Temp);
                for (int j = 0; j < data.chunks[i].blocks.Length; j++)
                {
                    blocks.Add(new int2(data.chunks[i].blocks[j].x, data.chunks[i].blocks[j].y));
                }

                using (BinaryWriter w = new BinaryWriter(File.Open(SaveFile(chunkPosition, true), FileMode.OpenOrCreate)))
                {
                    WriteChunkInfo(w, chunkPosition, palette, blocks);
                }
            }

            world.RefreshWorld();
        }

        private static List<Chunk> LoadAllChunks(VoxelWorld world, bool ignoreEmptyChunks)
        {
            DumpLoadedChunksToTemp(world);
            List<Chunk> chunks = new List<Chunk>();
            string[] tempChunks = Directory.GetFiles(TempSaveLocation, "*.bin");

            if (tempChunks != null && tempChunks.Length > 0)
            {
                for (int i = 0; i < tempChunks.Length; i++)
                {
                    Chunk chunk = new Chunk(world, int3.zero, new ChunkBlocks(Chunk.CHUNK_SIZE));
                    DeserializeChunk(chunk, tempChunks[i]);
                    if (ignoreEmptyChunks)
                    {
                        NativeArray<int> blocks = chunk.GetAllBlocks(Allocator.Temp);
                        bool empty = true;
                        for (int b = 0; b < blocks.Length; b++)
                        {
                            if (blocks[b] != BlockProvider.AIR_TYPE_ID)
                            {
                                empty = false;
                                break;
                            }
                        }

                        blocks.Dispose();

                        if (empty)
                        {
                            // Need to dispose the chunk here to avoid memory leak.
                            chunk.Dispose();
                            continue;
                        }
                    }

                    chunks.Add(chunk);
                }
            }

            return chunks;
        }

        private static void DumpLoadedChunksToTemp(VoxelWorld world)
        {
            ICollection<Chunk> loadedChunks = world.Chunks;
            foreach (Chunk chunk in loadedChunks)
            {
                if (chunk.changed)
                {
                    SaveChunk(chunk, true);
                }
            }
        }

        public static void ClearTemp()
        {
            if (!Directory.Exists(TempSaveLocation))
            {
                return;
            }

            string[] files = Directory.GetFiles(TempSaveLocation, "*.bin");
            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string SaveFile(int3 position, bool temporary)
        {
            builder.Clear();
            return builder.Append(temporary ? TempSaveLocation : saveLocation)
                .Append('/').Append(position.x).Append(',').Append(position.y).Append(',').Append(position.z).Append(".bin").ToString();
        }
    }
}
