using UnityEngine;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;


public class SpatialHashGrid
{
    // The side length of a cube / cell in the regular grid
    private float _cellSize;
    // The amount of entries of our hashtable
    private int _tableSize;
    // hashtable, counts how many objects fall into the same table cell
    private NativeArray<UnsafeAtomicCounter32> _table;
    // Actual amount of objects contained in gridEntries
    private int _numObjects;
    // list containing indices of all objects in the grid. The indices of objects that fall into the same table entry are adjacent in this list
    private NativeArray<int> _gridEntries;

    public SpatialHashGrid(float cellSize, int maxNumObjects)
    {
        _cellSize = cellSize;
        _tableSize = 5 * maxNumObjects;
        _table = new NativeArray<UnsafeAtomicCounter32>(_tableSize + 1, Allocator.Persistent);
        _gridEntries = new NativeArray<int>(maxNumObjects, Allocator.Persistent); 
    }

    public static int Hash(int3 gridPos, int _tableSize)
    {
        unchecked
        {
            return math.abs((gridPos.x * 92837111) ^ (gridPos.y * 689287499) ^ (gridPos.z * 283923481)) % _tableSize;
        }
    }

    public static int3 GridPosition(float3 position, float cellsize)
    {
        return new int3(math.floor(position / cellsize));
    }

    public void Create(Particle[] particles)
    {
        _numObjects = math.min(particles.Length, _gridEntries.Length);
    }
}

/*[BurstCompile]
struct InitHashTableJob : IJobParallelFor
{
    [ReadOnly] public Particle[] particles;
    public NativeArray<UnsafeAtomicCounter32> table;
    
    public void execute
}*/
