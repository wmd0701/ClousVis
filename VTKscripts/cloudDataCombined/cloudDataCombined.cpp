#include <vtkNew.h>
#include <vtkProperty.h>
#include <vtkXMLImageDataReader.h>
#include <vtkImageData.h>
#include <vtkPointData.h>
#include <vtkDataArray.h>
#include <vtkfloatArray.h>
#include <iostream>
#include <fstream>

inline unsigned int getLinearIndex(const unsigned int x, const unsigned int y, const unsigned int z) {
	return x + y * 1429 + z * 1429 * 1556;
}

int main()
{
	std::string inputFilename = "path to cli_10.vti";
	std::string inputFilename2 = "path to clw_10.vti";
	std::string inputFilename3 = "path to qr_10.vti";
	std::string inputFilename4 = "path to pres_10.vti";

	// Read the file
	vtkNew<vtkXMLImageDataReader> reader;
	reader->SetFileName(inputFilename.c_str());
	reader->Update();

	vtkNew<vtkXMLImageDataReader> reader2;
	reader2->SetFileName(inputFilename2.c_str());
	reader2->Update();

	vtkNew<vtkXMLImageDataReader> reader3;
	reader3->SetFileName(inputFilename3.c_str());
	reader3->Update();

	vtkNew<vtkXMLImageDataReader> reader4;
	reader4->SetFileName(inputFilename4.c_str());
	reader4->Update();

	vtkImageData* data1 = reader->GetOutput();
	vtkImageData* data2 = reader2->GetOutput();
	vtkImageData* data3 = reader3->GetOutput();
	vtkImageData* data4 = reader4->GetOutput();
	//	double* bounds = data->GetSpacing();
	//	bounds[2] *= 0.0001;		// scale the z spacing down
	//	data->SetSpacing(bounds);


	//std::cout << bounds[0] << " " << bounds[1] << " " << bounds[2] << std::endl;
	//int* extents = data->GetExtent();
	//std::cout << extents[0] << " " << extents[1] << " " << extents[2] << " " << extents[3] << " " << extents[4] << " " << extents[5] << std::endl;

	// Data is stored per point. (other options are per cell or per field... we only have per point data, i checked).
	vtkPointData* pointdata1 = data1->GetPointData();
	vtkPointData* pointdata2 = data2->GetPointData();
	vtkPointData* pointdata3 = data3->GetPointData();
	vtkPointData* pointdata4 = data4->GetPointData();
	//std::cout << pointdata->GetArrayName(0) << std::endl; // there is only one

	//The per-point-attributes are stored in an array by name... we only have one called "cli" for this dataset.
	// std::string name = pd->GetArrayName(i); // use this if you don't know the name. Replace i=0 as we always only have one attribute.
	vtkDataArray* dataarray1 = pointdata1->GetArray("cli");
	vtkDataArray* dataarray2 = pointdata2->GetArray("clw");
	vtkDataArray* dataarray3 = pointdata3->GetArray("qr");
	vtkDataArray* dataarray4 = pointdata4->GetArray("pres");
	// you can query the actual data-type stored (they store everything as doubles somehow internally)
	//std::cout << dataarray->GetDataTypeAsString() << " data type" << std::endl;

	// cast it to a nice float-array... this is only for not having to cast to float manually.
	vtkFloatArray* floatarray1 = vtkFloatArray::SafeDownCast(dataarray1);
	vtkFloatArray* floatarray2 = vtkFloatArray::SafeDownCast(dataarray2);
	vtkFloatArray* floatarray3 = vtkFloatArray::SafeDownCast(dataarray3);
	vtkFloatArray* floatarray4 = vtkFloatArray::SafeDownCast(dataarray4);
	// each entry is stored as a tuple (with a certain amount of components (if it is vector-like -> colour as example)
	// so we have for each point in the grid one tuple with one component each, as it is just a scalar.
	//int ntuples = floatarray->GetNumberOfTuples();
	//std::cout << "entries: " << ntuples << std::endl; // some 333 Million numbers O.O

	//std::cout << dataarray->GetMaxNorm() << " is max norm" << std::endl;

	// example for linear access of all elements:
	int nonzeros = 0;
	int xmini = 1429, ymini = 1556, zmini = 150;
	int xmaxi = 0, ymaxi = 0, zmaxi = 0;

	int xminc = 1429, yminc = 1556, zminc = 150;
	int xmaxc = 0, ymaxc = 0, zmaxc = 0;

	int xminr = 1429, yminr = 1556, zminr = 150;
	int xmaxr = 0, ymaxr = 0, zmaxr = 0;

	float maxval4 = -99999.0f;
	float minval4 = 99999.0f;

	std::fstream file;
	file.open("clouds.data", std::ios::out | std::ios::binary);

	//float minMag = 99999.0f;
	//float maxmag = -99999.0f;

	//float uOffset = 13.1580f;
	//float vOffset = 20.4192f;
	//float wOffset = 10.1056f;

	//float uStep = (47.3699f + 13.1579f) / 255.0f;
	//float vStep = (50.6398f + 20.4191f) / 255.0f;
	//float wStep = (15.765f + 10.1055f) / 255.0f;

	//float magStep = 64.8023f / 255.0f;

	float MAX_ICE = 0.00089f;
	float MAX_CLO = 0.00632f;
	float MAX_WAT = 0.00734f;

	float MAX_SUM = 0.00841903;

	// scale air pressure value since they are too large
	float pres_Scaler = 5e-8;

#define N 2047
	//std::vector<int> buckets_ice(N, 0.0f);
	//std::vector<int> buckets_clo(N, 0.0f);
	//std::vector<int> buckets_wat(N, 0.0f);
	//std::vector<int> buckets_sum(N, 0.0f);
	//std::vector<int> type_bucket(8, 0);


	for (unsigned int j = 0; j < 1556; j++)
	{
		for (unsigned int i = 0; i < 150; i++)
		{
			for (unsigned int k = 0; k < 1429; k++)
			{
				// first one is the index of the tuple, second one is index for component.
				float ice = floatarray1->GetTypedComponent((149 - i) * (1429 * 1556) + j * 1429 + k, 0);
				float clo = floatarray2->GetTypedComponent((149 - i) * (1429 * 1556) + j * 1429 + k, 0);
				float wat = floatarray3->GetTypedComponent((149 - i) * (1429 * 1556) + j * 1429 + k, 0);
				float pres = floatarray4->GetTypedComponent((149 - i) * (1429 * 1556) + j * 1429 + k, 0);
				pres *= pres_Scaler;

				if (pres < minval4) minval4 = pres;
				if (pres > maxval4) maxval4 = pres;

				//int rel_ice = 0;
				//int rel_clo = 0;
				//int rel_wat = 0;
				//if(ice > 0.0f) rel_ice = 1 + std::floor(N * (ice / MAX_ICE));
				//if(clo > 0.0f) rel_clo = 1 + std::floor(N * (clo / MAX_CLO));
				//if(wat > 0.0f) rel_wat = 1 + std::floor(N * (wat / MAX_WAT));

				//buckets_ice[rel_ice] += 1;
				//buckets_clo[rel_clo] += 1;
				//buckets_wat[rel_wat] += 1;


				//float sum = ice + clo + wat;
				//int rel_sum = 0;
				//if (sum > 0.0f) {
					//rel_sum = 1 + std::floor(N * (sum / MAX_SUM));
				//}
				//buckets_sum[rel_sum] += 1;


				//int type = 0;
				//if (ice > 0.0f) type += 1;
				//if (clo > 0.0f) type += 2;
				//if (wat > 0.0f) type += 4;

				//type_bucket[type] += 1;

				//if (sum > 0.0f) {
				//	nonzeros++;
				//}
				//if (sum > maxval1) maxval1 = sum;
				//if (sum < minval1) minval1 = sum;
				/*
				if (ice > 0.0f) {
					if (i > zmaxi) zmaxi = i;
					if (i < zmini) zmini = i;
					if (j > ymaxi) ymaxi = j;
					if (j < ymini) ymini = j;
					if (k > xmaxi) xmaxi = k;
					if (k < xmini) xmini = k;
				}

				if (clo > 0.0f) {
					if (i > zmaxc) zmaxc = i;
					if (i < zminc) zminc = i;
					if (j > ymaxc) ymaxc = j;
					if (j < yminc) yminc = j;
					if (k > xmaxc) xmaxc = k;
					if (k < xminc) xminc = k;
				}

				if (wat > 0.0f) {
					if (i > zmaxr) zmaxr = i;
					if (i < zminr) zminr = i;
					if (j > ymaxr) ymaxr = j;
					if (j < yminr) yminr = j;
					if (k > xmaxr) xmaxr = k;
					if (k < xminr) xminr = k;
				}
				*/

				//float mag = std::sqrt(entry * entry + entry2 * entry2 + entry3 * entry3);
				//if (mag > maxmag) maxmag = mag;
				//if (mag < minMag) minMag = mag;
					//if (xmin > k) xmin = k;
					//if (ymin > j) ymin = j;
					//if (zmin > i) zmin = i;
					//if (k > xmax) xmax = k;
					//if (j > ymax) ymax = j;
					//if (i > zmax) zmax = i;


				//writing pattern: v w u
				//unsigned char dv = (unsigned char)((entry2 + vOffset) / vStep);
				//unsigned char dw = (unsigned char)((entry3 + wOffset) / wStep);
				//unsigned char du = (unsigned char)((entry + uOffset) / uStep);

				//unsigned char dmag = (unsigned char)((mag) / magStep);

				file.write((char*)(&ice), sizeof(float));
				file.write((char*)(&clo), sizeof(float));
				file.write((char*)(&wat), sizeof(float));
				file.write((char*)(&pres), sizeof(float));
				//file.write((char*)(&type), sizeof(int));

				//file.write((char*)(&dv), sizeof(unsigned char));
				//file.write((char*)(&dw), sizeof(unsigned char));
				//file.write((char*)(&du), sizeof(unsigned char));
				//file.write((char*)(&dmag), sizeof(unsigned char));
			}
		}
	}


	file.close();

	std::cout << "min max pres: " << minval4 << " " << maxval4 << std::endl;

	//std::cout << std::endl << "nonzeros: " << nonzeros << std::endl; // just 2 Million actual entries above 0.00015
	//int z = zmin;
	//int y = ymin;
	//int x = xmin;
	//int z2 = zmax;
	//int y2 = ymax;
	//int x2 = xmax;
	/*
	std::cout << "nonzeros: " << nonzeros << std::endl;
	std::cout << "sum min/max: " << minval1 << " " << maxval1 << std::endl;

	std::cout << "min ice ijk:" << xmini << " " << ymini << " " << zmini << std::endl;
	std::cout << "max ice ijk:" << xmaxi << " " << ymaxi << " " << zmaxi << std::endl;
	std::cout << "min clo ijk:" << xminc << " " << yminc << " " << zminc << std::endl;
	std::cout << "max clo ijk:" << xmaxc << " " << ymaxc << " " << zmaxc << std::endl;
	std::cout << "min wat ijk:" << xminr << " " << yminr << " " << zminr << std::endl;
	std::cout << "max wat ijk:" << xmaxr << " " << ymaxr << " " << zmaxr << std::endl;
	*/

	/*
	std::cout << "bucket data: ice, cloud, water, type: " << std::endl;
	for (int i = 0; i < N + 1; i++) {
		//std::cout << buckets_ice[i] << ", ";
	}
	//std::cout << std::endl;
	for (int i = 0; i < N + 1; i++) {
		//std::cout << buckets_clo[i] << ", ";
	}
	//std::cout << std::endl;
	for (int i = 0; i < N + 1; i++) {
		//std::cout << buckets_wat[i] << ", ";
	}
	//std::cout << std::endl;
	for (int i = 0; i < N + 1; i++) {
		//std::cout << buckets_sum[i] << ", ";
	}
	std::cout << std::endl;
	*/
	
	/*
	for (int i = 0; i < 8; i++) {
		std::cout << type_bucket[i] << " ";
	}
	std::cout << std::endl;
	*/
	//std::cout << "u min: " << minval1 << " max: " << maxval1 << std::endl;
	//std::cout << "v min: " << minval2 << " max: " << maxval2 << std::endl;
	//std::cout << "w min: " << minval3 << " max: " << maxval3 << std::endl;

	//std::cout << "minmag: " << minMag << " maxmag: " << maxmag << std::endl;

	//std::cout << "first at: " << x << " " << y << " " << z << std::endl;
	//std::cout << "last at: " << x2 << " " << y2 << " " << z2 << std::endl;

	return 0;
}