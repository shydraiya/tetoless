using System.Collections.Generic;
using UnityEngine;

public sealed class SevenBag
{
    private const int BagSize = 7;

    private readonly List<int> queue = new List<int>();

    public int Draw()
    {
        EnsureQueued(1);
        int value = queue[0];
        queue.RemoveAt(0);
        return value;
    }

    public int Peek(int offset)
    {
        EnsureQueued(offset + 1);
        return queue[offset];
    }

    private void EnsureQueued(int count)
    {
        while (queue.Count < count)
        {
            AddBag();
        }
    }

    private void AddBag()
    {
        int[] bag = new int[BagSize];
        for (int i = 0; i < bag.Length; i++)
        {
            bag[i] = i;
        }

        for (int i = bag.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (bag[i], bag[randomIndex]) = (bag[randomIndex], bag[i]);
        }

        queue.AddRange(bag);
    }
}
