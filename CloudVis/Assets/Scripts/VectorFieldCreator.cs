using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(menuName="VectorFieldCreator")]
public class VectorFieldCreator : ScriptableObject
{
    int xCount = 1429;
    int yCount = 1556;
    int zCount = 150;

    public TextureFormat textureFormat = TextureFormat.RGB24;           // Note that actually only have 3 channels a 3 bit, but RGB24 is uncommon.
    public TextureWrapMode textureWrapMode = TextureWrapMode.Clamp;     // Adds padding with the same pixel value as border.

    public string source = "vectorfield";                               // Name of the .dat file located in "ImportedData".
    public string dest = "vectorfieldTexture";                          // Name of the texture once created.

    public void ReadAndCreate()
    /* Reads binary file (.dat) identified by <source>.dat in "ImportedData" and creates a 3D texture. */
    {
        // Path to file.
        string path = "Assets/ImportedData/" + source + ".data";
        // Check whether source file exists.
        if (!System.IO.File.Exists(path)) 
        // If it doesn't exist, tell the user via console.
        {
            Debug.Log(source + ".data could not be found in ImportedData.");
            return;
        }

        // The file exists. We create texture and fill it pixel by pixel.
        Texture3D texture = new Texture3D(xCount, yCount, zCount, textureFormat, false);
        texture.wrapMode = textureWrapMode;
        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)) )
        {
            for (int i = 0; i < zCount; i++) 
            {
                for (int j = 0; j < yCount; j++) 
                {
                    for (int k = 0; k < xCount; k++) 
                    {
                        byte u = reader.ReadByte();
                        byte v = reader.ReadByte();
                        byte w = reader.ReadByte();
                        Color32 color = new Color32(u, v, w, 0);
                        texture.SetPixel(k, j, i, color);
                    }
                }
            }
        }

        // Save texture.
        texture.Apply();
        AssetDatabase.CreateAsset(texture, "Assets/" + dest + ".asset");
    }
}

[CustomEditor(typeof(VectorFieldCreator), true)]
public class VectorFieldEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
		if (GUILayout.Button("Read and Create"))
		{
			((VectorFieldCreator)target).ReadAndCreate();
		}
		EditorGUI.EndDisabledGroup();
	}
}
