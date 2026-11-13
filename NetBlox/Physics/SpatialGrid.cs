using System.Numerics;
using System.Runtime.CompilerServices;
using NetBlox.Instances.Parts;
using NetBlox.Runtime;

namespace NetBlox.Physics;

public record struct Vector3Int
{
    public int X;
    public int Y;
    public int Z;
}
public record struct QuaternionInt
{
    public int X;
    public int Y;
    public int Z;
    public int W;
}

public class SpatialGrid
{
    public readonly PhysicsSolver PhysicsSolver;

    public Vector3Int CellSize = new Vector3Int { X = 6, Y = 6, Z = 6 };

    private Dictionary<BasePart, LinkedList<Vector3Int>> objectToCells = [];
    private Dictionary<Vector3Int, LinkedList<BasePart>> cellToObjects = [];

    private Dictionary<BasePart, (Vector3Int, Vector3Int)> objectsToLastBoundingBoxes = [];

    public SpatialGrid(PhysicsSolver physicsSolver)
    {
        PhysicsSolver = physicsSolver;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void UpdatePart(BasePart basePart)
    {
        basePart.CalculateBoundingBox(out Vector3 min, out Vector3 max);

        Vector3Int minInt = new Vector3Int { X = (int)MathF.Floor(min.X), Y = (int)MathF.Floor(min.Y), Z = (int)MathF.Floor(min.Z) };
        Vector3Int maxInt = new Vector3Int { X = (int)MathF.Ceiling(max.X), Y = (int)MathF.Ceiling(max.Y), Z = (int)MathF.Ceiling(max.Z) };

        bool hasPreviousBoundingBox = objectsToLastBoundingBoxes.TryGetValue(basePart, out (Vector3Int, Vector3Int) lastbbox);

        if (hasPreviousBoundingBox && lastbbox.Item1 == minInt && lastbbox.Item2 == maxInt)
            return;

        if (!objectToCells.TryGetValue(basePart, out LinkedList<Vector3Int>? cellList))
        {
            cellList = [];
            objectToCells[basePart] = cellList;
        }

        LinkedListNode<Vector3Int>? first = cellList.First;

        while (first != null)
        {
            LinkedListNode<Vector3Int> current = first;
            first = first.Next;

            if (current.Value.X < minInt.X || current.Value.X > maxInt.X || 
                current.Value.Y < minInt.Y || current.Value.Y > maxInt.Y || 
                current.Value.Z < minInt.Z || current.Value.Z > maxInt.Z)
                cellList.Remove(current);
        }
        
        for (int x = minInt.X; x < maxInt.X; x++)
        {
            for (int y = minInt.Y; y < maxInt.Y; y++)
            {
                for (int z = minInt.Z; z < maxInt.Z; z++)
                {
                    if (!hasPreviousBoundingBox || x < lastbbox.Item1.X || x > lastbbox.Item2.X ||
                        y < lastbbox.Item1.Y || y > lastbbox.Item2.Y ||
                        z < lastbbox.Item1.Z || z > lastbbox.Item2.Z)
                    {
                        Vector3Int cell = new Vector3Int { X = x, Y = y, Z = z };
                        cellList.AddLast(cell);
                        if (!cellToObjects.TryGetValue(cell, out LinkedList<BasePart>? partList))
                        {
                            partList = [];
                            cellToObjects[cell] = partList;
                        }
                        partList.AddLast(basePart);
                    }
                }
            }
        }

        objectsToLastBoundingBoxes[basePart] = (minInt, maxInt);
    }
    public void RemovePart(BasePart basePart)
    {
        if (objectToCells.ContainsKey(basePart))
        {
            LinkedList<Vector3Int> allCells = objectToCells[basePart];
            foreach (Vector3Int goneCell in allCells)
            {
                cellToObjects[goneCell].Remove(basePart);
                if (cellToObjects[goneCell].Count == 0)
                    cellToObjects.Remove(goneCell);
            }

            objectToCells.Remove(basePart);
        }
    }
    public BasePart[]? GetImmediatelyNeighboringParts(BasePart basePart)
    {
        if (!objectToCells.TryGetValue(basePart, out LinkedList<Vector3Int>? allCells))
            return null;

        HashSet<BasePart> baseParts = [];
        List<Vector3Int> surroundingCells = new List<Vector3Int>(allCells.Count + (int)Math.Pow(allCells.Count, 1 / 3f) * 7);

        surroundingCells.AddRange(allCells);

        (Vector3Int min, Vector3Int max) =  objectsToLastBoundingBoxes[basePart];

        for (int x = min.X - 1; x <= max.X + 1; x++)
            for (int z = min.Z - 1; z <= max.Z + 1; z++)
            {
                surroundingCells.Add(new Vector3Int { X = x, Y = min.Y - 1, Z = z });
                surroundingCells.Add(new Vector3Int { X = x, Y = max.Y + 1, Z = z });
            }

        for (int x = min.X - 1; x <= max.X + 1; x++)
            for (int y = min.Y; y <= max.Y; y++)
            {
                surroundingCells.Add(new Vector3Int { X = x, Y = y, Z = min.Z - 1 });
                surroundingCells.Add(new Vector3Int { X = x, Y = y, Z = max.Z + 1 });
            }

        for (int z = min.Z; z <= max.Z; z++)
            for (int y = min.Y; y <= max.Y; y++)
            {
                surroundingCells.Add(new Vector3Int { X = min.X - 1, Y = y, Z = z });
                surroundingCells.Add(new Vector3Int { X = max.X + 1, Y = y, Z = z });
            }

        foreach (Vector3Int cell in surroundingCells)
        {
            if (cellToObjects.ContainsKey(cell))
                baseParts.UnionWith(cellToObjects[cell]);
        }

        baseParts.Remove(basePart);

        return baseParts.ToArray();
    }
}