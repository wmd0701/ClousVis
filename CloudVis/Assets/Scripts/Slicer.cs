using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteInEditMode]
public class Slicer : MonoBehaviour
{
	public float minDistance = 150.0f;
	public int HalvingThresholdV = 56;
	public int HalvingThreshold = 56;

	Vector3 VolumeBoundaryMin = new Vector3( -3752.0f, 1.83f, -3890.0f);
	//Vector3 VolumeBoundaryExtent = new Vector3(7504.0f, 198.17f, 7781.0f);
    //Vector3 VolumeVoxelExtent = new Vector3(1429.0f, 150.0f, 1556.0f);
    //Vector3 cellSize = new Vector3(5.25122463f, 1.3211333f, 5.00064267f);

	Vector3 viewHitPosition = new Vector3(0.0f, 0.0f, 0.0f);
	float rayDistance = 0.0f;
	bool viewValid = false;
	float cTanFoV2;
	float aspect;
	int multiplier = 1;

	float frustumRadius = 0.0f;
	Vector2Int yRange = new Vector2Int(1, 1);
	Vector2Int[] points;
	Vector3 cubeSize;
	Vector3 cubecenter;

	// Start is called before the first frame update
	void Start()
	{
		Camera camera = Camera.main;
		cTanFoV2 = 2.0f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		aspect = camera.aspect;
	}

	void Awake()
	{
		Camera camera = Camera.main;
		cTanFoV2 = 2.0f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		aspect = camera.aspect;
	}

	// Bresenhams line plotting algorithms according to https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
	void plotLineLow(int xs, int ys, int xe, int ye, ref Vector2Int[] points)
    {
		int dx = xe - xs;
		int dy = ye - ys;
		int yi = 1;

		if(dy < 0) { yi = -1; dy = -dy; }

		int D = (2 * dy) - dx;
		int y = ys;
		int i = 0;
		for (int x = xs; x <= xe; x++)
        {
			points[i] = new Vector2Int(x, y);
			if (D > 0)
            {
				y = y + yi;
				D = D + (2 * (dy - dx));
            }
            else
            {
				D = D + 2 * dy;
            }
			i++;
        }
    }
	void plotLineHigh(int xs, int ys, int xe, int ye, ref Vector2Int[] points)
	{
		int dx = xe - xs;
		int dy = ye - ys;
		int xi = 1;

		if (dx < 0) { xi = -1; dx = -dx; }

		int D = (2 * dx) - dy;
		int x = xs;
		int i = 0;
		for (int y = ys; y <= ye; y++)
		{
			points[i] = new Vector2Int(x, y);
			if (D > 0)
			{
				x = x + xi;
				D = D + (2 * (dx - dy));
			}
			else
			{
				D = D + 2 * dx;
			}
			i++;
		}
	}

	Vector2Int[] BresenHamLine(int xs, int ys, int xe, int ye)
    {
		int xdiff = Mathf.Abs(xe - xs);
		int ydiff = Mathf.Abs(ye - ys);

		Vector2Int[] pointsOnLine = new Vector2Int[Mathf.Max(xdiff,ydiff)+1];

		if(ydiff < xdiff)
        {
			if (xs > xe) plotLineLow(xe, ye, xs, ys, ref pointsOnLine);
			else plotLineLow(xs, ys, xe, ye, ref pointsOnLine);
        }
        else
        {
			if (ys > ye) plotLineHigh(xe, ye, xs, ys, ref pointsOnLine);
			else plotLineHigh(xs, ys, xe, ye, ref pointsOnLine);
        }

		return pointsOnLine;
    }

	// Update which voxel data to draw once per frame...
	void Update()
	{
		// find position where the Volume is Hit (at least minDistance from camera)
		RaycastHit rayHit;
		if (Physics.Raycast(transform.position, transform.forward, out rayHit, 15000.0f)) viewValid = true; 
		rayDistance = Mathf.Max(minDistance, rayHit.distance);
		viewHitPosition = transform.position + transform.forward * rayDistance;
		Vector3 relViewHitPos = viewHitPosition - VolumeBoundaryMin;

		// TODO: adjust validity depending on angle to be more inclusive... see corner cases (when height is a bit lower than boundary, but column above mostly visible still -> do not invalidate.
		if (relViewHitPos.x < 0.0f || relViewHitPos.y < 0.0f || relViewHitPos.z < 0.0f ||
			relViewHitPos.x > 7504.0f || relViewHitPos.y > 198.17f || relViewHitPos.z > 7781.0f) viewValid = false;
		else viewValid = true;

		// do different behaviour on horizontal slab!

		if (viewValid)
        {
			// compute area of volume at that distance!
			float FrustumHeight = rayDistance * cTanFoV2;
			float FrustumWidth = FrustumHeight * aspect;
			float halfFrustumHeight = FrustumHeight * 0.4f;
			float halfFrustumWidth = FrustumWidth * 0.4f;

			Vector3Int centerVoxel = new Vector3Int((int) Mathf.Floor(relViewHitPos.x / 5.25122463f), 
													(int) Mathf.Floor(relViewHitPos.y / 1.3211333f), 
													(int) Mathf.Floor(relViewHitPos.z / 5.00064267f));

			if (transform.rotation.eulerAngles.x < 45.0f && transform.rotation.eulerAngles.x > -45.0f) // use vertical slicing...
            {

				frustumRadius = FrustumWidth * 0.4f;
				
				float orthoDirx = transform.right.x * halfFrustumWidth;
				//float orthoDiry = transform.right.y * halfFrustumHeight; // maybe for actual slice parallel to cam.
				float orthoDirz = transform.right.z * halfFrustumWidth;

				int voxelHalfSpanx = (int)Mathf.Ceil(Mathf.Abs(orthoDirx) / (7504.0f / 1429.0f));
				int voxelHalfSpany = (int)Mathf.Ceil(Mathf.Abs(halfFrustumHeight) / (198.17f / 150.0f));
				int voxelHalfSpanz = (int)Mathf.Ceil(Mathf.Abs(orthoDirz) / (7781.0f / 1556.0f));


				//Vector3Int voxelBoundsMin = new Vector3Int(centerVoxel.x - voxelHalfSpanx, centerVoxel.y - voxelHalfSpany, centerVoxel.z - voxelHalfSpanz);
				//Vector3Int voxelBoundsMax = new Vector3Int(centerVoxel.x + voxelHalfSpanx, centerVoxel.y + voxelHalfSpany, centerVoxel.z + voxelHalfSpanz);

				cubecenter = new Vector3(centerVoxel.x * 5.25122463f + VolumeBoundaryMin.x + 5.25122463f / 2.0f,
											centerVoxel.y * 1.3211333f + VolumeBoundaryMin.y + 1.3211333f / 2.0f,
											centerVoxel.z * 5.00064267f + VolumeBoundaryMin.z + 5.00064267f / 2.0f);

				// get index limits regarding boundaries and frustum size
				int xMin = centerVoxel.x - voxelHalfSpanx; xMin = xMin < 0 ? 0 : xMin;
				int xMax = centerVoxel.x + voxelHalfSpanx; xMax = xMax > 1428 ? 1428 : xMax;

				int yMin = centerVoxel.y - voxelHalfSpany; yMin = yMin < 0 ? 0 : yMin;
				int yMax = centerVoxel.y + voxelHalfSpany; yMax = yMax > 149 ? 149 : yMax;

				int zMin = centerVoxel.z - voxelHalfSpanz; zMin = zMin < 0 ? 0 : zMin;
				int zMax = centerVoxel.z + voxelHalfSpanz; zMax = zMax > 1556 ? 1556 : zMax;

				// TODO: finer grained adaptive sampling
				// another idea: if ratio between different resolutions is not even enough -> just sample every other element in that dimension... needs to keep track of offsets and reset when a global halving occurs...
				int maxRange = Mathf.Max(xMax - xMin, zMax - zMin, yMax-yMin);
				multiplier = 1;
				while (maxRange > HalvingThresholdV)
				{
					maxRange /= 2;
					multiplier *= 2;
				}
				xMin = (int) Mathf.Floor(xMin / multiplier);
				xMax = (int) Mathf.Ceil(xMax / multiplier);
				yMin = (int) Mathf.Floor(yMin / multiplier);
				yMax = (int) Mathf.Ceil(yMax / multiplier);
				zMin = (int) Mathf.Floor(zMin / multiplier);
				zMax = (int) Mathf.Ceil(zMax / multiplier);
				//Debug.Log((xMax - xMin + 1) * (zMax - zMin + 1));

				yRange = new Vector2Int(yMin, yMax);
			
				int boundedX = Mathf.Max(1,xMax - xMin);
				int boundedXCenter = (xMax - xMin) / 2 - voxelHalfSpanx;
				boundedXCenter = centerVoxel.x > 778 ? boundedXCenter : -boundedXCenter;

				int boundedY = yMax - yMin; // not required -> ALWAYS FULL HEIGHT!
				int boundedYCenter = (yMax - yMin) / 2 - voxelHalfSpany;
				boundedYCenter = centerVoxel.y > 75 ? boundedYCenter : -boundedYCenter;

				int boundedZ = Mathf.Max(1,zMax - zMin);
				int boundedZCenter = (zMax - zMin) / 2 - voxelHalfSpanz;
				boundedZCenter = centerVoxel.z > 715 ? boundedZCenter : -boundedZCenter;

				cubecenter.x = (centerVoxel.x + boundedXCenter) * 5.25122463f + VolumeBoundaryMin.x;
				cubecenter.y = 1.83f + 1.3211333f / 2.0f;
				cubecenter.z = (centerVoxel.z + boundedZCenter) * 5.00064267f + VolumeBoundaryMin.z;

				cubeSize.x = boundedX * 5.25122463f;
				cubeSize.z = boundedZ * 5.00064267f;

				// gather coordinates of thin line:

				Vector3 rot = transform.rotation.eulerAngles;
				// if rot > 360 modulo it.
				if(rot.y > 0.0f && rot.y <= 90.0f)
                {
					Vector2Int[] gridPoints = BresenHamLine(xMin, zMax, xMax, zMin);
					points = gridPoints;
                }
                else if (rot.y > 90.0f && rot.y <= 180.0f)
                {
					Vector2Int[] gridPoints = BresenHamLine(xMax, zMax, xMin, zMin);
					points = gridPoints;
				}
				else if (rot.y > 180.0f && rot.y <= 270.0f)
                {
					Vector2Int[] gridPoints = BresenHamLine(xMax, zMin, xMin, zMax);
					points = gridPoints;
				}
				else
                {
					Vector2Int[] gridPoints = BresenHamLine(xMin, zMin, xMax, zMax);
					points = gridPoints;
				}

				// TODO:
				// add points for y range and access according values from Data of choice... (just indexed access with all these x,y,z coordinates.)
				// then add this data in an instantiated renderer (variable size with some sort of pooling possible)
			}
            else // use horizontal slicing!
            {
				frustumRadius = FrustumHeight * 0.4f;
				int voxelHalfSpanx = 0;
				int voxelHalfSpanz = 0;

				Vector3 rot = transform.rotation.eulerAngles;
				if (rot.y > 0.0f && rot.y <= 45.0f || rot.y > 135.0f && rot.y <= 225.0f || rot.y > 315.0f && rot.y <= 360.0f)
                {
					float orthoDirx = transform.right.x * halfFrustumWidth;
					voxelHalfSpanx = (int)Mathf.Ceil(Mathf.Abs(orthoDirx) / (7504.0f / 1429.0f));
					voxelHalfSpanz = (int)Mathf.Ceil(Mathf.Abs(halfFrustumHeight) / (7781.0f / 1556.0f));
				}
                else
                {
					float orthoDirx = transform.right.z * halfFrustumWidth;
					Debug.Log(orthoDirx);
					voxelHalfSpanx = (int)Mathf.Ceil(Mathf.Abs(halfFrustumHeight) / (7504.0f / 1429.0f));
					voxelHalfSpanz = (int)Mathf.Ceil(Mathf.Abs(orthoDirx) / (7781.0f / 1556.0f));
				}

				// get index limits regarding boundaries and frustum size
				int xMin = centerVoxel.x - voxelHalfSpanx; xMin = xMin < 0 ? 0 : xMin;
				int xMax = centerVoxel.x + voxelHalfSpanx; xMax = xMax > 1428 ? 1428 : xMax;

				int zMin = centerVoxel.z - voxelHalfSpanz; zMin = zMin < 0 ? 0 : zMin;
				int zMax = centerVoxel.z + voxelHalfSpanz; zMax = zMax > 1556 ? 1556 : zMax;


				// sample lower resolutions when it gets too large:
				int maxRange = Mathf.Max(xMax - xMin, zMax - zMin);
				multiplier = 1;
				while(maxRange > HalvingThreshold)
                {
					maxRange /= 2;
					multiplier *= 2;
                }
				xMin = (int)Mathf.Floor(xMin / multiplier);
				xMax = (int)Mathf.Ceil(xMax / multiplier);
				zMin = (int)Mathf.Floor(zMin / multiplier);
				zMax = (int)Mathf.Ceil(zMax / multiplier);

				Vector2Int[] gridPoints = new Vector2Int[(xMax - xMin+1) * (zMax - zMin+1)];

				int counter = 0;
				for(int i = xMin; i <= xMax; i++)
                {
					for(int j = zMin; j <= zMax; j++)
                    {
						gridPoints[counter] = new Vector2Int(i, j);
						counter++;
                    }
                }
				points = gridPoints;
				
				yRange.x = (int)Mathf.Floor(centerVoxel.y / multiplier);
				yRange.y = (int)Mathf.Floor(centerVoxel.y / multiplier);
			}
        }
	}

	private void OnDrawGizmos()
	{
		if (!viewValid) Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
		else Gizmos.color = new Color(0.0f, 1.0f, 0.0f);
		Gizmos.DrawWireSphere(transform.position + transform.forward * rayDistance, frustumRadius);
	    Gizmos.DrawLine(transform.position, transform.position + transform.forward * rayDistance);
		//Gizmos.DrawSphere(transform.position + transform.forward * rayDistance, 2.5f);
		Gizmos.DrawCube(cubecenter, cubeSize);

		// Show the corners.
		Gizmos.color = Color.blue;
		Gizmos.color = Color.green;
		Gizmos.color = new Color(0.0f, 1.0f, 0.0f);
		
		/*
		for (int y = yRange.x; y <= yRange.y; y+=2)
        {
			for (int i = 0; i < points.Length; i++)
			{
				Vector3 center = Generate3DPoint(y, points[i]);
				Gizmos.DrawSphere(center, 1.5f * multiplier);
			}
        }
		*/
	}

	/**
	 * Takes a point generated on a Breshenham-line and generates a corresponding 3D point in world space.
	 * @param y: Vertical offset.
	 * @param point: Point on Bresenham-line.
	 */
	private Vector3 Generate3DPoint(float y, Vector2 point) {
		float xComp = point.x * 5.25122463f * multiplier + VolumeBoundaryMin.x + 5.25122463f * multiplier / 2.0f;
		float yComp = y * 1.3211333f * multiplier + VolumeBoundaryMin.y + 1.3211333f * multiplier / 2.0f;
		float zComp = point.y * 5.00064267f * multiplier + VolumeBoundaryMin.z + 5.00064267f * multiplier / 2.0f;
		return new Vector3(xComp, yComp, zComp);
	}

	/**
	 * Returns the corner points of the current slice.
	 * @return: An Vector3[] of size 4 where each entry is a corner point.
	 */
	public Vector3[] GetSliceCorners() {
		float yMin = yRange.x;
		float yMax = yRange.y;
		Vector3[] corners = new Vector3[4];
		corners[0] = Generate3DPoint(yMin, points[0]);
		corners[1] = Generate3DPoint(yMin, points[points.Length - 1]);
		corners[2] = Generate3DPoint(yMax, points[0]);
		corners[3] = Generate3DPoint(yMax, points[points.Length - 1]);
		return corners;
	}

}
