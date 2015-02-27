using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class ThreadSafeDestruction {

    private Vector3 hitPoint;
    private float hitForce;

    private PhysicalProperties physicalProperties;

    private Vector3 hit = new Vector3();

    private List<Vector3> voronoiPoints = new List<Vector3>();
    private List<Vector3> voronoiWorldPoints = new List<Vector3>();

    private Dictionary<short, Fragment> fragmentExtents = new Dictionary<short, Fragment>();
    private short[, ,] voronoiDiagram;

    private ThreadSafeVoxelisation.Voxelization.AABCGrid aabcGrid;

    private List<Color> colors = new List<Color>();

    public Dictionary<short, Fragment> Fragment(ThreadSafeVoxelisation.Voxelization.AABCGrid grid, Vector3 hitPoint, float hitForce, PhysicalProperties physicalProperties) {
        this.aabcGrid = grid;
        this.hitPoint = hitPoint;
        this.hitForce = hitForce;
        this.physicalProperties = physicalProperties;

        findHitVoxel(hitPoint);
        generateVoronoiPoints(calcRadius(hitForce), calcNumberOfPoints(hitForce));
        colourVoxels();
        FindIslands();
        return fragmentExtents;
    }

    public short[, ,] getVoronoiDiagram() { return voronoiDiagram; }

    public Dictionary<short, Fragment> getFragmentExtents() { return fragmentExtents; }

    private void findHitVoxel(Vector3 hitPoint) {

        ThreadSafeVoxelisation.GridSize gridSize = aabcGrid.GetSize();
        Vector3 center = aabcGrid.GetCenter();

        Vector3 difference = hitPoint - center;

        short xOff, yOff, zOff;

        xOff = (short)(difference.x / gridSize.side);
        yOff = (short)(difference.y / gridSize.side);
        zOff = (short)(difference.z / gridSize.side);

        hit.x = (short)(gridSize.x / 2 + xOff);
        hit.y = (short)(gridSize.y / 2 + yOff);
        hit.z = (short)(gridSize.z / 2 + zOff);

    }

    private float gaussianRandom() {
        float u1 = Random.value;
        float u2 = Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                        Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)
        float randNormal =
                        (1 / 3f) * randStdNormal; //random normal(mean,stdDev^2)

        if (Mathf.Abs(randNormal) > 1) randNormal /= Mathf.Abs(randNormal);

        return randNormal;
    }

    private Vector3 gaussianVector(float radius) {

        Vector3 v = Vector3.one;

        while (v.magnitude > 1.0f) {
            v = new Vector3(gaussianRandom(), gaussianRandom(), gaussianRandom());
        }

        return new Vector3(radius * v.x, radius * v.y, radius * v.z);

    }

    private Vector3 toWorldSpace(Vector3 voxelSpace) {
        ThreadSafeVoxelisation.GridSize gridSize = aabcGrid.GetSize();
        float x = (voxelSpace.x - (gridSize.x * 0.5f)) * gridSize.side;
        float y = (voxelSpace.y - (gridSize.y * 0.5f)) * gridSize.side;
        float z = (voxelSpace.z - (gridSize.z * 0.5f)) * gridSize.side;
        return new Vector3(x, y, z);
    }

    private Vector3 toVoxelSpace(Vector3 worldSpace) {
        float side = aabcGrid.GetSize().side;

        return new Vector3(worldSpace.x / side, worldSpace.y / side, worldSpace.z / side);
    }

    private Vector3 toVoxelGridSpace(Vector3 worldSpace) {
        Vector3 grid = new Vector3((int)(worldSpace.x + hit.x), (int)(worldSpace.y + hit.y), (int)(worldSpace.z + hit.z));
        if (grid.x < 0 || grid.y < 0 || grid.z < 0) {
            return Vector3.zero;
        }
        if (grid.x >= aabcGrid.GetSize().x || grid.y >= aabcGrid.GetSize().y || grid.z >= aabcGrid.GetSize().z) {
            return Vector3.zero;
        }
        if (aabcGrid.IsAABCSet((short)grid.x, (short)grid.y, (short)grid.z)) {
            return grid;
        }
        return Vector3.zero;
    }

    private void generateVoronoiPoints(float radius, int numPoints) {
        int i = 0;
        while (i < numPoints) {
            Vector3 voronoi = gaussianVector(radius);
            Vector3 voxelVoronoi = toVoxelGridSpace(toVoxelSpace(voronoi));
            if (voxelVoronoi != Vector3.zero) {
                voronoiWorldPoints.Add(voronoi);
                voronoiPoints.Add(voxelVoronoi);
                colors.Add(new Color(Random.value, Random.value, Random.value, 0.5f));
                i++;
            }
        }

        colors.Add(new Color(Random.value, Random.value, Random.value, 0.5f));
        colors.Add(new Color(Random.value, Random.value, Random.value, 0.5f));
    }

    private void colourVoxels() {
        if (voronoiPoints.Count == 0) {
            return;
        }

        var gridSize = aabcGrid.GetSize();
        voronoiDiagram = new short[gridSize.x, gridSize.y, gridSize.z];

        for (short i = 1; i <= voronoiPoints.Count + 1; i++) {
            fragmentExtents.Add(i, new Fragment(i));
        }

        for (short x = 0; x < gridSize.x; ++x) {
            for (short y = 0; y < gridSize.y; ++y) {
                for (short z = 0; z < gridSize.z; ++z) {
                    Vector3 voxel = new Vector3(x, y, z);
                    short index = 0;
                    if (aabcGrid.IsAABCSet(x, y, z)) {

                        bool main = false;
                        float magnitude = float.MaxValue;

                        for (short i = 0; i < voronoiPoints.Count; i++) {
                            Vector3 point = voronoiPoints[i];
                            float thisMagnitude = (point - voxel).magnitude;
                            if (thisMagnitude < magnitude) {
                                magnitude = thisMagnitude;
                                index = (short)(i + 2);
                            }
                        }

                        float radiusMagnitude = ((1.5f * calcRadius(hitForce)) / gridSize.side) - ((voxel - hit).magnitude);
                        if (radiusMagnitude < magnitude) {
                            index = 1;
                            main = true;
                        }

                        Fragment colouring;
                        bool found = fragmentExtents.TryGetValue(index, out colouring);

                        colouring.UpdateMinX(x);
                        colouring.UpdateMinY(y);
                        colouring.UpdateMinZ(z);

                        colouring.UpdateMaxX(x);
                        colouring.UpdateMaxY(y);
                        colouring.UpdateMaxZ(z);

                        colouring.vertices.Add(toWorldSpace(new Vector3(x, y, z)));

                    }
                    voronoiDiagram[x, y, z] = index;
                }
            }
        }

    }

    public void FindIslands() {

        var gridSize = aabcGrid.GetSize();

        Dictionary<short, Fragment> newDict = new Dictionary<short, Fragment>();

        short i = (short)(fragmentExtents.Count + 10);

        foreach (Fragment colouring in fragmentExtents.Values) {

            for (short x = colouring.minX; x <= colouring.maxX; x++) {
                for (short y = colouring.minY; y <= colouring.maxY; y++) {
                    for (short z = colouring.minZ; z <= colouring.maxZ; z++) {
                        if (voronoiDiagram[x, y, z] == colouring.colour) {
                            short newColour = (short)(i++);
                            Fragment newColouring = new Fragment(newColour);
                            newDict.Add(newColour, newColouring);
                            Flood(x, y, z, colouring.colour, newColour, newDict, newColouring);
                        }
                    }
                }
            }

        }

        fragmentExtents = newDict;

    }

    private void Flood(short x, short y, short z, short colour, short newColour, Dictionary<short, Fragment> newDict, Fragment colouring) {

        Queue<Vector3> neighbours = new Queue<Vector3>();
        HashSet<string> visited = new HashSet<string>();

        neighbours.Enqueue(new Vector3(x, y, z));
        visited.Add(x + "," + y + "," + z);

        while (neighbours.Count > 0) {

            Vector3 deq = neighbours.Dequeue();

            x = (short)deq.x;
            y = (short)deq.y;
            z = (short)deq.z;

            voronoiDiagram[x, y, z] = newColour;

            colouring.UpdateMinX(x);
            colouring.UpdateMinY(y);
            colouring.UpdateMinZ(z);

            colouring.UpdateMaxX(x);
            colouring.UpdateMaxY(y);
            colouring.UpdateMaxZ(z);

            colouring.vertices.Add(new Vector3(x, y, z));


            for (short i = -1; i <= 1; i++) {
                for (short j = -1; j <= 1; j++) {
                    for (short k = -1; k <= 1; k++) {
                        short xi = (short)(x + i);
                        short yj = (short)(y + j);
                        short zk = (short)(z + k);

                        if (xi < 0 || yj < 0 || zk < 0) continue;
                        if (i == 0 && j == 0 && k == 0) continue;
                        if (xi >= voronoiDiagram.GetLength(0) || yj >= voronoiDiagram.GetLength(1) || zk >= voronoiDiagram.GetLength(2)) continue;

                        if (voronoiDiagram[xi, yj, zk] == colour) {
                            if (!(visited.Contains(xi + "," + yj + "," + zk))) {
                                neighbours.Enqueue(new Vector3(xi, yj, zk));
                                visited.Add(xi + "," + yj + "," + zk);
                            }
                        }
                    }
                }
            }
        }
    }

    private float calcRadius(float hitForce) {
        //TODO physics
        return Mathf.Pow(hitForce, 1f / 3f);
    }

    private int calcNumberOfPoints(float hitForce) {
        //TODO physics
        return 20;
    }

    public void Update() {
        if (voronoiPoints.Count > 0) {
            foreach (Vector3 vector in voronoiWorldPoints) {
                Debug.DrawLine(hitPoint, hitPoint + vector, Color.red);
            }
        }
    }

}
