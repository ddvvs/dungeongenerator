using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int dungeonWidth = 50;
    public int dungeonHeight = 50;
    public int minRoomSize = 5;
    public int splitDepth = 4;

    [Header("Debugging")]
    public bool drawDebugLines = true;
    public bool drawDoors = true;
    public bool drawNodes = true;
    public float animationSpeed = 1f;

    private List<RectInt> rooms = new List<RectInt>();
    private Dictionary<RectInt, List<RectInt>> dungeonGraph = new Dictionary<RectInt, List<RectInt>>();
    private Coroutine drawingCoroutine;

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        rooms.Clear();
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
        RectInt dungeonSpace = new RectInt(0, 0, dungeonWidth, dungeonHeight); //define og dungeon as a rectangle

        SplitRoom(dungeonSpace, splitDepth); //start splitting

        // add rooms as nodes to the graph
        foreach (RectInt room in rooms)
        {
            dungeonGraph[room] = new List<RectInt>(); // this makes the adjacency list for every room
        }
    }

    void SplitRoom(RectInt room, int depth)
    {
        if (depth <= 0 || room.width < minRoomSize * 2 || room.height < minRoomSize * 2) //so if room too small then dont split
        {
            rooms.Add(room);
            return;
        }

        bool splitHorizontally = Random.value > 0.5f; //split vertically or horizontally?

        // split along the longer axis to show more consistency
        if (room.width > room.height)
        {
            splitHorizontally = false;
        }
        else if (room.height > room.width)
        {
            splitHorizontally = true;
        }

        if (splitHorizontally)
        {
            // horiz split
            int splitY = Random.Range(room.y + minRoomSize, room.y + room.height - minRoomSize);
            RectInt room1 = new RectInt(room.x, room.y, room.width, splitY - room.y);
            RectInt room2 = new RectInt(room.x, splitY, room.width, room.y + room.height - splitY);

            // split new rooms recursively 
            SplitRoom(room1, depth - 1);
            SplitRoom(room2, depth - 1);
        }
        else
        {
            // vert split
            int splitX = Random.Range(room.x + minRoomSize, room.x + room.width - minRoomSize);
            RectInt room1 = new RectInt(room.x, room.y, splitX - room.x, room.height);
            RectInt room2 = new RectInt(splitX, room.y, room.x + room.width - splitX, room.height);

            // split new rooms recursively 
            SplitRoom(room1, depth - 1);
            SplitRoom(room2, depth - 1);
        }
    }

    void CreateDoors()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                RectInt room1 = rooms[i];
                RectInt room2 = rooms[j];

                // check if rooms are adjacent and add edge between them
                if (AreRoomsAdjacent(room1, room2, out Vector3 doorPosition))
                {
                    dungeonGraph[room1].Add(room2);
                    dungeonGraph[room2].Add(room1);
                }
            }
        }
    }

    bool AreRoomsAdjacent(RectInt room1, RectInt room2, out Vector3 doorPosition)
    {
        doorPosition = Vector3.zero;

        // check if room2 is to the right of room1
        if (room1.x + room1.width == room2.x && room1.y < room2.y + room2.height && room1.y + room1.height > room2.y)
        {
            int overlapStart = Mathf.Max(room1.y, room2.y);
            int overlapEnd = Mathf.Min(room1.y + room1.height, room2.y + room2.height);
            int doorY = (overlapStart + overlapEnd) / 2;
            doorPosition = new Vector3(room1.x + room1.width, 0, doorY);
            return true;
        }

        // check if room2 is to the left of room1
        if (room2.x + room2.width == room1.x && room1.y < room2.y + room2.height && room1.y + room1.height > room2.y)
        {
            int overlapStart = Mathf.Max(room1.y, room2.y);
            int overlapEnd = Mathf.Min(room1.y + room1.height, room2.y + room2.height);
            int doorY = (overlapStart + overlapEnd) / 2;
            doorPosition = new Vector3(room1.x, 0, doorY);
            return true;
        }

        // check if room2 is above room1
        if (room1.y + room1.height == room2.y && room1.x < room2.x + room2.width && room1.x + room1.width > room2.x)
        {
            int overlapStart = Mathf.Max(room1.x, room2.x);
            int overlapEnd = Mathf.Min(room1.x + room1.width, room2.x + room2.width);
            int doorX = (overlapStart + overlapEnd) / 2;
            doorPosition = new Vector3(doorX, 0, room1.y + room1.height);
            return true;
        }

        // check if room2 is below room1
        if (room2.y + room2.height == room1.y && room1.x < room2.x + room2.width && room1.x + room1.width > room2.x)
        {
            int overlapStart = Mathf.Max(room1.x, room2.x);
            int overlapEnd = Mathf.Min(room1.x + room1.width, room2.x + room2.width);
            int doorX = (overlapStart + overlapEnd) / 2;
            doorPosition = new Vector3(doorX, 0, room1.y);
            return true;
        }

        return false;
    }

    void EnsureConnectivity()
    {
        if (rooms.Count > 0)
        {
            HashSet<RectInt> visited = new HashSet<RectInt>();
            Queue<RectInt> queue = new Queue<RectInt>();

            RectInt startRoom = rooms[0];
            queue.Enqueue(startRoom);
            visited.Add(startRoom);

            while (queue.Count > 0)
            {
                RectInt current = queue.Dequeue();
                foreach (RectInt neighbor in dungeonGraph[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (visited.Count != rooms.Count)
            {
                Debug.LogError("dungeon is not fully connected!");
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
            foreach (var room in dungeonGraph.Keys)
            {
                foreach (RectInt neighbor in dungeonGraph[room])
                {
                    if (AreRoomsAdjacent(room, neighbor, out Vector3 doorPosition))
                    {
                        yield return StartCoroutine(DrawDoorAnimated(doorPosition));
                    }
                }
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

        foreach (var room in dungeonGraph.Keys)
        {
            foreach (RectInt neighbor in dungeonGraph[room])
            {
                Vector3 start = new Vector3(room.x + room.width / 2f, 0, room.y + room.height / 2f);
                Vector3 end = new Vector3(neighbor.x + neighbor.width / 2f, 0, neighbor.y + neighbor.height / 2f);

                // gets the door positiobn between the 2 rooms
                if (AreRoomsAdjacent(room, neighbor, out Vector3 doorPosition))
                {
                    yield return StartCoroutine(DrawEdgeAnimated(start, end, doorPosition));
                }
            }
        }
    }

    IEnumerator DrawRoomAnimated(RectInt room)
    {
        Vector3 start = new Vector3(room.x, 0, room.y);
        Vector3 end = new Vector3(room.x + room.width, 0, room.y + room.height);
        float elapsedTime = 0f;

        while (elapsedTime < animationSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationSpeed);
            Vector3 currentEnd = Vector3.Lerp(start, end, t);

            Debug.DrawLine(new Vector3(room.x, 0, room.y), new Vector3(currentEnd.x, 0, room.y), Color.red, Mathf.Infinity); // bottom
            Debug.DrawLine(new Vector3(room.x, 0, room.y), new Vector3(room.x, 0, currentEnd.z), Color.red, Mathf.Infinity); // left
            Debug.DrawLine(new Vector3(room.x + room.width, 0, room.y), new Vector3(room.x + room.width, 0, currentEnd.z), Color.red, Mathf.Infinity); // right
            Debug.DrawLine(new Vector3(room.x, 0, room.y + room.height), new Vector3(room.x + room.width, 0, room.y + room.height), Color.red, Mathf.Infinity); // top
            yield return null;
        }
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
            float t = Mathf.Clamp01(elapsedTime / animationSpeed);
            size = Mathf.Lerp(0f, maxSize, t);

            Debug.DrawLine(nodePosition - new Vector3(size, 0, 0), nodePosition + new Vector3(size, 0, 0), Color.magenta, Mathf.Infinity);
            Debug.DrawLine(nodePosition - new Vector3(0, 0, size), nodePosition + new Vector3(0, 0, size), Color.magenta, Mathf.Infinity);
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

            // draw 1st segment from the center of room1 to door
            Vector3 currentDoor = Vector3.Lerp(start, doorPosition, t);
            Debug.DrawLine(start, currentDoor, Color.green, Mathf.Infinity);

            // draw 1st segment from the door to center of room2
            Vector3 currentEnd = Vector3.Lerp(doorPosition, end, t);
            Debug.DrawLine(doorPosition, currentEnd, Color.green, Mathf.Infinity);

            yield return null;
        }
    }
}
