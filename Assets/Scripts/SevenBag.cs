using UnityEngine;

public sealed class SevenBag
{
    private readonly int[] bag = new int[7];
    private int index = 7;

    public int Draw()
    {
        if (index >= bag.Length)
        {
            Refill();
        }

        return bag[index++];
    }

    private void Refill()
    {
        for (int i = 0; i < bag.Length; i++)
        {
            bag[i] = i;
        }

        for (int i = bag.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (bag[i], bag[randomIndex]) = (bag[randomIndex], bag[i]);
        }

        index = 0;
    }
}
