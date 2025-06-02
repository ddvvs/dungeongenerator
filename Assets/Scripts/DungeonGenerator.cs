using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int dungeonWidth = 50;
    public int dungeonHeight = 50;
    public int minRoomSize = 5;
    public int splitDepth = 4;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public float floorTileSize = 1f;

    [Header("Debugging")]
    public bool drawDebugLines = true;
    public bool drawDoors = true;
    public bool drawNodes = true;
    public float animationSpeed = 1f;

    private List<RectInt> rooms = new List<RectInt>();
    private Dictionary<WallPair, Vector3> doorPositions = new Dictionary<WallPair, Vector3>();
    private HashSet<Vector3> wallPositions = new HashSet<Vector3>();
    private HashSet<Vector3> floorPositions = new HashSet<Vector3>();
    private Dictionary<RectInt, List<RectInt>> dungeonGraph = new Dictionary<RectInt, List<RectInt>>();
    private Coroutine drawingCoroutine;
    private NavMeshSurface navMeshSurface;

    //to prevent dupes
    private struct WallPair
    {
        public RectInt room1;
        public RectInt room2;

        public WallPair(RectInt r1, RectInt r2)
        {
            //sorts rooms so that r1 is always <r2 to prevent double entries
            if (r1.x < r2.x || (r1.x == r2.x && r1.y < r2.y))
            {
                room1 = r1;
                room2 = r2;
            }
            else
            {
                room1 = r2;
                room2 = r1;
            }
        }
    }

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        rooms.Clear();
        doorPositions.Clear();
        wallPositions.Clear();
        floorPositions.Clear();
        dungeonGraph.Clear();

        CreateRooms();
        CreateDoors();
        EnsureConnectivity();

        if (drawingCoroutine != null)
        {
            StopCoroutine(drawingCoroutine);
        }

        drawingCoroutine = StartCoroutine(DrawDungeonAnimated());
    }

    void CreateRooms()
    {
        RectInt dungeonSpace = new RectInt(0, 0, dungeonWidth, dungeonHeight);
        SplitRoom(dungeonSpace, splitDepth);

        foreach (RectInt room in rooms)
        {
            dungeonGraph[room] = new List<RectInt>();
        }
    }

    void SplitRoom(RectInt room, int depth)
    {
        // if max depth/room too small to split stop
        if (depth <= 0 || room.width < minRoomSize * 2 || room.height < minRoomSize * 2)
        {
            rooms.Add(room);
            return;
        }

        bool splitHorizontally = Random.value > 0.5f;

        //if wide do vert if tall do horiz
        if (room.width > room.height)
            splitHorizontally = false;
        else if (room.height > room.width)
            splitHorizontally = true;

        if (splitHorizontally)
        {
            int splitY = Random.Range(room.y + minRoomSize, room.y + room.height - minRoomSize); //to leave at least minroomsize for both sides

            //make 2 new rooms by cutting horizontally on splitY
            RectInt room1 = new RectInt(room.x, room.y, room.width, splitY - room.y);
            RectInt room2 = new RectInt(room.x, splitY, room.width, room.y + room.height - splitY);

            //split new rooms recursively
            SplitRoom(room1, depth - 1);
            SplitRoom(room2, depth - 1);
        }
        else
        {
            //same but vert
            int splitX = Random.Range(room.x + minRoomSize, room.x + room.width - minRoomSize);
            RectInt room1 = new RectInt(room.x, room.y, splitX - room.x, room.height);
            RectInt room2 = new RectInt(splitX, room.y, room.x + room.width - splitX, room.height);

            SplitRoom(room1, depth - 1);
            SplitRoom(room2, depth - 1);
        }
    }

    void CreateDoors()
    {
        Dictionary<WallPair, List<Vector3>> potentialDoors = new Dictionary<WallPair, List<Vector3>>();

        //loop thru room pairs without dupes
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                RectInt room1 = rooms[i];
                RectInt room2 = rooms[j];

                if (GetAdjacentWallPositions(room1, room2, out List<Vector3> positions)) //check if rooms share a wall
                {
                    WallPair wall = new WallPair(room1, room2); //normalize order
                    potentialDoors[wall] = positions; //store good doors
                }
            }
        }

        foreach (var wall in potentialDoors)
        {
            if (wall.Value.Count > 0)
            {
                Vector3 doorPos = wall.Value[Random.Range(0, wall.Value.Count)]; //choose random pos in wall
                doorPositions[wall.Key] = doorPos;

                //add to graph
                dungeonGraph[wall.Key.room1].Add(wall.Key.room2);
                dungeonGraph[wall.Key.room2].Add(wall.Key.room1);
            }
        }
    }

    bool GetAdjacentWallPositions(RectInt room1, RectInt room2, out List<Vector3> positions)
    {
        positions = new List<Vector3>();

        if (room1.x + room1.width == room2.x) //check if room1 is left to room2
        {
            //get overlapping vert range on Y
            int overlapStart = Mathf.Max(room1.y, room2.y);
            int overlapEnd = Mathf.Min(room1.y + room1.height, room2.y + room2.height);

            //if theres vert overlap put door
            if (overlapStart < overlapEnd)
            {
                for (int y = overlapStart + 1; y < overlapEnd; y++) //basically +1 to make sure door is not at corner
                {
                    positions.Add(new Vector3(room1.x + room1.width, 0, y));
                }
                return true;
            }
        }

        if (room2.x + room2.width == room1.x) //check if room 2 is left to room1
        {
            int overlapStart = Mathf.Max(room1.y, room2.y);
            int overlapEnd = Mathf.Min(room1.y + room1.height, room2.y + room2.height);

            if (overlapStart < overlapEnd)
            {
                for (int y = overlapStart + 1; y < overlapEnd; y++)
                {
                    positions.Add(new Vector3(room1.x, 0, y));
                }
                return true;
            }
        }

        if (room1.y + room1.height == room2.y) //check if room1 above room2
        {
            int overlapStart = Mathf.Max(room1.x, room2.x);
            int overlapEnd = Mathf.Min(room1.x + room1.width, room2.x + room2.width);

            if (overlapStart < overlapEnd)
            {
                for (int x = overlapStart + 1; x < overlapEnd; x++)
                {
                    positions.Add(new Vector3(x, 0, room1.y + room1.height));
                }
                return true;
            }
        }

        if (room2.y + room2.height == room1.y) //check if room2 above room1
        {
            int overlapStart = Mathf.Max(room1.x, room2.x);
            int overlapEnd = Mathf.Min(room1.x + room1.width, room2.x + room2.width);

            if (overlapStart < overlapEnd)
            {
                for (int x = overlapStart + 1; x < overlapEnd; x++)
                {
                    positions.Add(new Vector3(x, 0, room1.y));
                }
                return true;
            }
        }

        return false;
    }

    //straight up bfs after checking if theres at least 1 room
    void EnsureConnectivity()
    {
        if (rooms.Count > 0)
        {
            HashSet<RectInt> visited = new HashSet<RectInt>(); //keep track of visited rooms during bfs
            Queue<RectInt> queue = new Queue<RectInt>(); //queue to manage bfs traversal order (first in first out)

            //start bfs from first room in list
            RectInt startRoom = rooms[0];
            queue.Enqueue(startRoom);
            visited.Add(startRoom);

            //bfs to visit all reachable rooms
            while (queue.Count > 0)
            {
                //deque next room to process
                RectInt current = queue.Dequeue();

                //check all rooms connected to the current room
                foreach (RectInt neighbor in dungeonGraph[current])
                {
                    if (!visited.Contains(neighbor)) //if neighbor wasnt visited
                    {
                        visited.Add(neighbor); //add to visited
                        queue.Enqueue(neighbor); //enqueue it
                    }
                }
            }

            if (visited.Count != rooms.Count)
            {
                Debug.LogError("dungeon not fully connected");
            }
        }
    }

    IEnumerator DrawDungeonAnimated()
    {
        foreach (RectInt room in rooms)
        {
            yield return StartCoroutine(DrawRoomAnimated(room));
        }

        if (drawDoors)
        {
            foreach (var door in doorPositions.Values)
            {
                yield return StartCoroutine(DrawDoorAnimated(door));
            }
        }

        if (drawNodes)
        {
            foreach (RectInt room in rooms)
            {
                Vector3 nodePosition = new Vector3(room.x + room.width / 2f, 0, room.y + room.height / 2f);
                yield return StartCoroutine(DrawNodeAnimated(nodePosition));
            }
        }

        foreach (var connection in doorPositions)
        {
            Vector3 start = new Vector3(
                connection.Key.room1.x + connection.Key.room1.width / 2f,
                0,
                connection.Key.room1.y + connection.Key.room1.height / 2f);
            Vector3 end = new Vector3(
                connection.Key.room2.x + connection.Key.room2.width / 2f,
                0,
                connection.Key.room2.y + connection.Key.room2.height / 2f);

            yield return StartCoroutine(DrawEdgeAnimated(start, end, connection.Value));
        }

        yield return new WaitForEndOfFrame();
        navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
        navMeshSurface.collectObjects = CollectObjects.All;
        navMeshSurface.BuildNavMesh();
    }

    IEnumerator DrawRoomAnimated(RectInt room)
    {
        float elapsedTime = 0f;

        if (drawDebugLines)
        {
            Vector3 bottomLeft = new Vector3(room.x, 0, room.y);
            Vector3 bottomRight = new Vector3(room.x + room.width, 0, room.y);
            Vector3 topLeft = new Vector3(room.x, 0, room.y + room.height);
            Vector3 topRight = new Vector3(room.x + room.width, 0, room.y + room.height);

            Debug.DrawLine(bottomLeft, bottomRight, Color.red, Mathf.Infinity);
            Debug.DrawLine(bottomLeft, topLeft, Color.red, Mathf.Infinity);
            Debug.DrawLine(bottomRight, topRight, Color.red, Mathf.Infinity);
            Debug.DrawLine(topLeft, topRight, Color.red, Mathf.Infinity);
        }

        while (elapsedTime < animationSpeed)
        {
            elapsedTime += Time.deltaTime;

            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    Vector3 floorPos = new Vector3(x + 0.5f, 0, y + 0.5f); //center of tile
                    if (!floorPositions.Contains(floorPos)) //prevent dupe
                    {
                        Instantiate(floorPrefab, floorPos, Quaternion.Euler(90f, 0f, 0f));
                        floorPositions.Add(floorPos);
                    }
                }
            }

            for (int i = room.x; i <= room.x + room.width; i++)
            {
                Vector3 bottom = new Vector3(i * floorTileSize, 0, room.y * floorTileSize);
                Vector3 top = new Vector3(i * floorTileSize, 0, (room.y + room.height) * floorTileSize);

                //instantiate wall if not door position (also prevent dupes lol)
                if (!IsDoorPosition(bottom) && !wallPositions.Contains(bottom)) 
                { 
                    Instantiate(wallPrefab, bottom, Quaternion.identity); wallPositions.Add(bottom);
                }
                if (!IsDoorPosition(top) && !wallPositions.Contains(top))
                { 
                    Instantiate(wallPrefab, top, Quaternion.identity); wallPositions.Add(top); 
                }
            }

            for (int i = room.y; i <= room.y + room.height; i++)
            {
                Vector3 left = new Vector3(room.x * floorTileSize, 0, i * floorTileSize);
                Vector3 right = new Vector3((room.x + room.width) * floorTileSize, 0, i * floorTileSize);

                //instantiate wall if not door position (also prevent dupes lol)
                if (!IsDoorPosition(left) && !wallPositions.Contains(left)) 
                { 
                    Instantiate(wallPrefab, left, Quaternion.identity); wallPositions.Add(left); 
                }
                if (!IsDoorPosition(right) && !wallPositions.Contains(right)) 
                { 
                    Instantiate(wallPrefab, right, Quaternion.identity); wallPositions.Add(right); 
                }
            }

            yield return null;
        }
    }


    bool IsDoorPosition(Vector3 position)
    {
        foreach (var doorPos in doorPositions.Values)
        {
            //if position close to an existing door (less than 0.1) return true
            if (Vector3.Distance(doorPos, position) < 0.1f)
                return true;
        }
        return false;
    }

    IEnumerator DrawDoorAnimated(Vector3 doorPosition)
    {
        Vector3 start = doorPosition - new Vector3(0.5f, 0, 0);
        Vector3 end = doorPosition + new Vector3(0.5f, 0, 0);
        float elapsedTime = 0f;

        while (elapsedTime < animationSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationSpeed);
            Vector3 currentEnd = Vector3.Lerp(start, end, t);
            Debug.DrawLine(currentEnd - new Vector3(0, 0, 0.5f), currentEnd + new Vector3(0, 0, 0.5f), Color.blue, Mathf.Infinity);
            Debug.DrawLine(currentEnd - new Vector3(0.5f, 0, 0), currentEnd + new Vector3(0.5f, 0, 0), Color.blue, Mathf.Infinity);
            yield return null;
        }
    }

    IEnumerator DrawNodeAnimated(Vector3 nodePosition)
    {
        float size = 0.5f;
        float maxSize = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < animationSpeed)
        {
            elapsedTime += Time.deltaTime;
            size = Mathf.Lerp(0f, maxSize, Mathf.Clamp01(elapsedTime / animationSpeed));
            Debug.DrawRay(nodePosition + new Vector3(0, size, 0), Vector3.up * size, Color.red, Mathf.Infinity);
            yield return null;
        }
    }

    IEnumerator DrawEdgeAnimated(Vector3 start, Vector3 end, Vector3 doorPosition)
    {
        float elapsedTime = 0f;

        while (elapsedTime < animationSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationSpeed);

            Vector3 currentDoor = Vector3.Lerp(start, doorPosition, t);
            Debug.DrawLine(start, currentDoor, Color.green, Mathf.Infinity);

            Vector3 currentEnd = Vector3.Lerp(doorPosition, end, t);
            Debug.DrawLine(doorPosition, currentEnd, Color.green, Mathf.Infinity);

            yield return null;
        }
    }
}