using System.IO;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hertzole.HertzVox
{
	public static class Serialization
	{
		private static string saveLocation;

		public static bool IsInitialized { get; private set; }

		public static string SaveLocation { get { return saveLocation; } set { saveLocation = value; TempSaveLocation = value + "/temp/"; } }
		public static string TempSaveLocation { get; private set; }

		public const ushort SAVE_VERSION = 1;

#if UNITY_2019_3_OR_NEWER
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void ResetStatics()
		{
			IsInitialized = false;
		}
#endif

		public static void Initialize(string saveLocation)
		{
			SaveLocation = saveLocation;

			IsInitialized = true;
		}

		public static void SaveChunk(Chunk chunk, bool temporary = false)
		{
			Assert.IsTrue(IsInitialized, "You need to initialize first!");

			if (chunk == null)
			{
				return;
			}

			MakeSureSaveLocationExists();

			NativeList<int2> blocks = chunk.blocks.Compress();

			string path = GetChunkPath(chunk, temporary);

			using (BinaryWriter w = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate)))
			{
				w.Write(SAVE_VERSION);

				int intSize = sizeof(int);
				int length = (intSize * blocks.Length) * 2 + sizeof(ushort);

				for (int i = 0; i < blocks.Length; i++)
				{
					w.Write(blocks[i].x);
					w.Write(blocks[i].y);
				}

				w.BaseStream.SetLength(length);
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

			string path = GetChunkPath(chunk, temporary);

			if (File.Exists(path))
			{
				NativeList<int2> compressedBlocks = new NativeList<int2>(Allocator.Temp);

				using (BinaryReader r = new BinaryReader(File.Open(path, FileMode.Open)))
				{
					int pos = 0;
					int streamLength = (int)r.BaseStream.Length;

					ushort saveVersion = r.ReadUInt16();
					pos += sizeof(ushort);

					while (pos < streamLength)
					{
						int id = r.ReadInt32();
						pos += sizeof(int);
						int length = r.ReadInt32();
						pos += sizeof(int);
						compressedBlocks.Add(new int2(id, length));
					}
				}

				chunk.blocks.DecompressAndApply(compressedBlocks);

				return true;
			}
			else
			{
				return false;
			}
		}

		public static void ClearTemp()
		{
			string[] files = Directory.GetFiles(TempSaveLocation, "*.bin");
			for (int i = 0; i < files.Length; i++)
			{
				File.Delete(files[i]);
			}
		}

		private static void MakeSureSaveLocationExists()
		{
			if (!Directory.Exists(SaveLocation))
			{
				Directory.CreateDirectory(SaveLocation);
			}

			if (!Directory.Exists(TempSaveLocation))
			{
				Directory.CreateDirectory(TempSaveLocation);
			}
		}

		private static string GetChunkPath(Chunk chunk, bool temporary)
		{
			return (temporary ? TempSaveLocation : SaveLocation) + $"/{chunk.position.x},{chunk.position.y},{chunk.position.z}.bin";
		}
	}
}
