using UnityEngine;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;

// Code adapted from https://github.com/matthias-research/pages/blob/master/tenMinutePhysics/15-selfCollision.html
public class SpatialHashGrid
{
    // The side length of a cube / cell in the regular grid
    private float _cellSize;
    // The amount of entries of our hashtable
    private int _tableSize;
    // hashtable, counts how many objects fall into the same table cell. 
    // The amount of objects stored in cell i is given by _table[i + 1] - _table[i]
    // and _table[i] encodes the index into the _gridEntries array to the location of the first particleIndex of the Particles stored in _table[i]
    private int[] _table;
    // Actual amount of objects contained in gridEntries
    private int _numObjects;
    // list containing indices of all objects in the grid. The indices of objects that fall into the same table entry are adjacent in this list
    private int[] _gridEntries;

    public SpatialHashGrid(float cellSize, int maxNumObjects)
    {
        _cellSize = cellSize;
        _tableSize = 2 * maxNumObjects;
        _table = new int[_tableSize + 1];
        _gridEntries = new int[maxNumObjects]; 
    }

    public static int Hash(int3 gridPos, int tableSize)
    {
        unchecked
        {
            return math.abs((gridPos.x * 92837111) ^ (gridPos.y * 689287499) ^ (gridPos.z * 283923481)) % tableSize;
        }
    }

    public static int3 GridPosition(float3 position, float cellsize)
    {
        return new int3(math.floor(position / cellsize));
    }

    public void Create(Particle[] particles)
    {
        if (particles.Length > _gridEntries.Length)
            Debug.LogWarning("Number of particles exceeds allocated resources for the hash grid");
        
        _numObjects = math.min(particles.Length, _gridEntries.Length);
        // reset table
        Array.Fill(_table, 0);
        Array.Fill(_gridEntries, 0);

        // count the particles that fall into each table entry
        for (int i = 0; i < _numObjects; i++)
        {
            int tableIndex = Hash(GridPosition(particles[i].X, _cellSize), _tableSize);
            _table[tableIndex]++;
        }

        // compute prefix sum
        for (int i = 1; i < _tableSize; i++)
            _table[i] = _table[i - 1] + _table[i];

        _table[_tableSize] = _table[_tableSize - 1]; // guard

        for (int i = 0; i < _numObjects; i++)
        {
            int tableIndex = Hash(GridPosition(particles[i].X, _cellSize), _tableSize);
            _table[tableIndex]--;
            _gridEntries[_table[tableIndex]] = i;
        }
    }

    /// <summary>
    /// Returns the particle indices of the Particles closer than maxDistance to pos
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="maxDistance"></param>
    /// <returns></returns>
    public List<int> Query(Vector3 pos, float maxDistance)
    {
        Vector3 maxDistVector = new(maxDistance, maxDistance, maxDistance);
        int3 minPos = GridPosition(pos - maxDistVector, _cellSize);
        int3 maxPos = GridPosition(pos + maxDistVector, _cellSize);

        List<int> neighbours = new();
        for (int xi = minPos.x; xi <= maxPos.x; xi++)
        {
            for (int yi = minPos.y; yi <= maxPos.y; yi++)
            {
                for (int zi = minPos.z; zi <= maxPos.z; zi++)
                {
                    int tableIndex = Hash(GridPosition(new(xi, yi, zi), _cellSize), _tableSize);
                    int start = _table[tableIndex];
                    int end = _table[tableIndex + 1];

                    for (int i = start; i <= end; i++)
                        neighbours.Add(_gridEntries[i]);
                }
            }
        }

        return neighbours;
    }
}
