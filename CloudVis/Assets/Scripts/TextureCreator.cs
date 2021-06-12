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

	// This is an example function used to load some binary file and convert it to texture.
	public void generateVectorfield()
	{
		Texture3D texture = new Texture3D(sizeX, sizeY, sizeZ, format, false); // TODO: This is not aligned with what we use in unity, where y is up... change it.
		texture.wrapMode = wrapMode;


		//float[] data_floats = new float[sizeX * sizeY * sizeZ];

		if (File.Exists("Assets/ImportedData/" + sourceName + ".data"))
		{
			using (BinaryReader reader = new BinaryReader(File.Open("Assets/ImportedData/" + sourceName + ".data", FileMode.Open)))
			{

				for(int z = 0; z < sizeZ; z++)
                {
					for(int y = 0; y < sizeY; y++)
                    {
						for(int x = 0; x < sizeX; x++)
						{
							byte u = reader.ReadByte();
							byte v = reader.ReadByte();
							byte w = reader.ReadByte();
							
							Color32 c = new Color32((byte)u, (byte)v, (byte)w, 1);
							texture.SetPixel(x, y, z, c);
                        }
                    }
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

		//Texture3D texture = new Texture3D(sizeX, sizeZ, sizeY, format, false); // TODO: This is not aligned with what we use in unity, where y is up... change it.
		//texture.wrapMode = wrapMode;

		//texture.SetPixelData<float>(data_floats, 0);
		//texture.Apply();

		AssetDatabase.CreateAsset(texture, "Assets/"+TextureName+".asset");
	}


	public void generateClouds()
	{
		Texture3D texture = new Texture3D(sizeX, sizeY, sizeZ, format, false); // TODO: This is not aligned with what we use in unity, where y is up... change it.
		texture.wrapMode = wrapMode;

		if (File.Exists("Assets/ImportedData/" + sourceName + ".data"))
		{
			using (BinaryReader reader = new BinaryReader(File.Open("Assets/ImportedData/" + sourceName + ".data", FileMode.Open)))
			{
				for (int z = 0; z < sizeZ; z++)
				{
					for (int y = 0; y < sizeY; y++)
					{
						for (int x = 0; x < sizeX; x++)
						{
							float ci = reader.ReadSingle();
							if (ci > 0.0f) ci = Mathf.Clamp(ci / 0.0065f * 10.0f,0.0001f,1.0f);
							float cw = reader.ReadSingle();
							if (cw > 0.0f) cw = Mathf.Clamp(cw / 0.0065f * 10.0f, 0.0001f, 1.0f);
							float qr = reader.ReadSingle();
							if (qr > 0.0f) qr = Mathf.Clamp(qr / 0.0065f * 10.0f, 0.0001f, 1.0f);
							float pres = reader.ReadSingle();

							Color c = new Color(ci, cw, qr,1.0f);
							texture.SetPixel(x, y, z, c);
						}
					}
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

		//Texture3D texture = new Texture3D(sizeX, sizeZ, sizeY, format, false); // TODO: This is not aligned with what we use in unity, where y is up... change it.
		//texture.wrapMode = wrapMode;
		//texture.SetPixelData<float>(data_floats, 0);
		//texture.Apply();

		AssetDatabase.CreateAsset(texture, "Assets/" + TextureName + ".asset");
	}

}

[CustomEditor(typeof(TextureCreator), true)]
public class TextureEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
		if (GUILayout.Button("Generate cloud texture"))
		{
			((TextureCreator)target).generateClouds();
		}
		if (GUILayout.Button("Generate Vectorfield"))
		{
			((TextureCreator)target).generateVectorfield();
		}
		EditorGUI.EndDisabledGroup();
	}
}