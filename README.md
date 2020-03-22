# HertzVox 2
HertzVox 2 is an efficient voxel framework for Unity, built with performance and usability in mind.

⚠ **This project is still in development. The future is also uncertain! But it's still usable!** ⚠

## 🎇 Features
- Built on efficient Burst jobs
- Easy to use API
- Easily create new blocks
- Supports both infinite worlds and set sized worlds
- Theoretically supports 64,000 blocks  

Also works really well in the Unity editor and with the following features  

- Fast enter play mode settings
- IL2CPP

## 🚧 Limitations
This framework does have some limitations you need to be aware of:  

- All the block textures need to be the same size. 
- Currently, only block shapes are present. New ones need to be hardcoded.
- Blocks can't have "states". They are basically just ID numbers connected to textures.
If you find any more limitations, please let me know and maybe we can fix it or it will be added to the list!

## ✅ To do
Here are some things I want to do in the future. The timeframe of these features are uncertain. Feel free to try and make them yourself and make a pull request, if you feel like it! 

- More XML documentation
- Better algorithms
- Support more block shapes, like slabs, stairs, etc
- Rotatable blocks
- Greedy meshing for chunk renderers and colliders

## 🔨 How do I...
### Get a block
```cs
// Get a block using the identifier.
Block stone = BlockProvider.GetBlock("stone");
// Get a block using the ID that it gets assigned. 
// NOTE: This can change between sessions. The identifier is the safe way to go.
Block grass = BlockProvider.GetBlock(2);
```

### Get a block in the world
```cs
// Position at 10, 10, 10 in world space.
int3 position = new int3(10, 10, 10);
// Get the block from the current active world.
Block block = Voxels.GetBlock(position);
```

### Set a block in the world
```cs
// Position at 10, 10, 10 in world space.
int3 position = new int3(10, 10, 10);
// Get the block with the 'grass' identifier.
Block block = BlockProvider.GetBlock("grass");
// Set the block in the current active world.
Voxels.SetBlock(position, block);
```

### Set multiple blocks in the world
```cs
// From position.
int3 fromPos = new int3(10, 10, 10);
// To position.
int3 toPos = new int3(20, 20, 20);
// The block I want to set.
Block block = BlockProvider.GetBlock("planks");
// Set the blocks in the current active world.
Voxels.SetBlocks(fromPos, toPos, block);
```

### Set a block in the world without updating the terrain
```cs
// Position at 10, 10, 10 in world space.
int3 position = new int3(10, 10, 10);
// Get the block with the 'grass' identifier.
Block block = BlockProvider.GetBlock("grass");
// Set the block in the current active world. Raw means it won't automatically update the chunks.
// Also works with SetBlocks.
Voxels.SetBlockRaw(position, block);
```

## ❓ Q&A
**Why HertzVox __2__?**  
This project is the "successor" to my previous voxel engine that got a bit inefficient and hard to work with.

**What about [Voxelmetric](https://github.com/Hertzole/Voxelmetric)?**  
This project focuses much more on usability and performance and I've spent a lot of time making sure usability is as easy as possible and keeping performance up. Voxelmetric is also quite easy to use and has a lot of performance with the number of features it provides, but it is a bit harder *for me* to work with. So I decided to roll my own solution. Voxelmetric, on the other hand, supports more block types and actual threading.

## 📃 License
The project is licensed under MIT. Basically, do whatever you want but I'm not liable for anything.