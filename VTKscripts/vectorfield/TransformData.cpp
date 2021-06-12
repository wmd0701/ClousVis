#include <vtkNew.h>
#include <vtkProperty.h>
#include <vtkXMLImageDataReader.h>
#include <vtkImageData.h>
#include <vtkPointData.h>
#include <vtkDataArray.h>
#include <vtkfloatArray.h>
#include <iostream>
#include <math.h>
#include <fstream>
#include <iostream>

namespace {
	void scaleZ(vtkImageData* data);
	std::tuple<float, float> getRange(vtkFloatArray* array);
	char convertTo8bit(float num, std::tuple<float, float> range);
}

int main()
{
	// Filepaths.
	std::string uFileName = "C:\\Users\\rapha\\Documents\\fs21\\vis\\Clouds\\ua\\ua_10.vti";
	std::string vFileName = "C:\\Users\\rapha\\Documents\\fs21\\vis\\Clouds\\va\\va_10.vti";
	std::string wFileName = "C:\\Users\\rapha\\Documents\\fs21\\vis\\Clouds\\wa\\wa_10.vti";

	// Reading.
	vtkNew<vtkXMLImageDataReader> uReader;
	uReader->SetFileName(uFileName.c_str());
	uReader->Update();
	vtkNew<vtkXMLImageDataReader> vReader;
	vReader->SetFileName(vFileName.c_str());
	vReader->Update();
	vtkNew<vtkXMLImageDataReader> wReader;
	wReader->SetFileName(wFileName.c_str());
	wReader->Update();

	// Scaling data.
	vtkImageData* uImgData = uReader->GetOutput();
	vtkImageData* vImgData = vReader->GetOutput();
	vtkImageData* wImgData = wReader->GetOutput();
	// scaleZ(uImgData);
	// scaleZ(vImgData);
	// scaleZ(wImgData);

	// Convert to float.
	vtkFloatArray* uFloatData = vtkFloatArray::SafeDownCast(uImgData->GetPointData()->GetArray(0));
	vtkFloatArray* vFloatData = vtkFloatArray::SafeDownCast(vImgData->GetPointData()->GetArray(0));
	vtkFloatArray* wFloatData = vtkFloatArray::SafeDownCast(wImgData->GetPointData()->GetArray(0));

	// Get ranges.
	std::tuple<float, float> uValRange = getRange(uFloatData);
	std::tuple<float, float> vValRange = getRange(vFloatData);
	std::tuple<float, float> wValRange = getRange(wFloatData);
	std::cout << std::get<0>(uValRange) << ", " << std::get<1>(uValRange) << std::endl;
	std::cout << std::get<0>(vValRange) << ", " << std::get<1>(vValRange) << std::endl;
	std::cout << std::get<0>(wValRange) << ", " << std::get<1>(wValRange) << std::endl;

	/* Creating a native C++ array to hold the data.
	* Apparently C++ only allows dynamic allocation for the first dimension.
	* Thus I need to flatten the data
	*/
	/*
	int* extent = uImgData->GetExtent();
	int x_ext = extent[1] + 1;
	int y_ext = extent[3] + 1;
	int z_ext = extent[5] + 1;
	long size = x_ext * y_ext * z_ext * 3;
	char* data = new char[size];
	for (int i = 0; i < x_ext; i++) {
		for (int j = 0; j < y_ext; j++) {
			for (int k = 0; k < z_ext; k++) {
				long floatFlatIdx = i * y_ext * z_ext + j * z_ext + k;
				long dataFlatIdx = i * y_ext * z_ext * 3 + j * z_ext * 3 + k * 3;
				float uVal = uFloatData->GetTypedComponent(floatFlatIdx, 0);
				float vVal = vFloatData->GetTypedComponent(floatFlatIdx, 0);
				float wVal = wFloatData->GetTypedComponent(floatFlatIdx, 0);
				data[dataFlatIdx] = convertTo8bit(uVal, uValRange);
				data[dataFlatIdx+1] = convertTo8bit(vVal, vValRange);
				data[dataFlatIdx+2] = convertTo8bit(wVal, wValRange);
			}
		}
	}

	// Write to binary file.
	std::ofstream binFile;
	binFile.open("vectorfield.data", ios::out | ios::binary);
	binFile.write(data, size);
	binFile.close();


	// Free up memory.
	delete[] data;
	*/

	std::fstream binFile;
	binFile.open("vectorfield2.data", std::ios::out | std::ios::binary);
	for (int j = 0; j < 1556; j++) {
		for (int i = 0; i < 150; i++) {
			for (int k = 0; k < 1429; k++) {
				int flatIdx = (149 - i) * 1556 * 1429 + j * 1429 + k;
				char u = convertTo8bit(uFloatData->GetTypedComponent(flatIdx, 0), uValRange);
				char v = convertTo8bit(vFloatData->GetTypedComponent(flatIdx, 0), vValRange);
				char w = convertTo8bit(wFloatData->GetTypedComponent(flatIdx, 0), wValRange);
				binFile.write(&u, 1);
				binFile.write(&v, 1);
				binFile.write(&w, 1);
			}
		}
	}
	binFile.close();
}

namespace {

	void scaleZ(vtkImageData* data) {
		double* spacing = data->GetSpacing();
		spacing[2] *= 0.0001;
		data->SetSpacing(spacing);
	}

	char convertTo8bit(float num, std::tuple<float, float> range) {

		float min = std::get<0>(range);
		float max = std::get<1>(range);

		// Normalizing.
		num -= min;
		num /= (max - min);

		// Getting relative value in [0, 255]
		num *= 255.0;
		// Discretizing.
		return static_cast<char>(static_cast<int>(num));
	}

	std::tuple<float, float> getRange(vtkFloatArray* array) {

		// Setting highs and lows.
		float min = pow(2, 32);
		float max = -pow(2, 32);

		// Values in vtk-arrays are stored as tuples.
		int tupleCount = array->GetNumberOfTuples();
		for (int i = 0; i < tupleCount; i++) {
			float value = array->GetTypedComponent(i, 0);
			min = value < min ? value : min;
			max = value > max ? value : max;
		}
		return std::make_tuple(min, max);
	}
}