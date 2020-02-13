# HertzVox 2
HertzVox 2 is a efficient voxel framework for Unity, built with performance and usability in mind.

## Features
- Efficient Burst compiled jobs
- Easy to use methods to manipulate your voxel world
- Easy to use API to generate worlds
- Easily create blocks with scriptable objects
- Supports over 4 million blocks (in theory)!
- Generates greedy Unity colliders

## Limitations
Currently there are a few limitations due to jobs:
- Currently only block shapes are present. New ones need to be hard coded.
- The world size can't be infinite (will be changed in the future).
- No block merging (greedy meshing) for the actual meshes.

## Q&A
**Why HertzVox __2__?**  
This project is the "successor" to my previous voxel engine that got a bit inefficient and hard to work with.

**What about [Voxelmetric](https://github.com/Hertzole/Voxelmetric)?**  
This project focuses much more on usability and performance and I've spent a lot of time making sure usability is as easy as possible and keeping performance up. Voxelmetric is also quite easy to use and has a lot of performance with the amount of features it provides, but it is a bit harder *for me* to work with. So I decided to roll my own solution. Voxelmetric on the other hand supports more block types and actual threading.
