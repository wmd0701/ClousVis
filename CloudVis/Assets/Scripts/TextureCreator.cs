using UnityEngine;
using UnityEditor;
using System.IO;

[CreateAssetMenu(menuName ="TextureCreator")]
public class TextureCreator : ScriptableObject
{
	int sizeX = 1429;
	int sizeY = 150;
	int sizeZ = 1556;

	public TextureFormat format = TextureFormat.RFloat;
	public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

	public string sourceName = "clw";
	public string TextureName = "clw_normalized";

	// creates a sphere of radius 7.5km at the center of the texture. This was just for testing purposes.
	public void createSphere()
	{
		Texture3D texture = new Texture3D(sizeX, sizeY, sizeZ, format, false);
		texture.wrapMode = wrapMode;

		int totalValues = sizeX * sizeY * sizeZ;
		float[] data = new float[totalValues];

		for (int y = 0; y < sizeY; y++)
		{
			int yOffset = y * sizeX * sizeZ;
			for (int z = 0; z < sizeZ; z++)
			{
				int zOffset = z * sizeX;
				for (int x = 0; x < sizeX; x++)
				{
					float length = Mathf.Sqrt((x-1752.0f) * (x - 1752.0f) + (y - 75.0f) * (y - 75.0f) + (z - 1890.5f) * (z - 1890.5f));
					if (length < 75.0f)
					data[x + yOffset + zOffset] = 1/length;
				}
			}
		}

		texture.SetPixelData<float>(data, 0);
		texture.Apply();

		AssetDatabase.CreateAsset(texture, "Assets/SphereTexture.asset");
		Debug.Log("Created a new texture at: "+"Assets/SphereTexture.asset");
	}

	// This is an example function used to load some binary file and convert it to texture.
	public void readAndCreate()
	{
		float[] data_floats = new float[sizeX * sizeY * sizeZ];

		if (File.Exists("Assets/ImportedData/" + sourceName + ".data"))
		{
			using (BinaryReader reader = new BinaryReader(File.Open("Assets/ImportedData/" + sourceName + ".data", FileMode.Open)))
			{
				for (int i = 0; i < sizeX * sizeY * sizeZ; i++)
				{
					data_floats[i] = reader.ReadSingle() / 0.00631f; //normalize (by approximate largest value I found in paraview for clw)
				}
				reader.Close();
			}
		}
		else
		{
			Debug.LogError("File " + sourceName + " not found. Please make sure it is in folder: Assets/ImportedData/  and that it ends in .data!");
			return;
		}

		Debug.Log("Successfully read the data!");

		Texture3D texture = new Texture3D(sizeX, sizeZ, sizeY, format, false); // TODO: This is not aligned with what we use in unity, where y is up... change it.
		texture.wrapMode = wrapMode;

		texture.SetPixelData<float>(data_floats, 0);
		texture.Apply();

		AssetDatabase.CreateAsset(texture, "Assets/"+TextureName+".asset");
	}

}

[CustomEditor(typeof(TextureCreator), true)]
public class TextureEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
		if (GUILayout.Button("Read and Create"))
		{
			((TextureCreator)target).readAndCreate();
		}
		if (GUILayout.Button("Debug: Create Sphere"))
		{
			((TextureCreator) target).createSphere();
		}
		EditorGUI.EndDisabledGroup();
	}
}