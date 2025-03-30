using System;
using System.Numerics;
using VECS;
using VECS.ECS;
using VECS.ECS.Presentation;
using VECS.ECS.Transforms;

namespace COMP302
{
    public static class ExpirmentUtilities
    {
        public static void GetStats(DirectSubMesh[] highRes, DirectSubMesh[] lowRes)
        {
            CalculateVertsAndTris(lowRes, out uint Lverts, out uint Ltris);
            CalculateVertsAndTris(highRes, out uint Hverts, out uint Htris);

            Console.WriteLine(string.Format("Low res Mesh stats: Verts: {0}, Indices: {1}, Tris {2}", Lverts, Ltris, Ltris / 3));
            Console.WriteLine(string.Format("High res Mesh stats: Verts: {0}, Indices: {1}, Tris {2}", Hverts, Htris, Htris / 3));
            Console.WriteLine(string.Format("Reduction rates: Verts: {0}%, Indices: {1}%", (((float)Lverts / (float)Hverts) * 100f).ToString("00.00"), (((float)Ltris / (float)Htris) * 100f).ToString("00.00")));
        }

        public static Vector2 CalculateSimplificationRates(DirectSubMesh[] a, DirectSubMesh[] b)
        {
            CalculateVertsAndTris(a, out uint aV, out uint aT);
            CalculateVertsAndTris(b, out uint bV, out uint bT);

            return new((float)bV / (float)aV, (float)bT / (float)aT);
        }

        public static Vector2 CalculateResultantSimplificationRates(DirectSubMesh a, DirectSubMesh b)
        {
            uint aV = a.VertexCount;
            uint aT = a.IndexCount;
            uint bV = b.VertexCount;
            uint bT = b.IndexCount;

            return new((float)bV / (float)aV, (float)bT / (float)aT);
        }

        public static void CalculateVertsAndTris(DirectSubMesh[] lowRes, out uint verts, out uint indices)
        {
            verts = 0;
            indices = 0;
            for (int i = 0; i < lowRes[0].DirectMeshBuffer.SubMeshInfos.Length; i++)
            {
                verts += lowRes[0].DirectMeshBuffer.SubMeshInfos[i].VertexCount;
                indices += lowRes[0].DirectMeshBuffer.SubMeshInfos[i].IndexCount;
            }
        }

        public static int GetClosestTerrainGenSimplificationRate(int baseSubdivisions,float inputReductionRate, out float actualReductionRate)
        {
            int subdivisonsForB = baseSubdivisions;
            uint currentIndexCount = GetSubdivisonCounts(0, baseSubdivisions).Y;
            int dstFromTarget = int.MaxValue;
            uint targetIndexCount = (uint)MathF.Floor(currentIndexCount * inputReductionRate);
            for (int i = baseSubdivisions; i > 0; i--)
            {
                int newDstFromTarget = Math.Abs((int)targetIndexCount - (int)GetSubdivisonCounts(0, i).Y);

                if (newDstFromTarget < dstFromTarget)
                {
                    subdivisonsForB = i;
                    dstFromTarget = newDstFromTarget;
                }
                else if (newDstFromTarget > dstFromTarget)
                {
                    break;
                }
            }
            uint calculatedIndexCount = GetSubdivisonCounts(0, subdivisonsForB).Y;

            actualReductionRate = (float)calculatedIndexCount / (float)currentIndexCount;

            return subdivisonsForB;
        }

        private static Vector2UInt GetSubdivisonCounts(int tile, int subdivisions)
        {
            var vertices = MeshExtensions.GetVertsPerFace(subdivisions);
            var indices = MeshExtensions.GetIndicesPerFace(subdivisions);
            var currentIndices = Expirment.ReferenceMesh.SubMeshInfos[tile].IndexCount;

            vertices *= currentIndices / 3;
            indices *= currentIndices / 3;

            return new(vertices, indices);
        }

        public static DirectSubMesh[] GetMeshesInChildren(EntityManager entityManager, Entity parent)
        {
            Entity[] entityMeshes = entityManager.GetComponent<Children>(parent).Value;
            DirectSubMesh[] meshes = new DirectSubMesh[entityMeshes.Length];
            for (int j = 0; j < entityMeshes.Length; j++)
            {
                meshes[j] = DirectSubMesh.GetSubMeshAtIndex(entityManager.GetComponent<DirectSubMeshIndex>(entityMeshes[j]));
            }
            return meshes;
        }

        public static void SetUV_Y(float maxDev, DirectSubMesh[] aMeshes, DirectSubMesh[] bMeshes, DirectSubMesh[] cMeshes, DirectSubMesh[] dMeshes)
        {
            SetUV_Ys(maxDev, aMeshes[0].DirectMeshBuffer);
            SetUV_Ys(maxDev, bMeshes[0].DirectMeshBuffer);
            SetUV_Ys(maxDev, cMeshes[0].DirectMeshBuffer);
            SetUV_Ys(maxDev, dMeshes[0].DirectMeshBuffer);
        }

        public static void SetUV_Ys(float maxDev, DirectMeshBuffer directMesh)
        {
            var uvs = directMesh.GetFullVertexData<Vector2>(VertexAttribute.TexCoord0);
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i].Y = maxDev;
            }
            directMesh.FlushAll();
            DirectMeshBuffer.RecalcualteAllNormals(directMesh);
        }

    }
}
