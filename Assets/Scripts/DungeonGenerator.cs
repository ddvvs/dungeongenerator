using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int dungeonWidth;
    public int dungeonHeight;
    public int minRoomSize;
    public int maxSplits;
    public float stepDelay = 0.5f;

    private List<RectInt> rooms = new List<RectInt>();
    private List<Vector2Int> doors = new List<Vector2Int>();
    private Queue<RectInt> animationQueue = new Queue<RectInt>();

    private HashSet<RectInt> drawnRooms = new HashSet<RectInt>();
    private HashSet<Vector2Int> drawnDoors = new HashSet<Vector2Int>();

    void Start()
    {
        StartCoroutine(GenerateDungeonAnimated());
    }

    IEnumerator GenerateDungeonAnimated()
    {
        RectInt mainRoom = new RectInt(0, 0, dungeonWidth, dungeonHeight);
        drawnRooms.Add(mainRoom);
        yield return new WaitForSeconds(stepDelay);

        SplitRoom(mainRoom, maxSplits);
        StartCoroutine(AnimateDungeon());
    }

    void SplitRoom(RectInt room, int depth)
    {
        if (depth <= 0 || room.width < minRoomSize * 2 || room.height < minRoomSize * 2)
        {
            rooms.Add(room);
            animationQueue.Enqueue(room);
            return;
        }

        bool splitHorizontally = Random.value > 0.5f;
        if (room.width > room.height) splitHorizontally = false;
        if (room.height > room.width) splitHorizontally = true;

        if (splitHorizontally)
        {
            int splitY = Random.Range(room.y + minRoomSize, room.y + room.height - minRoomSize);
            RectInt room1 = new RectInt(room.x, room.y, room.width, splitY - room.y);
            RectInt room2 = new RectInt(room.x, splitY, room.width, room.y + room.height - splitY);

            SplitRoom(room1, depth - 1);
            SplitRoom(room2, depth - 1);
        }
        else
        {
            int splitX = Random.Range(room.x + minRoomSize, room.x + room.width - minRoomSize);
            RectInt room1 = new RectInt(room.x, room.y, splitX - room.x, room.height);
            RectInt room2 = new RectInt(splitX, room.y, room.x + room.width - splitX, room.height);

            SplitRoom(room1, depth - 1);
            SplitRoom(room2, depth - 1);
        }
    }

    IEnumerator AnimateDungeon()
    {
        while (animationQueue.Count > 0)
        {
            RectInt room = animationQueue.Dequeue();
            drawnRooms.Add(room);
            yield return new WaitForSeconds(stepDelay);
        }

        AddDoors();
        StartCoroutine(AnimateDoors());
    }

    void AddDoors()
    {
        HashSet<Vector2Int> placedDoors = new HashSet<Vector2Int>();

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                RectInt roomA = rooms[i];
                RectInt roomB = rooms[j];

                if (roomA.xMax == roomB.x && roomA.yMin < roomB.yMax && roomA.yMax > roomB.yMin)
                {
                    int minY = Mathf.Max(roomA.yMin, roomB.yMin);
                    int maxY = Mathf.Min(roomA.yMax, roomB.yMax) - 1;

                    if (maxY > minY)
                    {
                        int doorY = Random.Range(minY + 1, maxY);
                        Vector2Int doorPos = new Vector2Int(roomA.xMax, doorY);

                        if (placedDoors.Add(doorPos))
                            doors.Add(doorPos);
                    }
                }

                else if (roomA.yMax == roomB.y && roomA.xMin < roomB.xMax && roomA.xMax > roomB.xMin)
                {
                    int minX = Mathf.Max(roomA.xMin, roomB.xMin);
                    int maxX = Mathf.Min(roomA.xMax, roomB.xMax) - 1;

                    if (maxX > minX)
                    {
                        int doorX = Random.Range(minX + 1, maxX);
                        Vector2Int doorPos = new Vector2Int(doorX, roomA.yMax);

                        if (placedDoors.Add(doorPos))
                            doors.Add(doorPos);
                    }
                }
            }
        }
    }

    IEnumerator AnimateDoors()
    {
        foreach (var door in doors)
        {
            drawnDoors.Add(door);
            yield return new WaitForSeconds(stepDelay);
        }
    }

    void Update()
    {
        foreach (var room in drawnRooms)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.red);
        }
        foreach (var door in drawnDoors)
        {
            AlgorithmsUtils.DebugPoint(door, Color.blue);
        }
    }
}
