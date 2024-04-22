using UnityEngine;

public static class MeshGenerator
{
    // Generate a mesh for a defined terrain
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _meshHeightCurve, int LevelOfDetail, bool useFlatShading)
    {

        AnimationCurve heightCurve = new AnimationCurve(_meshHeightCurve.keys);

        int meshSimplification = (LevelOfDetail == 0) ? 1 : LevelOfDetail * 2;

        int borderSize = heightMap.GetLength(0) ;
        int meshSize = borderSize - 2*meshSimplification ;
        int meshSizeUnsimplified = borderSize - 2;

        int verticesPerLine = (meshSize - 1) / meshSimplification + 1;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        // Generate Data
        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);
        int[,] verticesMap = CalculateVertextIndicesMap(borderSize, meshSimplification);

        for (int y = 0; y < borderSize; y += meshSimplification)
        {
            for (int x = 0; x < borderSize; x += meshSimplification)
            {
                int meshVertIndex = verticesMap[x, y];
                Vector2 percent = GetXYPercent(x, y, meshSize, meshSimplification);
                Vector3 vertexPosition = GetXYvertexPosition(x, y, percent, heightMap, meshSizeUnsimplified, topLeftX, topLeftZ, heightMultiplier, heightCurve);

                meshData.AddVertex(vertexPosition, percent, meshVertIndex);

                if (x < borderSize - 1 && y < borderSize - 1)
                {
                    int a = verticesMap[x, y];
                    int b = verticesMap[x + meshSimplification, y];
                    int c = verticesMap[x, y + meshSimplification];
                    int d = verticesMap[x + meshSimplification, y + meshSimplification];

                    meshData.AddTriangle(a,d,c);
                    meshData.AddTriangle(d,a,b);
                }

            }
        }

        meshData.ProcessMesh();

        return meshData;
    }

    // Create Triangles for a defined MeshData
    private static Vector3 GetXYvertexPosition(int x, int y, Vector2 percent, float[,] heightMap, float meshSize, float topX, float topZ, float heightMultiplier, AnimationCurve meshHeightCurve)
    {
        float height = meshHeightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
        Vector3 vertexPosition = new Vector3(topX + percent.x * meshSize, height, topZ - percent.y * meshSize);

        return vertexPosition;
    }

    // Create Uvs for a defined MeshData
    private static Vector2 GetXYPercent(int x, int y, int meshSize, int meshSimplification)
    {
        // For each uv in our mesh we add a Uvs
        Vector2 percent = new Vector2((x- meshSimplification) / (float)meshSize, (y-meshSimplification) / (float)meshSize);
        return percent;
    }

    // Calcultate for each vertex if it's an border vertex or an mesh vertex
    private static int[,] CalculateVertextIndicesMap(int borderSize, int levelOfDetail)
    {
        int[,] vertexIndicesMap = new int[borderSize, borderSize];
        int meshVert = 0;
        int borderVert = -1;

        for (int y = 0; y < borderSize; y += levelOfDetail)
        {
            for (int x = 0; x < borderSize; x += levelOfDetail)
            {
                bool isBorderedVertex = y == 0 || y == borderSize - 1 || x == 0 || x == borderSize - 1;
                if (isBorderedVertex)
                {
                    vertexIndicesMap[x, y] = borderVert;
                    borderVert--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVert;
                    meshVert++;
                }
            }
        }
        return vertexIndicesMap;
    }

}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    bool useFlatShading;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    public MeshData(int verticesPerLine, bool useFlatShading)
    {
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[verticesPerLine * 24];

        this.useFlatShading = useFlatShading;
    }

    public void AddVertex(Vector3 position, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVertices[-vertexIndex-1] = position;
        } else
        {
            vertices[vertexIndex] = position;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b , int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        } else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTIndex = i * 3;
            int vertexIndexA = triangles[normalTIndex];
            int vertexIndexB = triangles[normalTIndex + 1];
            int vertexIndexC = triangles[normalTIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTIndex];
            int vertexIndexB = borderTriangles[normalTIndex + 1];
            int vertexIndexC = borderTriangles[normalTIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int a, int b ,int c)
    {
        Vector3 pointA = (a < 0) ? borderVertices[-a-1] : vertices[a];
        Vector3 pointB = (b < 0) ? borderVertices[-b - 1] : vertices[b];
        Vector3 pointC = (c < 0) ? borderVertices[-c - 1] : vertices[c];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;  
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh()
    {
        if (useFlatShading)
        {
            FlatShading();
        } else
        {
            BakeNormals();
        }
    }

    void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    void FlatShading()
    {
        Vector3[] flatShadedVerticles = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for (int i = 0;i < triangles.Length;i++)
        {
            flatShadedVerticles[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVerticles;
        uvs = flatShadedUvs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices; 
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if (useFlatShading)
        {
            mesh.RecalculateNormals();
        } else
        {
            mesh.normals = bakedNormals;
        }


        return mesh;
    }

}
