namespace UNIONBridge;

using System.Numerics;

public class BulletCollision
{
    public struct BulletCollisionChunk
    {
        public List<Vector3> Vertices = [];
        public List<uint> Faces = [];
        public List<uint> Indices = [];

        public BulletCollisionChunk()
        {
        }
    }
    
    public static readonly uint Magic = 0xEC17AC07;
    public int DataVersion = 1;
    public int Hash;
    public List<BulletCollisionChunk> Chunks = [];
}