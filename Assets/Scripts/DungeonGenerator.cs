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
    public float stepDelay;

    private List<RectInt> rooms = new List<RectInt>();

    void Start()
    {
        StartCoroutine(GenerateDungeon());
    }

    IEnumerator GenerateDungeon()
    {
        rooms.Clear();
        RectInt mainRoom = new RectInt(0, 0, dungeonWidth, dungeonHeight);
        yield return StartCoroutine(SplitRoom(mainRoom, maxSplits));
    }

    IEnumerator SplitRoom(RectInt room, int depth)
    {
        if (depth <= 0 || room.width < minRoomSize * 2 || room.height < minRoomSize * 2)
        {
            rooms.Add(room);
            yield return new WaitForSeconds(stepDelay);
            yield break;
        }

        bool splitHorizontally = Random.value > 0.5f;
        if (room.width > room.height) splitHorizontally = false;
        if (room.height > room.width) splitHorizontally = true;

        if (splitHorizontally)
        {
            int splitY = Random.Range(room.y + minRoomSize, room.y + room.height - minRoomSize);
            RectInt room1 = new RectInt(room.x, room.y, room.width, splitY - room.y);
            RectInt room2 = new RectInt(room.x, splitY, room.width, room.y + room.height - splitY);

            rooms.Add(room1);
            yield return new WaitForSeconds(stepDelay);
            yield return StartCoroutine(SplitRoom(room1, depth - 1));

            rooms.Add(room2);
            yield return new WaitForSeconds(stepDelay);
            yield return StartCoroutine(SplitRoom(room2, depth - 1));
        }
        else
        {
            int splitX = Random.Range(room.x + minRoomSize, room.x + room.width - minRoomSize);
            RectInt room1 = new RectInt(room.x, room.y, splitX - room.x, room.height);
            RectInt room2 = new RectInt(splitX, room.y, room.x + room.width - splitX, room.height);

            rooms.Add(room1);
            yield return new WaitForSeconds(stepDelay);
            yield return StartCoroutine(SplitRoom(room1, depth - 1));

            rooms.Add(room2);
            yield return new WaitForSeconds(stepDelay);
            yield return StartCoroutine(SplitRoom(room2, depth - 1));
        }
    }

    void Update()
    {
        foreach (var room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.red);
        }
    }
}
