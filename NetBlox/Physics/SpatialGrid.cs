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

    public Vector3Int CellSize = new Vector3Int { X = 3, Y = 3, Z = 3 };

    private Stack<HashSet<Vector3Int>> objectPool_HashSetVector3Int = [];
    private Dictionary<BasePart, HashSet<Vector3Int>> objectToCells = [];
    private Dictionary<Vector3Int, HashSet<BasePart>> cellToObjects = [];

    private Dictionary<BasePart, (Vector3Int, Vector3Int)> objectsToLastBoundingBoxes = [];

    public SpatialGrid(PhysicsSolver physicsSolver)
    {
        PhysicsSolver = physicsSolver;
    }

    private HashSet<Vector3Int> ObjectPool_HashSetVector3Int_Get()
    {
        if (objectPool_HashSetVector3Int.Count > 0)
            return objectPool_HashSetVector3Int.Pop();
        return new HashSet<Vector3Int>();
    }
    private void ObjectPool_HashSetVector3Int_Return(HashSet<Vector3Int> obj)
    {
        objectPool_HashSetVector3Int.Push(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void UpdatePart(BasePart basePart)
    {
        basePart.CalculateBoundingBox(out Vector3 min, out Vector3 max);

        Vector3Int minInt = new Vector3Int { X = (int)MathF.Floor(min.X), Y = (int)MathF.Floor(min.Y), Z = (int)MathF.Floor(min.Z) };
        Vector3Int maxInt = new Vector3Int { X = (int)MathF.Ceiling(max.X), Y = (int)MathF.Ceiling(max.Y), Z = (int)MathF.Ceiling(max.Z) };

        if (objectsToLastBoundingBoxes.TryGetValue(basePart, out (Vector3Int, Vector3Int) lastbbox) && lastbbox.Item1 == minInt && lastbbox.Item2 == maxInt)
            return;
        
        objectsToLastBoundingBoxes[basePart] = (minInt, maxInt);

        HashSet<Vector3Int> previousCells = ObjectPool_HashSetVector3Int_Get();
        HashSet<Vector3Int> currentCells = ObjectPool_HashSetVector3Int_Get();

        if (!objectToCells.ContainsKey(basePart))
            objectToCells[basePart] = [];

        previousCells.Clear();
        previousCells.UnionWith(objectToCells[basePart]);

        currentCells.Clear();

        for (int x = minInt.X; x < maxInt.X; x += CellSize.X)
            for (int y = minInt.Y; y < maxInt.Y; y += CellSize.Y)
                for (int z = minInt.Z; z < maxInt.Z; z += CellSize.Z)
                    currentCells.Add(new Vector3Int { X = x, Y = y, Z = z });
                    
        HashSet<Vector3Int> goneCells = ObjectPool_HashSetVector3Int_Get();
        goneCells.Clear();
        goneCells.UnionWith(previousCells);
        goneCells.ExceptWith(currentCells);

        HashSet<Vector3Int> newCells = ObjectPool_HashSetVector3Int_Get();
        newCells.Clear();
        newCells.UnionWith(currentCells);
        newCells.ExceptWith(previousCells);

        if (goneCells.Count > 0)
        {
            objectToCells[basePart].ExceptWith(goneCells);
            foreach (Vector3Int goneCell in goneCells)
            {
                if (cellToObjects.ContainsKey(goneCell))
                {
                    cellToObjects[goneCell].Remove(basePart);
                    if (cellToObjects[goneCell].Count == 0)
                        cellToObjects.Remove(goneCell);
                }
            }
        }

        if (newCells.Count > 0)
        {
            objectToCells[basePart].UnionWith(newCells);
            foreach (Vector3Int newCell in newCells)
            {
                if (!cellToObjects.ContainsKey(newCell))
                    cellToObjects[newCell] = [];
                cellToObjects[newCell].Add(basePart);
            }
        }

        ObjectPool_HashSetVector3Int_Return(previousCells);
        ObjectPool_HashSetVector3Int_Return(currentCells);
        ObjectPool_HashSetVector3Int_Return(goneCells);
        ObjectPool_HashSetVector3Int_Return(newCells);
    }
    public void RemovePart(BasePart basePart)
    {
        if (objectToCells.ContainsKey(basePart))
        {
            HashSet<Vector3Int> allCells = objectToCells[basePart];
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
        if (!objectToCells.ContainsKey(basePart))
            return null;

        HashSet<BasePart> baseParts = [];
        HashSet<Vector3Int> surroundingCells = ObjectPool_HashSetVector3Int_Get();

        surroundingCells.Clear();

        foreach (Vector3Int cell in objectToCells[basePart])
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                        surroundingCells.Add(new Vector3Int 
                            { X = cell.X + x, Y = cell.Y + y, Z = cell.Z + z });
        }

        foreach (Vector3Int cell in surroundingCells)
        {
            if (cellToObjects.ContainsKey(cell))
                baseParts.UnionWith(cellToObjects[cell]);
        }

        baseParts.Remove(basePart);

        ObjectPool_HashSetVector3Int_Return(surroundingCells);
        return baseParts.ToArray();
    }
}