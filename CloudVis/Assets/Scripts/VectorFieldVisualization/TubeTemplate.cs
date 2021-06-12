using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TubeTemplate : ScriptableObject {

    #region Exposed Properties

    [Tooltip("Number of vertices in a ring.")]
    [SerializeField] int _divisionCount = 6;
    public int divisionCount {get {return Mathf.Clamp(_divisionCount, 2, 64);}}

    [Tooltip("Number of segments in a tube (has to equals to <IteratorSteps>).")]
    [SerializeField] int _segmentCount = 1024;
    public int segmentCount {get {return Mathf.Clamp(_segmentCount, 4, 4096);}}

    #endregion

    #region Serialized Data

    [SerializeField] Mesh _mesh;
    public Mesh mesh {get {return _mesh;}}

    #endregion

    #region Editor functions

    #if UNITY_EDITOR
        
        public void Rebuild() {

            // Vertex array.
            List<Vector3> verts = new List<Vector3>();

            // Head.
            verts.Add(new Vector3(0.0f, -1.0f, 0.0f));

            // Body.
            for (int i = 0; i <= _segmentCount; i++) {
                for (int j = 0; j < _divisionCount; j++) {
                    float angle = (Mathf.PI * 2.0f * j) / _divisionCount;
                    verts.Add(new Vector3(angle, 0, i));
                }
            }

            // Tail.
            verts.Add(new Vector3(0.0f, 1.0f, _segmentCount));

            // Index array.
            List<int> idxs = new List<int>();

            // Head.
            for (int i = 0; i < _divisionCount - 1; i++) {
                idxs.Add(0);
                idxs.Add(i + 2);
                idxs.Add(i + 1);
            }
            idxs.Add(0);
            idxs.Add(1);
            idxs.Add(_divisionCount);

            // Body.
            int refi = 1;

            for (int i = 0; i < _segmentCount; i++) {
                for (int j = 0; j < _divisionCount - 1; j++) {
                    idxs.Add(refi);
                    idxs.Add(refi + 1);
                    idxs.Add(refi + _divisionCount);

                    idxs.Add(refi + 1);
                    idxs.Add(refi + 1 + _divisionCount);
                    idxs.Add(refi + _divisionCount);

                    refi++;
                }
                idxs.Add(refi);
                idxs.Add(refi + 1 - _divisionCount);
                idxs.Add(refi + _divisionCount);

                idxs.Add(refi + 1 - _divisionCount);
                idxs.Add(refi + 1);
                idxs.Add(refi + _divisionCount);

                refi++;
            }

            // Tail.
            for (int i = 0; i < _divisionCount - 1; i++) {
                idxs.Add(refi + i);
                idxs.Add(refi + i + 1);
                idxs.Add(refi + _divisionCount);
            }

            idxs.Add(refi + _divisionCount - 1);
            idxs.Add(refi);
            idxs.Add(refi + _divisionCount);

            for (int i = 0; i < idxs.Count; i++) {
                if (idxs[i] > 6152) {
                    Debug.Log(idxs[i]);
                }
            }

            // Building mesh.
            _mesh.Clear();
            _mesh.SetVertices(verts);
            _mesh.SetIndices(idxs.ToArray(), MeshTopology.Triangles, 0);
            _mesh.UploadMeshData(true);
        }

    #endif

    #endregion

    #region ScriptableObject Functions

    void OnEnable() {
        if (_mesh == null) {
            _mesh = new Mesh();
            _mesh.name = "TubeTemplate";
        }
    }

    void OnValidate() {
        _divisionCount = Mathf.Clamp(_divisionCount, 2, 64);
        _segmentCount = Mathf.Clamp(_segmentCount, 4, 4096);
    }

    #endregion
}
