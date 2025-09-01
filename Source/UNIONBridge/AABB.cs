namespace UNIONBridge;

using System.Numerics;

public class AABB
{
    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }
    public Vector3 Center => (Min + Max) / 2f;
    public Vector3 Size => Max - Min;

    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public static AABB FromVertices(List<Vector3> vertices)
    {
        if (vertices == null || vertices.Count == 0)
        {
            throw new ArgumentException("Vertices collection cannot be null or empty.");
        }

        Vector3 minPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxPoint = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (Vector3 vertex in vertices)
        {
            minPoint.X = Math.Min(minPoint.X, vertex.X);
            minPoint.Y = Math.Min(minPoint.Y, vertex.Y);
            minPoint.Z = Math.Min(minPoint.Z, vertex.Z);

            maxPoint.X = Math.Max(maxPoint.X, vertex.X);
            maxPoint.Y = Math.Max(maxPoint.Y, vertex.Y);
            maxPoint.Z = Math.Max(maxPoint.Z, vertex.Z);
        }

        return new AABB(minPoint, maxPoint);
    }
}
