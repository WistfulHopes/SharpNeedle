// See https://aka.ms/new-console-template for more information

using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using SharpNeedle.Framework.HedgehogEngine.Bullet;
using SharpNeedle.IO;
using System.Numerics;
using UNIONBridge;
using Crc32 = System.IO.Hashing.Crc32;

if (args.Length < 2)
{
    Console.WriteLine("Missing args!");
    
    return;
}

HostFile? bulletMeshFile = HostFile.Open(args[0]);

if (bulletMeshFile == null)
{
    Console.WriteLine("Btmesh doesn't exist!");

    return;
}

BulletMesh bulletMesh = new();
bulletMesh.Read(bulletMeshFile);

BulletCollision bulletCollision = new();

uint vertexOffset = 0;

foreach (BulletShape shape in bulletMesh.Shapes)
{
    BulletCollision.BulletCollisionChunk chunk = new() { Vertices = shape.Vertices.ToList() };

    if (shape.Faces != null)
    {
        if (shape.IsConvex)
        {
            int[] faces = ConvexHullGenerator.GenerateHull(shape.Vertices);
            chunk.Indices.AddRange(faces.Select(x => (uint)x + vertexOffset));

            for (uint i = 0; i < faces.Length / 3; i++)
            {
                chunk.Faces.Add(i);
            }
        }
        else
        {
            SortedSet<CompTri> usedTriangles = [];

            for (int i = 0, f = 0; i < shape.Faces.Length; i += 3, f++)
            {
                chunk.Faces.Add((uint)f);
                
                uint t1 = shape.Faces[i];
                uint t2 = shape.Faces[i + 1];
                uint t3 = shape.Faces[i + 2];

                if(t1 == t2 || t2 == t3 || t3 == t1 || !usedTriangles.Add(new CompTri(t1, t2, t3)))
                {
                    continue;
                }
            
                chunk.Indices.Add(t1 + vertexOffset);
                chunk.Indices.Add(t2 + vertexOffset);
                chunk.Indices.Add(t3 + vertexOffset);
            }
        }
    }

    vertexOffset = (uint)shape.Vertices.Length;
    bulletCollision.Chunks.Add(chunk);
}

byte[][] bulletChunkData = new byte[bulletCollision.Chunks.Count][];
byte[] bulletChunkOffsetData = new byte[bulletCollision.Chunks.Count * 4];
int chunkDataCount = 16;

for (int i = 0; i < bulletCollision.Chunks.Count; i++)
{
    BulletCollision.BulletCollisionChunk chunk = bulletCollision.Chunks[i];
    
    byte[] vertexData = new byte[(chunk.Vertices.Count + 2) * 12];
    
    for (int j = 0; j < chunk.Vertices.Count; j++)
    {
        Vector3 vertex = chunk.Vertices[j];
        BitConverter.GetBytes(vertex.X).CopyTo(vertexData, j * 12);
        BitConverter.GetBytes(vertex.Y).CopyTo(vertexData, (j * 12) + 4);
        BitConverter.GetBytes(vertex.Z).CopyTo(vertexData, (j * 12) + 8);
    }
    
    byte[] indexData = new byte[chunk.Indices.Count * 4];
    for (int j = 0; j < chunk.Indices.Count; j++)
    {
        BitConverter.GetBytes(chunk.Indices[j]).CopyTo(indexData, j * 4);
    }
    
    byte[] unkData0 = new byte[32];
    byte[] unkData1 = new byte[8]; 
    BitConverter.GetBytes(1).CopyTo(unkData1, 4);
    
    byte[] aabbData = new byte[24];

    float minX = float.MaxValue;
    int indexMinX = -1;
    
    for (int j = 0; j < chunk.Vertices.Count; j++)
    {
        if (chunk.Vertices[j].X < minX)
        {
            minX = -chunk.Vertices[j].X;
            indexMinX = j;
        }
    }
    
    BitConverter.GetBytes(indexMinX).CopyTo(aabbData, 0);
    
    float minY = float.MaxValue;
    int indexMinY = -1;
    
    for (int j = 0; j < chunk.Vertices.Count; j++)
    {
        if (chunk.Vertices[j].Y < minY)
        {
            minY = chunk.Vertices[j].Y;
            indexMinY = j;
        }
    }
    
    BitConverter.GetBytes(indexMinY).CopyTo(aabbData, 4);
    
    float minZ = float.MaxValue;
    int indexMinZ = -1;
    
    for (int j = 0; j < chunk.Vertices.Count; j++)
    {
        if (chunk.Vertices[j].Z < minZ)
        {
            minZ = chunk.Vertices[j].Z;
            indexMinZ = j;
        }
    }
    
    BitConverter.GetBytes(indexMinZ).CopyTo(aabbData, 8);

    float maxX = float.MinValue;
    int indexMaxX = -1;
    
    for (int j = 0; j < chunk.Vertices.Count; j++)
    {
        if (chunk.Vertices[j].X > maxX)
        {
            maxX = -chunk.Vertices[j].X;
            indexMaxX = j;
        }
    }
    
    BitConverter.GetBytes(indexMaxX).CopyTo(aabbData, 12);
    
    float maxY = float.MinValue;
    int indexMaxY = -1;
    
    for (int j = 0; j < chunk.Vertices.Count; j++)
    {
        if (chunk.Vertices[j].Y > maxY)
        {
            maxY = chunk.Vertices[j].Y;
            indexMaxY = j;
        }
    }
    
    BitConverter.GetBytes(indexMaxY).CopyTo(aabbData, 16);
    
    float maxZ = float.MinValue;
    int indexMaxZ = -1;
    
    for (int j = 0; j < chunk.Vertices.Count; j++)
    {
        if (chunk.Vertices[j].Z > maxZ)
        {
            maxZ = chunk.Vertices[j].Z;
            indexMaxZ = j;
        }
    }
    
    BitConverter.GetBytes(indexMaxZ).CopyTo(aabbData, 20);

    byte[] chunkHeaderData = new byte[88];
    BitConverter.GetBytes(1).CopyTo(chunkHeaderData, 0);
    BitConverter.GetBytes(chunkHeaderData.Length + vertexData.Length + indexData.Length).CopyTo(chunkHeaderData, 4);
    BitConverter.GetBytes(chunk.Vertices.Count).CopyTo(chunkHeaderData, 8);
    BitConverter.GetBytes(12).CopyTo(chunkHeaderData, 12);
    BitConverter.GetBytes(chunkHeaderData.Length).CopyTo(chunkHeaderData, 16);
    BitConverter.GetBytes(2).CopyTo(chunkHeaderData, 24);
    BitConverter.GetBytes(16).CopyTo(chunkHeaderData, 28);
    BitConverter.GetBytes(chunkHeaderData.Length + vertexData.Length + indexData.Length + 24).CopyTo(chunkHeaderData, 32);
    BitConverter.GetBytes(2).CopyTo(chunkHeaderData, 36);
    BitConverter.GetBytes(4).CopyTo(chunkHeaderData, 40);
    BitConverter.GetBytes(chunkHeaderData.Length + vertexData.Length + indexData.Length + 56).CopyTo(chunkHeaderData, 44);
    BitConverter.GetBytes(2).CopyTo(chunkHeaderData, 48);
    BitConverter.GetBytes(2).CopyTo(chunkHeaderData, 52);
    BitConverter.GetBytes(chunkHeaderData.Length + vertexData.Length + indexData.Length).CopyTo(chunkHeaderData, 56);
    BitConverter.GetBytes(12).CopyTo(chunkHeaderData, 60);
    BitConverter.GetBytes(chunkHeaderData.Length + vertexData.Length).CopyTo(chunkHeaderData, 64);
    BitConverter.GetBytes(2).CopyTo(chunkHeaderData, 68);
    BitConverter.GetBytes(2).CopyTo(chunkHeaderData, 72);
    BitConverter.GetBytes(12).CopyTo(chunkHeaderData, 76);
    BitConverter.GetBytes(chunkHeaderData.Length + vertexData.Length + indexData.Length).CopyTo(chunkHeaderData, 80);
    BitConverter.GetBytes(2).CopyTo(chunkHeaderData, 84);
    
    bulletChunkData[i] = new byte[chunkHeaderData.Length + vertexData.Length + indexData.Length + 64];
    chunkHeaderData.CopyTo(bulletChunkData[i], 0);
    vertexData.CopyTo(bulletChunkData[i], chunkHeaderData.Length);
    indexData.CopyTo(bulletChunkData[i], chunkHeaderData.Length + vertexData.Length);
    aabbData.CopyTo(bulletChunkData[i], chunkHeaderData.Length + vertexData.Length + indexData.Length);
    unkData0.CopyTo(bulletChunkData[i], chunkHeaderData.Length + vertexData.Length + indexData.Length + 24);
    unkData1.CopyTo(bulletChunkData[i], chunkHeaderData.Length + vertexData.Length + indexData.Length + 56);
    
    BitConverter.GetBytes((bulletCollision.Chunks.Count * 4) + chunkDataCount).CopyTo(bulletChunkOffsetData, i * 4);
    chunkDataCount += chunkHeaderData.Length + vertexData.Length + indexData.Length + 64;
}

byte[] bulletCollisionData = new byte[(bulletCollision.Chunks.Count * 4) + chunkDataCount - 16];
bulletChunkOffsetData.CopyTo(bulletCollisionData, 0);
chunkDataCount = 0;

foreach (byte[] chunkData in bulletChunkData)
{
    chunkData.CopyTo(bulletCollisionData, (bulletCollision.Chunks.Count * 4) + chunkDataCount);
    chunkDataCount += chunkData.Length;
}

byte[] finalData = new byte[16 + bulletCollisionData.Length];

uint crc32 = Crc32.HashToUInt32(bulletCollisionData);

BitConverter.GetBytes(BulletCollision.Magic).CopyTo(finalData, 0);
BitConverter.GetBytes(bulletCollision.DataVersion).CopyTo(finalData, 4);
BitConverter.GetBytes(crc32).CopyTo(finalData, 8);
BitConverter.GetBytes(bulletCollision.Chunks.Count).CopyTo(finalData, 12);
bulletCollisionData.CopyTo(finalData, 16);

File.WriteAllBytes(args[1], finalData);

Console.WriteLine("Done!");
