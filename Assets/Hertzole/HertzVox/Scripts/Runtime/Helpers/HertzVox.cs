using System;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public static class HertzVox
    {
        public static VoxelRaycastHit Raycast(Ray ray, VoxelWorld world, float range)
        {
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

                // The while loop then evaluates if hitblock is a viable block to stop on and
                // if not does it all again starting from the new position
            }

            return new VoxelRaycastHit()
            {
                block = hitBlock,
                blockPosition = bPos,
                adjacentPosition = adjacentBPos,
                direction = dir,
                scenePosition = pos
            };
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