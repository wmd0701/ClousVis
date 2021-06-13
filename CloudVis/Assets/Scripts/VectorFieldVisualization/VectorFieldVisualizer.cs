using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Slicer))]
// [ExecuteInEditMode]
public class VectorFieldVisualizer : MonoBehaviour {

    public float displacement;                  // Displacment at which grid points are seeded.
    public Gradient gradient;                   // Gradient used to color the debug streamlines.
    public ComputeShader computeShader;         // Executes the streamline integration on the GPU.
    public int maxStreamlineCount;              // Maximal number of streamlines that will be calculated.
    int iteratorSteps {get {return template.segmentCount + 1;}}                  // Number of steps the of the integration.
    public float stepSize;                      // Stepsize of the iterator.
    public Texture3D vectorfieldTexture;        // 3D texture which contains the encoded vectorfield.
    public GameObject volumeBoundary;           // Boundary of the area where vectorfield gets visualized in.
    public Material material;                   // Material of the tubes.
    public float radius;                        // Radius of the tubes.
    public TubeTemplate template;               // Template mesh for tubes.

    private Slicer slicer;                      // Slicer instance to get slice plane.
    private float maxStepDist;                  // Maximal distance between two streamline-points (used for coloring).
    private float minStepDist;                  // Minimal distance between tw0 streamline-points (used for coloring).
    private ComputeBuffer seedBuffer;           // Buffer holding the seedpoints for the streamlines in each frame (GPU).
    private ComputeBuffer streamlineBuffer;     // Buffer holding the positions of the streamlines (GPU).
    private ComputeBuffer normalBuffer;         // Buffer for the normals of the streamlines.
    private ComputeBuffer tangentBuffer;        // Buffer for the tangents of the streamlines.
    private ComputeBuffer segmentLengthBuffer;  // Length of segments (used for coloring).
    private ComputeBuffer gradientColors;       // Colors used to color the streamlines.
    private ComputeBuffer drawArgsBuffer;       // Holds arguments to tube shader.
    private ComputeBuffer maxLengthBuffer;      // Holds the maximal segment length for each streamline.
    private ComputeBuffer minLengthBuffer;      // Holds the minimal segment length for each streamline.
    private MaterialPropertyBlock props;        // Holds material properties.
    private Vector3[] seedPoints;               // Array holding the seedpoints (CPU).
    private Vector3[] streamlinePoints;         // Array holding the positions of the streamlines (CPU).    
    private int groupSize = 64;                 // Number of threads run in a single group.
    private int groupCount;                     // Number of groups requires to calculate <maxStreamlineCount> streamlines.
    private Vector3 volumeBoundaryMin;          // Minimal point of volume-boundary.
    private bool materialIsCloned;              // Whether material is cloned.
    public static bool enabled;                 // Whether show tubes or not


    // Precomputation of the shader properties.
    static readonly int

        // IDs for the Integration shader.
        seedBufferId = Shader.PropertyToID("seedBuffer"),
        streamlineBufferId = Shader.PropertyToID("streamlineBuffer"),
        tangentBufferId = Shader.PropertyToID("tangentBuffer"),
        normalBufferId = Shader.PropertyToID("normalBuffer"),
        streamlineRecBufferId = Shader.PropertyToID("streamlineRecBuffer"),
        maxStreamlineCountId = Shader.PropertyToID("maxStreamlineCount"),
        streamlineCountId = Shader.PropertyToID("streamlineCount"),
        segmentLengthBufferId = Shader.PropertyToID("segmentLengthBuffer"),
        minLengthBufferId = Shader.PropertyToID("minLengthBuffer"),
        maxLengthBufferId = Shader.PropertyToID("maxLengthBuffer"),
        iteratorStepsId = Shader.PropertyToID("iteratorSteps"),
        stepSizeId = Shader.PropertyToID("stepSize"),
        vectorfieldTextureId = Shader.PropertyToID("vectorfieldTexture"),
        minLenId = Shader.PropertyToID("minLen"),
        maxLenId = Shader.PropertyToID("maxLen"),
        volumeBoundaryMinId = Shader.PropertyToID("volumeBoundaryMin"),

        // IDs for the tube shader.
        _GradientColorsId = Shader.PropertyToID("_GradientColors"),
        _RadiusId = Shader.PropertyToID("_Radius"),
        _LocalToWorldId = Shader.PropertyToID("_LocalToWorld"),
        _WorldToLocalId = Shader.PropertyToID("_WorldToLocal"),
        _StreamlineBufferId = Shader.PropertyToID("_StreamlineBuffer"),
        _TangentBufferId = Shader.PropertyToID("_TangentBuffer"),
        _NormalBufferId = Shader.PropertyToID("_NormalBuffer"),
        _IntegratorStepsId = Shader.PropertyToID("_IntegratorSteps"),
        _MaxStreamlineCountId = Shader.PropertyToID("_MaxStreamlineCount"),
        _StreamlineCountId = Shader.PropertyToID("_StreamlineCount"),
        _VolumeBoundaryMinId = Shader.PropertyToID("_VolumeBoundaryMin"),
        _SegmentLengthBufferId = Shader.PropertyToID("_SegmentLengthBuffer"),
        _MaxLengthBufferId = Shader.PropertyToID("_MaxLengthBuffer"),
        _MinLenthBufferId = Shader.PropertyToID("_MinLengthBuffer");



    // Start is called before the first frame update
    void Start() {
        slicer = GetComponent<Slicer>();
        groupCount = Mathf.CeilToInt(((float) maxStreamlineCount) / groupSize);
        seedPoints = new Vector3[maxStreamlineCount];
        streamlinePoints = new Vector3[maxStreamlineCount * iteratorSteps];
        maxStepDist = float.MinValue;
        minStepDist = float.MaxValue;
        volumeBoundaryMin = volumeBoundary.GetComponent<BoxCollider>().bounds.min;

        // Instantiation of tube-rendering-related stuff.
        drawArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        drawArgsBuffer.SetData(new uint[5] {template.mesh.GetIndexCount(0), 
                                            (uint) maxStreamlineCount, 
                                            0, 0, 0});
        
        // Only used to avoid instancing bug.
        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", Random.value);

        // Clone material (TODO: Find out why).
        material = new Material(material);
        material.name += "_cloned";
        materialIsCloned = true;
        ExtractGradientColors();
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
        seedBuffer = new ComputeBuffer(maxStreamlineCount, 4 * 3);

        /**
         * Initializing the buffer holding the positions of the streamlines.
         * In the buffer, there are always <maxStreamlineCount> positions linearly layed-out corresponding to a single
         * step of the iterator for all streamlines.
         * As before, we might allocate more memory than needed for the sake of speed.
         */
        streamlineBuffer = new ComputeBuffer(maxStreamlineCount * iteratorSteps, 4 * 3);

        // Buffers holding tangents and normals, same layout and size as <streamlineBuffer>.
        tangentBuffer = new ComputeBuffer(maxStreamlineCount * iteratorSteps, 4 * 3);
        normalBuffer = new ComputeBuffer(maxStreamlineCount * iteratorSteps, 4 * 3);
        segmentLengthBuffer = new ComputeBuffer(maxStreamlineCount * iteratorSteps, 4);
        gradientColors = new ComputeBuffer(8, 4 * 3);
        maxLengthBuffer = new ComputeBuffer(maxStreamlineCount, 4);
        minLengthBuffer = new ComputeBuffer(maxStreamlineCount, 4);
    }

    void OnDisable() {

        // Deallocating resources to prevent memory leaks.
        FreeBuffer(seedBuffer);
        FreeBuffer(streamlineBuffer);
        FreeBuffer(tangentBuffer);
        FreeBuffer(normalBuffer);
        FreeBuffer(segmentLengthBuffer);
        FreeBuffer(gradientColors);
        FreeBuffer(maxLengthBuffer);
        FreeBuffer(minLengthBuffer);
        FreeBuffer(drawArgsBuffer);
    }

    // Update is called once per frame
    void Update() {

        // Get the current corner points of the slice.
        Vector3[] corners = slicer.GetSliceCorners();

        // Generate seed-points.
        (int, int) shape = GenerateGridSeedPoints(corners, ref seedPoints, displacement);

        // Actual number of streamlines in this frame (< <maxStreamlineCount>).
        int streamlineCount = shape.Item1 * shape.Item2;

        // Set the initial points.
        seedBuffer.SetData(seedPoints);

        // Update for GPU.
        InitializeIterator(streamlineCount);

        // Get data after GPU-calculation.
        // streamlineBuffer.GetData(streamlinePoints);

        // Draw streamlines for debugging.
        /*
        DrawStreamlinesDebug(ref streamlinePoints, 
                             iteratorSteps, 
                             maxStreamlineCount, 
                             streamlineCount);
        */

        // Reconstruct streamlines.
        InitializeReconstructor();

        // For debugging, retrieve normal data and display it at the corresponding vertices.
        /*
        Vector3[] normalArray = new Vector3[maxStreamlineCount * iteratorSteps];
        Vector3[] tangentArray = new Vector3[maxStreamlineCount * iteratorSteps];
        normalBuffer.GetData(normalArray);
        tangentBuffer.GetData(tangentArray);
        DrawStreamlineNormalsDebug(ref streamlinePoints,
                                   ref normalArray,
                                   iteratorSteps,
                                   maxStreamlineCount,
                                   streamlineCount,
                                   10.0f);
        */
        /*
        float[] segmentLengthArray = new float[maxStreamlineCount * iteratorSteps];
        int offset = 0;
        segmentLengthBuffer.GetData(segmentLengthArray);
        for (int i = 0; i < iteratorSteps; i++) {
            Debug.Log(segmentLengthArray[offset + i * maxStreamlineCount]);
        }
        */

        // Initialize tube shader.
        InitializeTubeShader(streamlineCount);

        // Draw tubes.
        if(enabled)
            Graphics.DrawMeshInstancedIndirect(mesh:template.mesh,
                                               submeshIndex:0,
                                               material:material,
                                               bounds:new Bounds(transform.position, transform.lossyScale * 10000),
                                               bufferWithArgs:drawArgsBuffer,
                                               argsOffset:0,
                                               properties:props);

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
                                              float delta) {
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
                seedPoints[u * V + v] = SwapYZ(rawVec);
            }
        }

        return (U, V);
    }

    /**
     * For visual debugging.
     */
    /*
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
    */

    /**
     * Visual debugging of streamlines.
     */
    void DrawStreamlinesDebug(ref Vector3[] positions, 
                              int iteratorSteps, 
                              int maxStreamlineCount, 
                              int streamlineCount) {
        for (int i = 0; i < streamlineCount; i++) {
            for (int j = 1; j < iteratorSteps; j++) {
                Vector3 last = SwapYZ(positions[i + (j - 1) * maxStreamlineCount]);
                Vector3 curr = SwapYZ(positions[i + j * maxStreamlineCount]);
                float distance = Vector3.Magnitude(last - curr);
                maxStepDist = distance > maxStepDist ? distance : maxStepDist;
                minStepDist = distance < minStepDist ? distance : minStepDist;
                float t = (distance - minStepDist) / (maxStepDist - minStepDist);
                Debug.DrawLine(last, curr, gradient.Evaluate(Mathf.Min(t, 1.0f)));
            }
        }
    }

    void DrawStreamlinesDebugSimple(ref Vector3[] positions, 
                                    int iteratorSteps, 
                                    int maxStreamlineCount, 
                                    int streamlineCount) {
        for (int i = 0; i < streamlineCount; i++) {
            for (int j = 1; j < iteratorSteps; j++) {
                Vector3 last = SwapYZ(positions[i + (j - 1) * maxStreamlineCount]);
                Vector3 curr = SwapYZ(positions[i + j * maxStreamlineCount]);
                float t = (float) i / streamlineCount;
                Debug.DrawLine(last, curr, gradient.Evaluate(Mathf.Min(t, 1.0f)));
            }
        }
    }

    /**
     * Debugging normal directions.
     */
    void DrawStreamlineNormalsDebug(ref Vector3[] positions,
                                    ref Vector3[] normals,
                                    int iteratorSteps,
                                    int maxStreamlineCount,
                                    int streamlineCount,
                                    float normalMultiplier) {
        for (int i = 0; i < streamlineCount; i++) {
            for (int j = 0; j < iteratorSteps; j++) {
                Vector3 p = SwapYZ(positions[i + j * maxStreamlineCount]);
                Vector3 n = SwapYZ(normals[i + j * maxStreamlineCount]);
                float t = (float) j / iteratorSteps;
                Debug.DrawLine(p, p + normalMultiplier * n, gradient.Evaluate(t));
            }
        }  
    }

    /**
     * Convenience method that swaps the y- and z-component of a vector (for portability between Unity and GPU).
     */
    Vector3 SwapYZ(Vector3 vec) {
        return new Vector3(vec.x, vec.z, vec.y);
    }

    /**
     * Extracts 8 color (maximal number of handles for gradient) values from the gradient and passes it to 
     * <gradientColors>.
     */
    void ExtractGradientColors() {
        
        // Using Vector3[] here because I don't know whether Color[] is blittable.
        Vector3[] colors = new Vector3[8];
        for (int i = 0; i < 8; i++) {
            Color color = gradient.Evaluate(i / 7.0f);
            Vector3 colorVec = new Vector3(color.r, color.g, color.b);
            colors[i] = colorVec;
        }
        gradientColors.SetData(colors);
    }

    /**
     * Frees memory held by <buffer>.
     */
    void FreeBuffer(ComputeBuffer buffer) {
        buffer.Release();
        buffer = null;
    }

    void InitializeIterator(int streamlineCount) {
        int kernelId = computeShader.FindKernel("Iterator");
        computeShader.SetInt(maxStreamlineCountId, maxStreamlineCount);
        computeShader.SetInt(streamlineCountId, streamlineCount);
        computeShader.SetInt(iteratorStepsId, iteratorSteps);
        computeShader.SetFloat(stepSizeId, stepSize);
        computeShader.SetTexture(kernelId, vectorfieldTextureId, vectorfieldTexture, 0);
        computeShader.SetBuffer(kernelId, streamlineBufferId, streamlineBuffer);
        computeShader.SetBuffer(kernelId, seedBufferId, seedBuffer);
        computeShader.SetVector(volumeBoundaryMinId, SwapYZ(volumeBoundaryMin));
        computeShader.Dispatch(kernelId, groupCount, 1, 1);
    }

    void InitializeReconstructor() {
        int kernelId = computeShader.FindKernel("Reconstructor");
        computeShader.SetBuffer(kernelId, streamlineRecBufferId, streamlineBuffer);
        computeShader.SetBuffer(kernelId, tangentBufferId, tangentBuffer);
        computeShader.SetBuffer(kernelId, normalBufferId, normalBuffer);
        computeShader.SetBuffer(kernelId, segmentLengthBufferId, segmentLengthBuffer);
        
        // Setting the maximal and minimal values for each streamline.
        float[] maxLengthArray = new float[maxStreamlineCount];
        float[] minLengthArray = new float[maxStreamlineCount];
        for (int i = 0; i < maxStreamlineCount; i++) {
            maxLengthArray[i] = 0.0f;
            minLengthArray[i] = float.MaxValue;
        }
        maxLengthBuffer.SetData(maxLengthArray);
        minLengthBuffer.SetData(minLengthArray);

        computeShader.SetBuffer(kernelId, maxLengthBufferId, maxLengthBuffer);
        computeShader.SetBuffer(kernelId, minLengthBufferId, minLengthBuffer);

        computeShader.SetFloat(minLenId, float.MaxValue);
        computeShader.SetFloat(maxLenId, 0.0f);
        computeShader.Dispatch(kernelId, groupCount, 1, 1);
    }

    void InitializeTubeShader(int streamlineCount) {
        material.SetFloat(_RadiusId, radius);
        material.SetInt(_MaxStreamlineCountId, maxStreamlineCount);
        material.SetInt(_StreamlineCountId, streamlineCount);
        material.SetBuffer(_MaxLengthBufferId, maxLengthBuffer);
        material.SetBuffer(_MinLenthBufferId, minLengthBuffer);
        material.SetBuffer(_GradientColorsId, gradientColors);
        material.SetBuffer(_StreamlineBufferId, streamlineBuffer);
        material.SetBuffer(_TangentBufferId, tangentBuffer);
        material.SetBuffer(_NormalBufferId, normalBuffer);
        material.SetBuffer(_SegmentLengthBufferId, segmentLengthBuffer);
        material.SetVector(_VolumeBoundaryMinId, volumeBoundaryMin);
    }

}
