// File for storing classes that perform calculations

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ManhattanSpiral
{
    private static bool Generated = false;

    private static int _length = 1000;
    private static (int x, int y)[] _values = new (int x, int y)[_length];

    public static int Length
    {
        get
        {
            if (!Generated)
            {
                Generate(ref _values);
            }
            return _length;
        }
    }

    public static (int x, int y)[] Values
    {
        get
        {
            if (!Generated)
            {
                Generate(ref _values);
            }
            return _values;
        }
    }

    public static (int x, int y) GetValue(int index)
    {
        if (!Generated)
        {
            Generate(ref _values);
        }
        return _values[index];
    }

    private static void Generate(ref (int, int)[] tuples)
    {
        if (tuples == null || tuples.Length == 0)
        {
            throw new System.ArgumentNullException();
        }

        tuples[0] = (0, 0);

        int ctr = 1;
        int direction = 0;
        int distance = 1;

        bool exit = false;

        while (!exit)
        {
            if (direction == 0)
            {
                // +Y direction to +X direction
                for (int i = 0; i < distance; i++)
                {
                    tuples[ctr] = (i, distance - i);
                    ctr++;
                    if (ctr == tuples.Length)
                    {
                        exit = true;
                        break;
                    }
                }
                direction = 1;
            }
            else if (direction == 1)
            {
                // +X direction to -Y direction
                for (int i = 0; i < distance; i++)
                {
                    tuples[ctr] = (distance - i, -i);
                    ctr++;
                    if (ctr == tuples.Length)
                    {
                        exit = true;
                        break;
                    }
                }
                direction = 2;
            }
            else if (direction == 2)
            {
                // -Y direction to -X direction
                for (int i = 0; i < distance; i++)
                {
                    tuples[ctr] = (-i, -distance + i);
                    ctr++;
                    if (ctr == tuples.Length)
                    {
                        exit = true;
                        break;
                    }
                }
                direction = 3;
            }
            else if (direction == 3)
            {
                // -X to +Y direction
                for (int i = 0; i < distance; i++)
                {
                    tuples[ctr] = (-distance + i, i);
                    ctr++;
                    if (ctr == tuples.Length)
                    {
                        exit = true;
                        break;
                    }
                }
                direction = 0;
                distance++;
            }
        }

        Generated = true;
        // return;
    }
}

public class LinearMap
{
    public float t0, t1, x0, x1;

    /// <summary>
    /// Get the value of x, clamped between x0 and x1 of the map, at the provided value of t.
    /// </summary>
    public float GetValueAt(float t)
    {
        return Mathf.Lerp(x0, x1, Mathf.InverseLerp(t0, t1, t));
    }

    public LinearMap(float t0, float t1, float x0, float x1)
    {
        this.t0 = t0;
        this.t1 = t1;
        this.x0 = x0;
        this.x1 = x1;
    }
}

public class MultiPerlinNoise
{
    private int Octaves { get; }
    private float Persistence { get; }
    private float Lacunarity { get; }
    private float MaxValue { get; }

    private Vector2[] RandomOrigins;

    public float GetValueAt(float x, float y)
    {
        float octaveWeight = 1f;
        float octaveScale = 1f;

        float currentValue = 0f;

        for (int i = 0; i < Octaves; i++)
        {
            currentValue += Mathf.PerlinNoise(RandomOrigins[i].x + x / octaveScale, RandomOrigins[i].y + y / octaveScale) * octaveWeight;
            octaveWeight *= Persistence;
            octaveScale *= Lacunarity;
        }

        return currentValue / MaxValue;
    }

    public MultiPerlinNoise(int octaves, float persistence, float lacunarity)
    {
        Octaves = octaves;
        Persistence = persistence;
        Lacunarity = lacunarity;
        RandomOrigins = new Vector2[octaves];

        float octaveWeight = 1f;
        for (int i = 0; i < Octaves; i++)
        {
            MaxValue += octaveWeight;
            octaveWeight *= persistence;

            RandomOrigins[i] = new Vector2(Random.Range(-999f, 999f), Random.Range(-999f, 999f));
        }
    }
}
