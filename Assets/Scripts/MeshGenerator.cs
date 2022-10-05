using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] borderedSizeMap, float borderedSizeMultiplayer, AnimationCurve _borderedSizeCurve,int levelOfDetails)
    {
        AnimationCurve borderedSizeCurve = new AnimationCurve(_borderedSizeCurve.keys);

        int meshSimplificationIncrement = (levelOfDetails == 0) ? 1 : levelOfDetails * 2;

        int borderedSize = borderedSizeMap.GetLength(0);
        int meshSize = borderedSize - 2*meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verticiesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticiesPerLine);

        int[,] vertexIndiecesMap = new int[borderedSize,borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int i = 0; i < borderedSize; i += meshSimplificationIncrement)
        {
            for (int j = 0; j < borderedSize; j += meshSimplificationIncrement)
            {
                bool isBorderVertex = i == 0 || i == borderedSize - 1 || j == 0 || j == borderedSize - 1;

                if (isBorderVertex)
                {
                    vertexIndiecesMap[j, i] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndiecesMap[j, i] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
        for (int i=0; i < borderedSize; i+= meshSimplificationIncrement)
        {
            for(int j = 0; j < borderedSize; j+= meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndiecesMap[j, i];
                Vector2 percent = new Vector2((j - meshSimplificationIncrement) / (float)meshSize, (i - meshSimplificationIncrement) / (float)meshSize);
                float height = borderedSizeCurve.Evaluate(borderedSizeMap[j, i]) * borderedSizeMultiplayer;
                Vector3 vertexPosition = new Vector3(topLeftX+percent.x* meshSizeUnsimplified, height , topLeftZ-percent.y* meshSizeUnsimplified);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                if(j < borderedSize-1 && i < borderedSize - 1)
                {
                    int a = vertexIndiecesMap[j, i];
                    int b = vertexIndiecesMap[j + meshSimplificationIncrement, i];
                    int c = vertexIndiecesMap[j, i + meshSimplificationIncrement];
                    int d = vertexIndiecesMap[j + meshSimplificationIncrement, i + meshSimplificationIncrement];
                    meshData.AddTriangle(a,d,c);
                    meshData.AddTriangle(d,a,b);
                }
                vertexIndex++;
            }
        }

        meshData.BakeNormals();

        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertecies;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] borderVertecies;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    public MeshData(int verteciesPerLine)
    {
        vertecies = new Vector3[verteciesPerLine * verteciesPerLine];
        uvs = new Vector2[verteciesPerLine * verteciesPerLine];
        triangles = new int[(verteciesPerLine - 1) * (verteciesPerLine - 1) * 6];

        borderVertecies = new Vector3[verteciesPerLine * 4 + 4];
        borderTriangles = new int[24*verteciesPerLine];
    }
    public void AddVertex(Vector3 vertexPosition, Vector2 uv,int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVertecies[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            vertecies[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }
    public void AddTriangle(int a,int b,int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }
    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertecies.Length];
        int triangleCount = triangles.Length / 3;
        for(int i = 0; i < triangleCount; ++i)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; ++i)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)  vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0)  vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0)  vertexNormals[vertexIndexC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; ++i)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA,int indexB,int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertecies[-indexA - 1] : vertecies[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertecies[-indexB - 1] : vertecies[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertecies[-indexC - 1] : vertecies[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }
    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertecies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = bakedNormals;
        return mesh;
    }
}
