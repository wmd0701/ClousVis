# Wind-flow visualization

<!---What was my meta-goal?-->
## Introduction
My contribution is the implementation of a real-time capable method to visualize wind-flow. 
<!---What is my high-level approach?-->
To that end, I have developed a method that allows for tracing of thousands of streamlines with a Runge-Kutta integrator of fourth order and visualizing them as stream-tubes in each frame. 
<!---How did I achieve that on a lower level.-->
To cope with the high computational load, the stream-tubes are traced and rendered in parallel on the GPU. To allow for efficient data-access and data-processing by the GPU, the vectorfield was encoded as a 3D-texture.

<!---I give a short recap of the data and explain how it was preprocessed.-->
## Data
The wind-flow data was provided as a wind-velocity vector at each grid-point, where the longitudinal, latitudinal and vertical components were given individually by scalar fields. In the following I will denote such a vector by ***x*** = [_u_, _v_, _w_], where _u_, _v_ and _w_ correspond to the longitudinal, latitudinal and altitude components respectively.

<!---What king of preprocessing was done? Why?-->
## Preprocessing
In a first step, the data was transformed into a 3D-texture, where the width and height of the texture capture the latitudinal and longitudinal spatial dimension, and the vertical dimension is encompassed by the depth of the texture. This was done to allow for efficient data-access by the GPU.

<!---How was the data accessed?-->
Since the ```.vti```-file-format in which the vector-data was represented is not directly readable by Unity, I have written a file that reads in the scalar values corresponding to the different vector components and subsequently dumps them into a binary file ([implementation](https://github.com/danielettog/CloudVis/blob/main/VTKscripts/vectorfield/TransformData.cpp)).

<!---How was the data encoded?-->
The vector at each grid-point was encoded as a single pixel in the texture. The components _u_, _v_ and _v_ were transformed and quantized such that they could be represented by an 8-bit color-channel. The following figure displays the resulting texture:
![Three slice planes in the 3D-texture which encodes the vectorfield. The red, green and blue channels encode the longitudinal, latitudinal and altitude wind-vector-components respectively](https://github.com/danielettog/CloudVis/blob/main/CloudVis/Assets/Scripts/VectorFieldVisualization/3dtexture.PNG)

## Streamline Tracing
Building on top of Gianluca's interactive slice plane, my method samples seed-points on the slice plane in a regular grid. Instead of tracing the streamline for each seedpoint sequentially, I leveraged the GPUs ability for parallel computation to trace the streamlines in parallel. This was done in a Compute Shader ([implementation](https://github.com/danielettog/CloudVis/blob/main/CloudVis/Assets/Scripts/VectorFieldVisualizationCompute.compute)), which is Unity's standard GPU-programming interface. A RK(4)-integrator was used to trace the streamlines in a first pass. The number streamlines, step-size and number of integration is adjustable.

##Construction of streamtubes
While the previously described computation would suffice to render simple streamlines (by simply connecting the integrated positions), an additional pass was needed to precompute the tangents and normals to the streamline. A conventional shader was used to then construct the tube-geometry from the tangents and normals at each streamline-position ([implementation](https://github.com/danielettog/CloudVis/blob/main/CloudVis/Assets/Scripts/VectorFieldVisualization/TubeShader.shader)).

## Appearance of Streamlines
To enhance the depth perception of the streamlines, they are rendered with a metallic look which creates easily distinguishable specular highlights. Furthermore, the tube geometry supports self-shadowing, which can provide further depth-cues to the viewer. A transfer function of the particle velocity is used for the base color of the tube. The following image qualitatively compares the visual appearance of the streamtubes with varying amounts of depth-cues:
![The material choice and shadowing can aid the viewers depth perception. Left: Simple diffuse shader. Middle: Specular highlights. Right: With self-shadowing.](https://github.com/danielettog/CloudVis/blob/main/CloudVis/Assets/Scripts/VectorFieldVisualization/depthcues.jpg)

## Performance Comparison
To highlight the fact that a GPU-based implementation was required to execute the described computation at interactive frame-rates, the following figure shows a frame-rate comparisones of a purely CPU-based and GPU-based approach for varying number of integration steps.
