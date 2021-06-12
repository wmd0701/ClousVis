using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    public string filePath;     // The file path of the height data.
    public float xDim;          // The real-world dimension of the width-axis.
    public float zDim;          // The real-world dimension of the height-axis.
    public float yMult;         // The height-data will be multiplied with this..

    // Start is called before the first frame update
    void Start()
    {
        // Read the terrain-data.
        string[] lines = System.IO.File.ReadAllLines(filePath);

        // Get the dimensions of the data.
        int X = lines.Length;                   // Nr. samples along dimension 1.
        int Z = lines[0].Split(',').Length;     // Nr. samples along dimension 2.

        // Create float array holding all the heights.
        float[,] elevation = new float[X, Z];

        // Transform height data from string to float.
        for (int x = 0; x < X; x++) {
            string line = lines[x];
            string[] lineSplit = line.Split(',');
            for (int z = 0; z < Z; z++) {
                elevation[x, z] = float.Parse(lineSplit[z]);
            }
        }

        /* The maximal size of a mesh is 2^16 vertices. 
         * Because we expect our mesh to be larger, we need to split it into potentially many meshes.
         * For that reason we will create many meshes:
         * __________________________________
         * | mesh 1 | mesh 2 | ... | mesh N | X
         * |________|________|_____|________| 
         *                 Z                    */

        // Find the largest number of rows along dimension X that fit into 2^16.
        int subZ = Mathf.FloorToInt(X * Z / Mathf.Pow(2, 16));
        int meshCount = Mathf.CeilToInt(Z / subZ); 

        // Create <meshCount> gameObjects to hold the required <MeshFilter>s and <Mesh>es.
        GameObject[] parents = new GameObject[meshCount];
        MeshFilter[] meshFilters = new MeshFilter[meshCount];
        MeshRenderer[] meshRenderers = new MeshRenderer[meshCount];
        Mesh[] meshes = new Mesh[meshCount];

        // Initialize.
        for (int i = 0; i < meshCount; i++) {
            parents[i] = new GameObject();
            meshRenderers[i] = parents[i].AddComponent<MeshRenderer>();
            meshRenderers[i].sharedMaterial = new Material(Shader.Find("Standard"));
            meshFilters[i] = parents[i].AddComponent<MeshFilter>();
        }

        // Increments per cell for dimensions 1 and 2.
        float dx = xDim / X;
        float dz = zDim / Z;

        Debug.Log(X);
        Debug.Log(Z);
        Debug.Log(meshCount);
        Debug.Log(subZ);

        // For each submesh ...
        for (int i = 0; i < meshCount; i++) {
            int zEnd = (int) Mathf.Min((i + 1) * subZ + 1, Z);
            float[,] subElevation = SubArray<float>(elevation, 0, X, i * subZ, zEnd);
            Vector3 origin = new Vector3(0.0f, 0.0f, i * subZ * dz); // Need to displace this mesh along the z-dimension.
            meshFilters[i].mesh = CreateMesh(subElevation, origin, dx, dz, yMult);
        }                
    }

    T[,] SubArray<T>(T[,] src, int iMin, int iMax, int jMin, int jMax) {
        /* Allows to get a two-dimensional subarray from array (numpy style).
         * Params:
         *      src: Original two-dimensional array.
         *      iMin: first row to be included.
         *      iMax: first row to excluded again (must be larger than minRow).
         *      jMin: first col to be included.
         *      jMax: first col to be excluded again (must be larger than minCol).
         * Returns:
         *      Two-dimensional array corresponding to the slice. */
         
        // Properties of the source array.
        int m_src = src.GetLength(0);
        int n_src = src.GetLength(1);

        // Properties of the destination array.
        int m_dst = iMax - iMin;
        int n_dst = jMax - jMin;

        T[,] dst = new T[m_dst, n_dst];
         
        // Copying.
        for (int i = iMin; i < iMax; i++) {
            Array.Copy(src, i * n_src + jMin, dst, (i - iMin) * n_dst, n_dst);
        }

        return dst;
    }

    Mesh CreateMesh(float[,] heights, Vector3 origin, float dx, float dz, float yMult) {
        /* Creates a mesh for the given array of elevation data.
         * Params:
         *      heights: Elevation data.
         *      origin: Origin at which the mesh is positioned.
         *      dx: Displacement per cell along dimension 1.
         *      dz: Displacement per cell along dimension 2.
         *      yMult: y-coordinates will be multiplied with this value.
         * Returns:
         *      Mesh corresponding to the given elevation data. */
        
        Mesh mesh = new Mesh();

        // Dimensions.
        int X = heights.GetLength(0);
        int Z = heights.GetLength(1);

        // Initialize array for vertices, triangles and UVs.
        Vector3[] verts = new Vector3[X * Z];
        int[] tris = new int[(X - 1) * (Z - 1) * 3 * 2];
        Vector2[] UVs = new Vector2[X * Z];

        // Fill in all vertices and UVs.
        for (int x = 0; x < X; x++) {
            for (int z = 0; z < Z; z++) {
                int flatIdx = x * Z + z; // The index for the flattened array.
                verts[flatIdx] = origin + new Vector3(x * dx, yMult * heights[x, z], z * dz);
                UVs[flatIdx] = new Vector3(x * dx, z * dz);
            }
        }

        int triIdx = 0; // Index to keep track of the current triangle.

        // Generate the triangle indices.
        for (int x = 0; x < X - 1; x++) {
            for (int z = 0; z < Z - 1; z++) {
                
                int flatIdx = x * Z + z; // Index for the flattened array.

                // Top-left triangle.
                tris[triIdx] = flatIdx;             // Top-left corner.
                tris[triIdx + 1] = flatIdx + 1;     // Right corner.
                tris[triIdx + 2] = flatIdx + Z;     // Bottom corner.
                triIdx += 3;                        // Next triangle.

                // Bottom-right triangle.
                tris[triIdx] = flatIdx + Z + 1;     // Bottom-right corner.
                tris[triIdx + 1] = flatIdx + Z;     // Left corner.
                tris[triIdx + 2] = flatIdx + 1;     // Top-cornern.
                triIdx += 3;                        // Next triangle.    
            }
        }

        // Set the properties of the mesh.
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = UVs;
        mesh.RecalculateNormals();

        return mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
