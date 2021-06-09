using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Slicer))]
[ExecuteInEditMode]
public class VectorFieldVisualizer : MonoBehaviour {

    public float displacement;          // Displacment at which grid points are seeded.
    public Gradient debugGradient;      // Gradient used to color the debug streamlines.
    public ComputeShader computeShader; // Executes the streamline integration on the GPU.
    public int maxStreamlineCount;      // Maximal number of streamlines that will be calculated.
    public int iteratorSteps;           // Number of steps the of the integration.

    private Slicer slicer;              // Slicer instance to get slice plane.
    private float maxSegmentDist;       // Maximal distance between to streamline-points (used for coloring):


    // Precomputation of the shader properties.
    static readonly int
        initialPositionsBufferId = Shader.PropertyToID("InitialPositionsBuffer"),
        streamLineCountId = Shader.PropertyToID("StreamLineCount"),
        trajectoryBufferId = Shader.PropertyToID("TrajectoryBuffer"),
        streamLineLengthId = Shader.PropertyToID("StreamLineLength"),
        stepSizeId = Shader.PropertyToID("StepSize"),
        vectorFieldId = Shader.PropertyToID("VectorFieldTexture"),
        dxId = Shader.PropertyToID("dx"),
        dyId = Shader.PropertyToID("dy"),
        dzId = Shader.PropertyToID("dz");

    // Start is called before the first frame update
    void Start() {
        slicer = GetComponent<Slicer>();
    }

    void OnEnable() {

    }

    void OnDisable() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }

    /**
     * Generates seedpoints for streamlines.
     * @param corners: Corners of the slice to seed in.
     * @param du: Distance between gridpoints in the first dimension of the slice plane.
     * @param dv: Distance between gridpoints in the second dimension of the slice plance.
     * @return: Vector3[,] where each entry is a seedpoint.
     */
    private Vector3[,] GetGridSeedPoints(Vector3[] corners, float du, float dv) {
        Vector3 dispVecU = corners[1] - corners[0];
        Vector3 dispVecV = corners[2] - corners[0];
        int countU = Mathf.FloorToInt(Vector3.Magnitude(dispVecU) / du);
        int countV = Mathf.FloorToInt(Vector3.Magnitude(dispVecV) / dv);
        Vector3 dispVecNormU = Vector3.Normalize(dispVecU);
        Vector3 dispVecNormV = Vector3.Normalize(dispVecV);
        Vector3[,] seedPoints = new Vector3[countU, countV];
        for (int u = 0; u < countU; u++) {
            for (int v = 0; v < countV; v++) {
                seedPoints[u, v] = corners[0] + u * du * dispVecNormU + v * dv * dispVecNormV;
            }
        }  
        return seedPoints;
    }

    /**
     * For visual debugging.
     */
    void OnDrawGizmos() {
        Vector3[] corners = slicer.GetSliceCorners();
        Vector3[,] seedPoints = GetGridSeedPoints(corners, displacement, displacement);
        int U = seedPoints.GetLength(0);
        int V = seedPoints.GetLength(1);
        for (int u = 0; u < seedPoints.GetLength(0); u++) {
            for (int v = 0; v < seedPoints.GetLength(1); v++) {
                Gizmos.color = Color.blue + ((float) u) / U * Color.red + ((float) v) / V * Color.green;
                Gizmos.DrawSphere(seedPoints[u, v], 2.0f);
            }
        }
    }

    /**
     * Visual debugging of streamlines.
     */
    void DrawStreamlinesDebug(Vector3[] positions, int streamlineLength, int streamlineCount) {
        for (int i = 0; i < streamlineCount; i++) {
            for (int j = 1; j < streamlineLength; j++) {
                Vector3 last = positions[i + (j - 1) * streamlineCount];
                Vector3 curr = positions[i + j * streamlineCount];
                float distance = Vector3.Magnitude(last - curr);
                maxSegmentDist = distance > maxSegmentDist ? distance : maxSegmentDist;
                Debug.DrawLine(last, curr, debugGradient.Evaluate(Mathf.Max(distance / maxSegmentDist, 1.0f)));
            }
        }
    }

    /**
     * Convenience method that swaps the y- and z-component of a vector (for portability between Unity and GPU).
     */
    Vector3 SwapYZ(Vector3 vec) {
        return new Vector3(vec.x, vec.z, vec.y);
    }

    void UpdateFunctionOnGPU() {
        var kernelId = computeShader.FindKernel("StepThroughStreamLine");
        computeShader.SetInt(streamLineCountId, streamLineCount);
        computeShader.SetInt(streamLineLengthId, streamLineLength);
        computeShader.SetFloat(stepSizeId, stepSize);
        computeShader.SetTexture(kernelId, vectorFieldId, vectorField, 0);
        computeShader.SetBuffer(kernelId, trajectoryBufferId, trajectoryBuffer);
        computeShader.SetBuffer(kernelId, initialPositionsBufferId, initialPositionsBuffer);
        computeShader.Dispatch(kernelId, Mathf.CeilToInt(streamLineCount / 64.0f), 1, 1);
    }
}
