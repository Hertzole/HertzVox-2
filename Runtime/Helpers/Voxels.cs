using System;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public static class Voxels
    {
        /// <summary>
        /// Gets a block from the world.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="z">The Z position.</param>
        /// <param name="world">The world you want to get the block from. If null, the current active world will be used.</param>
        /// <returns>The block at the given position.</returns>
        public static Block GetBlock(int x, int y, int z, VoxelWorld world = null)
        {
            return GetBlock(new int3(x, y, z), world);
        }

        /// <summary>
        /// Gets a block from the world.
        /// </summary>
        /// <param name="position">The world position.</param>
        /// <param name="world">The world you want to get the block from. If null, the current active world will be used.</param>
        /// <returns>The block at the given position.</returns>
        public static Block GetBlock(int3 position, VoxelWorld world = null)
        {
            if (world == null)
            {
                world = VoxelWorld.Main;

                if (world == null)
                {
                    Debug.LogError("There's no world to get a block from.");
                    return BlockProvider.GetBlock(BlockProvider.AIR_TYPE);
                }
            }

            return world.GetBlock(position);
        }

        /// <summary>
        /// Set a block in the world and updates the terrain.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="z">The Z position.</param>
        /// <param name="block">The block you want to set.</param>
        /// <param name="urgent">If true, the chunk will update as soon as possible.</param>
        /// <param name="world">The world you want to get the block from. If null, the current active world will be used.</param>
        public static void SetBlock(int x, int y, int z, Block block, bool urgent = true, VoxelWorld world = null)
        {
            SetBlock(new int3(x, y, z), block, urgent, world);
        }

        /// <summary>
        /// Set a block in the world and updates the terrain.
        /// </summary>
        /// <param name="position">The world position.</param>
        /// <param name="block">The block you want to set.</param>
        /// <param name="urgent">If true, the chunk will update as soon as possible.</param>
        /// <param name="world">The world you want to get the block from. If null, the current active world will be used.</param>
        public static void SetBlock(int3 position, Block block, bool urgent = true, VoxelWorld world = null)
        {
            if (world == null)
            {
                world = VoxelWorld.Main;

                if (world == null)
                {
                    Debug.LogError("There's no world to set a block in.");
                    return;
                }
            }

            world.SetBlock(position, block, urgent);
        }

        /// <summary>
        /// Set a block in the world but it does NOT updates the terrain.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="z">The Z position.</param>
        /// <param name="block">The block you want to set.</param>
        /// <param name="world">The world you want to get the block from. If null, the current active world will be used.</param>
        public static void SetBlockRaw(int x, int y, int z, Block block, VoxelWorld world = null)
        {
            SetBlockRaw(new int3(x, y, z), block, world);
        }

        /// <summary>
        /// Set a block in the world but it does NOT updates the terrain.
        /// </summary>
        /// <param name="position">The world position.</param>
        /// <param name="block">The block you want to set.</param>
        /// <param name="world">The world you want to get the block from. If null, the current active world will be used.</param>
        public static void SetBlockRaw(int3 position, Block block, VoxelWorld world = null)
        {
            if (world == null)
            {
                world = VoxelWorld.Main;

                if (world == null)
                {
                    Debug.LogError("There's no world to set a block in.");
                    return;
                }
            }

            world.SetBlockRaw(position, block);
        }

        /// <summary>
        /// Sets multiple blocks in the world.
        /// </summary>
        /// <param name="fromX">From X position.</param>
        /// <param name="fromY">From Y position.</param>
        /// <param name="fromZ">From Z position.</param>
        /// <param name="toX">To X position.</param>
        /// <param name="toY">To Y position.</param>
        /// <param name="toZ">To Z position.</param>
        /// <param name="block">The block you want to set.</param>
        /// <param name="world">The world you want to get the block from. If null, the current active world will be used.</param>
        public static void SetBlocks(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, Block block, VoxelWorld world = null)
        {
            SetBlocks(new int3(fromX, fromY, fromZ), new int3(toX, toY, toZ), block, world);
        }

        /// <summary>
        /// Sets multiple blocks in the world.
        /// </summary>
        /// <param name="from">From world position.</param>
        /// <param name="to">To world position.</param>
        /// <param name="block">The block you want to set.</param>
        /// <param name="world">The world you want to get the block from. If null, the current active world will be used.</param>
        public static void SetBlocks(int3 from, int3 to, Block block, VoxelWorld world = null)
        {
            if (world == null)
            {
                world = VoxelWorld.Main;

                if (world == null)
                {
                    Debug.LogError("There's no world to set blocks in.");
                    return;
                }
            }

            world.SetBlocks(from, to, block);
        }

        /// <summary>
        /// Casts a ray into the world.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="hit"></param>
        /// <param name="world"></param>
        /// <param name="range"></param>
        /// <returns>True if it hit something.</returns>
        public static bool Raycast(Ray ray, out VoxelRaycastHit hit, float range, VoxelWorld world = null)
        {
            if (world == null)
            {
                world = VoxelWorld.Main;
                if (world == null)
                {
                    Debug.LogError("There's no world to raycast in.");
                    hit = new VoxelRaycastHit()
                    {
                        adjacentPosition = int3.zero,
                        block = BlockProvider.GetBlock(BlockProvider.AIR_TYPE),
                        blockPosition = int3.zero,
                        direction = float3.zero,
                        scenePosition = Vector3.zero
                    };
                    return false;
                }
            }

            // Position as we work through the raycast, starts at origin and gets updated as it reaches each block boundary on the route
            Vector3 pos = ray.origin;
            //Normalized direction of the ray
            Vector3 dir = ray.direction.normalized;

            //Transform the ray to match the rotation and position of the world:
            pos -= world.transform.position;
            pos -= new Vector3(0.5f, 0.5f, 0.5f);
            pos = Quaternion.Inverse(world.gameObject.transform.rotation) * pos;
            dir = Quaternion.Inverse(world.transform.rotation) * dir;

            // BlockPos to check if the block should be returned
            int3 bPos = new int3(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
            //Block pos that gets set to one block behind the hit block, useful for placing blocks at the hit location
            int3 adjacentBPos = bPos;

            // Positive copy of the direction
            Vector3 dirP = new Vector3(Math.Abs(dir.x), Math.Abs(dir.y), Math.Abs(dir.z));
            // The sign of the direction
            int3 dirS = new int3(dir.x > 0 ? 1 : -1, dir.y > 0 ? 1 : -1, dir.z > 0 ? 1 : -1);

            // Boundary will be set each step as the nearest block boundary to each direction
            Vector3 boundary;
            // dist will be set to the distance in each direction to hit a boundary
            Vector3 dist;

            //The block at bPos
            Block hitBlock = world.GetBlock(bPos);
            bool hitSomething = false;
            while (hitBlock.id == 0 && math.distance(ray.origin, pos) < range)
            {
                // Get the nearest upcoming boundary for each direction
                boundary.x = MakeBoundary(dirS.x, pos.x);
                boundary.y = MakeBoundary(dirS.y, pos.y);
                boundary.z = MakeBoundary(dirS.z, pos.z);

                //Find the distance to each boundary and make the number positive
                dist = boundary - pos;
                dist = new Vector3(Math.Abs(dist.x), Math.Abs(dist.y), Math.Abs(dist.z));

                // Divide the distance by the strength of the corresponding direction, the
                // lowest number will be the boundary we will hit first. This is like distance
                // over speed = time where dirP is the speed and the it's time to reach the boundary
                dist.x /= dirP.x;
                dist.y /= dirP.y;
                dist.z /= dirP.z;

                // Use the shortest distance as the distance to travel this step times each direction
                // to give us the position where the ray intersects the closest boundary
                if (dist.x < dist.y && dist.x < dist.z)
                {
                    pos += dist.x * dir;
                }
                else if (dist.y < dist.z)
                {
                    pos += dist.y * dir;
                }
                else
                {
                    pos += dist.z * dir;
                }

                // Set the block pos but use ResolveBlockPos because one of the components of pos will be exactly on a block boundary
                // and will need to use the corresponding direction sign to decide which side of the boundary to fall on
                adjacentBPos = bPos;
                bPos = new int3(ResolveBlockPos(pos.x, dirS.x), ResolveBlockPos(pos.y, dirS.y), ResolveBlockPos(pos.z, dirS.z));
                hitBlock = world.GetBlock(bPos);
                if (hitBlock.id != 0)
                {
                    hitSomething = true;
                }

                if (bPos.y <= 0)
                {
                    break;
                }

                // The while loop then evaluates if hitblock is a viable block to stop on and
                // if not does it all again starting from the new position
            }

            if (!hitSomething)
            {
                bPos.y = 0;
                adjacentBPos.y = 0;
            }

            hit = new VoxelRaycastHit()
            {
                block = hitBlock,
                blockPosition = bPos,
                adjacentPosition = adjacentBPos,
                direction = dir,
                scenePosition = pos
            };

            return hitSomething;
        }

        // Resolve a component of a vector3 into an int for a blockPos by using the sign
        // of the corresponding direction to decide if the position is on a boundary
        private static int ResolveBlockPos(float pos, int dirS)
        {
            float fPos = pos + 0.5f;
            int iPos = (int)fPos;

            if (Math.Abs(fPos - iPos) < 0.001f)
            {
                return dirS == 1 ? iPos : iPos - 1;
            }

            return Mathf.RoundToInt(pos);
        }

        // Returns the nearest boundary to pos
        private static float MakeBoundary(int dirS, float pos)
        {
            pos += 0.5f;

            int result = dirS == -1 ? Mathf.FloorToInt(pos) : Mathf.CeilToInt(pos);

            if (Math.Abs(result - pos) < 0.001f)
            {
                result += dirS;
            }

            return result - 0.5f;
        }
    }
}
