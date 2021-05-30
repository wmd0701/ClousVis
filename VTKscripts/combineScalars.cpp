#include <vtkNew.h>
#include <vtkProperty.h>
#include <vtkXMLImageDataReader.h>
#include <vtkImageData.h>
#include <vtkPointData.h>
#include <vtkDataArray.h>
#include <vtkfloatArray.h>
#include <vtkImageResize.h>
#include <iostream>
#include <string>
#include <fstream>
#include <vtkImageActor.h>
#include <vtkRenderer.h>
#include <vtkRenderWindow.h>
#include <vtkInteractorStyleImage.h>
#include <vtkRenderWindowInteractor.h>
#include <vtkImageReslice.h>
#include <vtkImageResample.h>

inline int getLinearIndex(const int x, const int y, const int z) {
	return x + y * 1429 + z * 1429 * 1556;
}

int main()
{
	const int numComponents = 3;

	std::string xFileName = "C:/Users/kate/CloudStation/ETH/Visualization/Project/Clouds/clw/clw_10.vti";
	std::string yFileName = "C:/Users/kate/CloudStation/ETH/Visualization/Project/Clouds/cli/cli_10.vti";
	std::string zFileName = "C:/Users/kate/CloudStation/ETH/Visualization/Project/Clouds/qr/qr_10.vti";

	std::string outputFileName = "clouds";

	double thresholds[numComponents] = { 0.0004, 0.0015, 0.0001 };
	double downsamplingFactors[3] = { 2, 2, 2 };

	std::string fileNames[3] = { xFileName, yFileName, zFileName };
	//std::string fileNames[numComponents] = { xFileName };
	vtkSmartPointer<vtkImageData> data[numComponents];

	vtkPointData* pointData;
	vtkFloatArray* dataArrays[numComponents];

	int xDim = 0, yDim = 0, zDim = 0;

	for (int i = 0; i < numComponents; i++)
	{
		// Read the file
		vtkNew<vtkXMLImageDataReader> reader;
		reader->SetFileName(fileNames[i].c_str());
		reader->Update();

		data[i] = reader->GetOutput();
		//reader->get
		//double* bounds = data[i]->GetSpacing();
		//bounds[2] *= 0.0001;		// scale the z spacing down
		//data->SetSpacing(bounds);
		//std::cout << "bounds: " << bounds[0] << " " << bounds[1] << " " << bounds[2] << std::endl;
		int* extents = data[i]->GetExtent();
		std::cout << "extents: " << extents[0] << " " << extents[1] << " " << extents[2] << " " << extents[3] << " " << extents[4] << " " << extents[5] << std::endl;

		double* spacing = data[i]->GetSpacing();
		std::cout << "spacing: " << spacing[0] << " " << spacing[1] << " " << spacing[2] << std::endl;

		/*
		// nvm this, anyway only works if dimensions of all components are the same
		xDim = (xDim >= extents[1]) ? xDim : extents[1];
		yDim = (yDim >= extents[3]) ? yDim : extents[3];
		zDim = (zDim >= extents[5]) ? zDim : extents[5];
		*/

		xDim = extents[1] + 1;
		yDim = extents[3] + 1;
		zDim = extents[5] + 1;

		std::cout << "dimensions: " << xDim << " " << yDim << " " << zDim << std::endl;

		/*
		vtkSmartPointer<vtkImageReslice> reslice = vtkSmartPointer<vtkImageReslice>::New();
		reslice->SetInputData(data[i]);
		reslice->SetOutputSpacing(1., 1., 1.);
		reslice->SetInterpolationModeToCubic();
		reslice->Update();
		vtkImageData* test = reslice->GetOutput();
		*/
		
		/*
		vtkSmartPointer<vtkImageResize> resize = vtkSmartPointer<vtkImageResize>::New();
		resize->SetInputData(data[i]);
		resize->SetResizeMethodToMagnificationFactors();
		resize->SetMagnificationFactors(downsamplingFactors);
		resize->Update();
		vtkImageData* test = resize->GetOutput();
		*/

		/*
		extents = test->GetExtent();
		std::cout << "extents: " << extents[0] << " " << extents[1] << " " << extents[2] << " " << extents[3] << " " << extents[4] << " " << extents[5] << std::endl;

		spacing = test->GetSpacing();
		std::cout << "spacing: " << spacing[0] << " " << spacing[1] << " " << spacing[2] << std::endl;
		*/
		pointData = data[i]->GetPointData();
		dataArrays[i] = vtkFloatArray::SafeDownCast(pointData->GetArray(pointData->GetArrayName(0)));
	}

	std::cout << "xDim: " << xDim << std::endl << "yDim: " << yDim << std::endl << "zDim: " << zDim << std::endl;

	
	int nonzeros = 0;

	std::fstream file;
	file.open(outputFileName + ".data", std::ios::out | std::ios::binary);

	for (int i = 0; i < zDim; i++)
	{
		for (int j = 0; j < yDim; j++)
		{
			for (int k = 0; k < xDim; k++)
			{
				for (int c = 0; c < numComponents; c++)
				{
					// first one is the index of the tuple, second one is index for component.
					float entry = dataArrays[c]->GetTypedComponent(i * (xDim * yDim) + j * xDim + k, 0);

					file.write((char*)(&entry), sizeof(float));

					if (entry > 0.0004f) {
						nonzeros++;
					}
				}
			}
		}
	}

	file.close();
	

	/*
	std::cout << std::endl << "nonzeros: " << nonzeros << std::endl; // just 2 Million actual entries above 0.00015
	int z = zmin;
	int y = ymin;
	int x = xmin;
	int z2 = zmax;
	int y2 = ymax;
	int x2 = xmax;

	std::cout << "first at: " << x << " " << y << " " << z << std::endl;
	std::cout << "last at: " << x2 << " " << y2 << " " << z2 << std::endl;
	*/

	return 0;
}