using System.IO;
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

			NativeList<int2> blocks = chunk.blocks.Compress();

			string path = SaveFile(chunk.position, temporary);

		writing:
			try
			{
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

			if (File.Exists(path))
			{
				NativeList<int2> compressedBlocks = new NativeList<int2>(Allocator.Temp);

				Profiler.BeginSample("Load chunk binary");
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
				Profiler.EndSample();

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

		private static string SaveFile(int3 position, bool temporary)
		{
			builder.Clear();
			return builder.Append(temporary ? TempSaveLocation : saveLocation).Append("/" + position.x + "," + position.y + "," + position.z + ".bin").ToString();
			//string save = GetSaveLocation(temporary);
			//save += FileName(position);
			//return save;
		}

		private static string FileName(int3 position)
		{
			return position.x + "," + position.y + "," + position.z + ".bin";
		}

		private static string GetSaveLocation(bool temporary)
		{
			return (temporary ? TempSaveLocation : SaveLocation) + "/";
		}
	}
}
