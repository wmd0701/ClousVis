using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Slicer))]
[ExecuteInEditMode]
public class VectorFieldVisualizer : MonoBehaviour {

    public float displacement;              // Displacment at which grid points are seeded.
    public Gradient debugGradient;          // Gradient used to color the debug streamlines.
    public ComputeShader computeShader;     // Executes the streamline integration on the GPU.
    public int maxStreamlineCount;          // Maximal number of streamlines that will be calculated.
    public int iteratorSteps;               // Number of steps the of the integration.
    public float stepSize;                  // Stepsize of the iterator.
    public Texture3D vectorfieldTexture;    // 3D texture which contains the encoded vectorfield.
    public GameObject volumeBoundary;       // Boundary of the area where vectorfield gets visualized in.

    private Slicer slicer;                  // Slicer instance to get slice plane.
    private float maxStepDist;              // Maximal distance between two streamline-points (used for coloring).
    private float minStepDist;              // Minimal distance between tw0 streamline-points (used for coloring).
    private ComputeBuffer seedBuff;         // Buffer holding the seedpoints for the streamlines in each frame (GPU).
    private Vector3[] seedPoints;           // Array holding the seedpoints (CPU).
    private ComputeBuffer streamlineBuff;   // Buffer holding the positions of the streamlines (GPU).
    private Vector3[] streamlinePoints;     // Array holding the positions of the streamlines (CPU).    
    private int groupSize = 64;             // Number of threads run in a single group.
    private int groupCount;                 // Number of groups requires to calculate <maxStreamlineCount> streamlines.
    private Vector3 volumeBoundaryMin;      // Minimal point of volume-boundary.


    // Precomputation of the shader properties.
    static readonly int
        seedBuffId = Shader.PropertyToID("seedBuff"),
        maxStreamlineCountId = Shader.PropertyToID("maxStreamlineCount"),
        streamlineCountId = Shader.PropertyToID("streamlineCount"),
        streamlineBuffId = Shader.PropertyToID("streamlineBuff"),
        iteratorStepsId = Shader.PropertyToID("iteratorSteps"),
        stepSizeId = Shader.PropertyToID("stepSize"),
        vectorfieldTextureId = Shader.PropertyToID("vectorfieldTexture");

    // Start is called before the first frame update
    void Start() {
        slicer = GetComponent<Slicer>();
        groupCount = Mathf.CeilToInt(((float) maxStreamlineCount) / groupSize);
        seedPoints = new Vector3[maxStreamlineCount];
        streamlinePoints = new Vector3[maxStreamlineCount * iteratorSteps];
        maxStepDist = float.MinValue;
        minStepDist = float.MaxValue;
        volumeBoundaryMin = volumeBoundary.GetComponent<BoxCollider>().bounds.min;
    }

    void OnEnable() {

        /**
         * Initializing the buffer holding the seed-points for the streamlines.
         * All vectors are linearly layed-out.
         * All vector components are stored as 4-byte floats, resulting in 3 * 4 bytes per seed-point.
         * If <streamlineCount> < <maxStreamlineCount>, this allocated more memory than will be effectively used.
         * We make this compromise since re-allocating the buffer at each update to represent the variable number of
         * streamlines is costly.
         */
        seedBuff = new ComputeBuffer(maxStreamlineCount, 4 * 3);

        /**
         * Initializing the buffer holding the positions of the streamlines.
         * In the buffer, there are always <maxStreamlineCount> positions linearly layed-out corresponding to a single
         * step of the iterator for all streamlines.
         * As before, we might allocate more memory than needed for the sake of speed.
         */
        streamlineBuff = new ComputeBuffer(maxStreamlineCount * iteratorSteps, 4 * 3);
    }

    void OnDisable() {

        // Deallocating resources to prevent memory leaks.
        seedBuff.Release();
        seedBuff = null;
        streamlineBuff.Release();
        streamlineBuff = null;
    }

    // Update is called once per frame
    void Update() {

        // Get the current corner points of the slice.
        Vector3[] corners = slicer.GetSliceCorners();

        // Generate seed-points.
        (int, int) shape = GenerateGridSeedPoints(corners, ref seedPoints, displacement, volumeBoundaryMin);

        // Actual number of streamlines in this frame (< <maxStreamlineCount>).
        int streamlineCount = shape.Item1 * shape.Item2;

        // Update for GPU.
        UpdateFunctionOnGPU(streamlineCount);

        // Set the initial points.
        seedBuff.SetData(seedPoints);

        // Get data after GPU-calculation.
        streamlineBuff.GetData(streamlinePoints);

        // Draw streamlines for debugging.
        DrawStreamlinesDebugSimple(ref streamlinePoints, 
                                   iteratorSteps, 
                                   maxStreamlineCount, 
                                   streamlineCount, 
                                   volumeBoundaryMin);
    }

    /**
     * Generates seedpoints for streamlines.
     * @param corners: Corners of the slice to seed in.
     * @param du: Distance between gridpoints in the first dimension of the slice plane.
     * @param seedPoints: The which will contain the seedPoints.
     * @param dv: Distance between gridpoints in the second dimension of the slice plance.
     * @return: Tuple corresponding to the number of samples for each dimension of the plane.
     */
    private (int, int) GenerateGridSeedPoints(Vector3[] corners, 
                                              ref Vector3[] seedPoints, 
                                              float delta, 
                                              Vector3 volumeBoundaryMin) {
        Vector3 dispVecU = corners[1] - corners[0];
        Vector3 dispVecV = corners[2] - corners[0];
        float distU = Vector3.Magnitude(dispVecU);
        float distV = Vector3.Magnitude(dispVecV);
        int U = Mathf.FloorToInt(Vector3.Magnitude(dispVecU) / delta);
        int V = Mathf.FloorToInt(Vector3.Magnitude(dispVecV) / delta);

        // Check if a grid of dimensions <U * V> would contain to many points. If so, increase the delta.
        if (U * V > maxStreamlineCount) {
            delta = Mathf.Sqrt((distU * distV) / maxStreamlineCount);
            U = Mathf.FloorToInt(Vector3.Magnitude(dispVecU) / delta);
            V = Mathf.FloorToInt(Vector3.Magnitude(dispVecV) / delta);
        }
        Vector3 dispVecNormU = Vector3.Normalize(dispVecU);
        Vector3 dispVecNormV = Vector3.Normalize(dispVecV);

        /**
         * Fill in seedPoints as far as possible. If the number of seed-points exceeds the capacity of the <seedPoints>
         * (is more than <maxStreamlineCount>) this function will throw an exception.
         */
        if (U * V > maxStreamlineCount) {
            string exceptionMessage = "The number of seed-points exceeds the maximal number of streamlines you have " +
                                      "requested. Please increase the maxStreamlineCount or increase displacement.";
            throw new System.Exception(exceptionMessage);
        } 
        for (int u = 0; u < U; u++) {
            for (int v = 0; v < V; v++) {
                Vector3 rawVec = corners[0] + u * delta * dispVecNormU + v * delta * dispVecNormV;
                seedPoints[u * V + v] = SwapYZ(rawVec - volumeBoundaryMin);
            }
        }

        return (U, V);
    }

    /**
     * For visual debugging.
     */
    void OnDrawGizmos() {
        Vector3[] corners = slicer.GetSliceCorners();
        (int, int) shape = GenerateGridSeedPoints(corners, ref seedPoints, displacement, volumeBoundaryMin);
        int U = shape.Item1;
        int V = shape.Item2;
        for (int u = 0; u < U; u++) {
            for (int v = 0; v < V; v++) {
                Gizmos.color = Color.blue + ((float) u) / U * Color.red + ((float) v) / V * Color.green;
                Gizmos.DrawSphere(seedPoints[u * V + v], 2.0f);
            }
        }
    }

    /**
     * Visual debugging of streamlines.
     */
    void DrawStreamlinesDebug(ref Vector3[] positions, 
                              int iteratorSteps, 
                              int maxStreamlineCount, 
                              int streamlineCount,
                              Vector3 volumeBoundaryMin) {
        for (int i = 0; i < streamlineCount; i++) {
            for (int j = 1; j < iteratorSteps; j++) {
                Vector3 last = SwapYZ(positions[i + (j - 1) * maxStreamlineCount]) + volumeBoundaryMin;
                Vector3 curr = SwapYZ(positions[i + j * maxStreamlineCount]) + volumeBoundaryMin;
                float distance = Vector3.Magnitude(last - curr);
                maxStepDist = distance > maxStepDist ? distance : maxStepDist;
                minStepDist = distance < minStepDist ? distance : minStepDist;
                float t = (distance - minStepDist) / (maxStepDist - minStepDist);
                Debug.DrawLine(last, curr, debugGradient.Evaluate(Mathf.Min(t, 1.0f)));
            }
        }
    }

    void DrawStreamlinesDebugSimple(ref Vector3[] positions, 
                                    int iteratorSteps, 
                                    int maxStreamlineCount, 
                                    int streamlineCount,
                              Vector3 volumeBoundaryMin) {
        for (int i = 0; i < streamlineCount; i++) {
            for (int j = 1; j < iteratorSteps; j++) {
                Vector3 last = SwapYZ(positions[i + (j - 1) * maxStreamlineCount]) + volumeBoundaryMin;
                Vector3 curr = SwapYZ(positions[i + j * maxStreamlineCount]) + volumeBoundaryMin;
                float distance = Vector3.Magnitude(last - curr);
                float t = (float) i / streamlineCount;
                Debug.DrawLine(last, curr, debugGradient.Evaluate(Mathf.Min(t, 1.0f)));
            }
        }
    }

    /**
     * Convenience method that swaps the y- and z-component of a vector (for portability between Unity and GPU).
     */
    Vector3 SwapYZ(Vector3 vec) {
        return new Vector3(vec.x, vec.z, vec.y);
    }

    void UpdateFunctionOnGPU(int streamlineCount) {
        int kernelId = computeShader.FindKernel("Iterator");
        computeShader.SetInt(maxStreamlineCountId, maxStreamlineCount);
        computeShader.SetInt(streamlineCountId, streamlineCount);
        computeShader.SetInt(iteratorStepsId, iteratorSteps);
        computeShader.SetFloat(stepSizeId, stepSize);
        computeShader.SetTexture(kernelId, vectorfieldTextureId, vectorfieldTexture, 0);
        computeShader.SetBuffer(kernelId, streamlineBuffId, streamlineBuff);
        computeShader.SetBuffer(kernelId, seedBuffId, seedBuff);
        computeShader.Dispatch(kernelId, groupCount, 1, 1);
    }
}
