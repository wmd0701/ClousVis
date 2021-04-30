Hi There :)

Open this project with unity by using the "add" button in the hub. Then open it.

By now you should have all Files from the Unity Project with some erros due to missing textures and other data.
(Shown as warning signs at the bottom of the screen in Unity, I kept the meta-data inside in order not to break connections I hope)

What I did not include is:

- clw.data
	This File i created in vtk, using the script provided in the folder "VTKScripts" that is included in the github folder.
	Just run it, and it should create a 1.3GB binary file on your Desktop called clw.data (in Windows at least).
	Then import (drag and drop) this file into the folder "ImportedData" in the unity Project.

- clw_normalized.asset
	This is the actual 3D-Texture that is generated and loaded by the shaders. You have to convert the clw.data file into a 3D-Texture,
	by using the nice "TextureCreator" script I wrote. It's right in the Assets folder (Cube with curly brackets logo).
	Click on it and on the inspector Tab (normally right column of unity) you should see some options.
	Set: Source Name to "clw", and Texture Name to "clw_normalized".
	Then click the "Read and Create" button below it... and get a coffee.
	The new texture should have correctly been created in the "Assets" folder (shown as black cube)

Now everything should work, unless some links are broken. Write me if anything is still wrong.


Using the Project:

There are 2 Scenes:
-CloudVis (for seeing the cloud data in 1/100 scale raymarched)
-Slicer (for visualizing the sclicing algorithm, in horrible programmer art)

I will explain the inner workings during the meeting.