#include <vtkNew.h>
#include <vtkProperty.h>
#include <vtkXMLImageDataReader.h>
#include <vtkImageData.h>
#include <vtkPointData.h>
#include <vtkDataArray.h>
#include <vtkfloatArray.h>
#include <iostream>
#include <fstream>
 
inline int getLinearIndex(const int x, const int y, const int z) {
	return x + y * 1429 + z * 1429 * 1556;
}

int main()
{
	std::string inputFilename = "C:/users/gdani/Desktop/Clouds/clw/clw_10.vti";

	// Read the file
	vtkNew<vtkXMLImageDataReader> reader;
	reader->SetFileName(inputFilename.c_str());
	reader->Update();

	vtkImageData* data = reader->GetOutput();
		double* bounds = data->GetSpacing();
		bounds[2] *= 0.0001;		// scale the z spacing down
		data->SetSpacing(bounds);


	std::cout << bounds[0] << " " << bounds[1] << " " << bounds[2] << std::endl;
	int* extents = data->GetExtent();
	std::cout << extents[0] << " " << extents[1] << " " << extents[2] << " " << extents[3] << " " << extents[4] << " " << extents[5] << std::endl;

	// Data is stored per point. (other options are per cell or per field... we only have per point data, i checked).
	vtkPointData* pointdata = data->GetPointData();
	std::cout << pointdata->GetArrayName(0) << std::endl; // there is only one

	//The per-point-attributes are stored in an array by name... we only have one called "cli" for this dataset.
	// std::string name = pd->GetArrayName(i); // use this if you don't know the name. Replace i=0 as we always only have one attribute.
	vtkDataArray* dataarray = pointdata->GetArray("clw");
	
	// you can query the actual data-type stored (they store everything as doubles somehow internally)
	std::cout << dataarray->GetDataTypeAsString() << " data type" << std::endl;

	// cast it to a nice float-array... this is only for not having to cast to float manually.
	vtkFloatArray* floatarray = vtkFloatArray::SafeDownCast(dataarray);

	// each entry is stored as a tuple (with a certain amount of components (if it is vector-like -> colour as example)
	// so we have for each point in the grid one tuple with one component each, as it is just a scalar.
	int ntuples = floatarray->GetNumberOfTuples();
	std::cout << "entries: " << ntuples << std::endl; // some 333 Million numbers O.O

	std::cout << dataarray->GetMaxNorm() << " is max norm" << std::endl;

	// example for linear access of all elements:
	int nonzeros = 0;
	int xmin = 1429, ymin = 1556, zmin = 150;
	int xmax = 0, ymax = 0, zmax = 0;

	std::fstream file;
	file.open("clw.data", std::ios::out | std::ios::binary);

	for (int i = 0; i < 150; i++)
	{
		for (int j = 0; j < 1556; j++)
		{
			for (int k = 0; k < 1429; k++)
			{
				// first one is the index of the tuple, second one is index for component.
				float entry = floatarray->GetTypedComponent(i*(1429*1556)+j*1429+k, 0);
				
				file.write((char*)(&entry), sizeof(float));
				
				if (entry > 0.0004f) {
					nonzeros++;
					//if (xmin > k) xmin = k;
					//if (ymin > j) ymin = j;
					//if (zmin > i) zmin = i;
					//if (k > xmax) xmax = k;
					//if (j > ymax) ymax = j;
					//if (i > zmax) zmax = i;
				}
			}
		}
	}
	
	file.close();
	
	std::cout << std::endl << "nonzeros: " << nonzeros << std::endl; // just 2 Million actual entries above 0.00015
	int z = zmin;
	int y = ymin;
	int x = xmin;
	int z2 = zmax;
	int y2 = ymax;
	int x2 = xmax;

	std::cout << "first at: " << x << " " << y << " " << z << std::endl;
	std::cout << "last at: " << x2 << " " << y2 << " " << z2 << std::endl;

	return 0;
}