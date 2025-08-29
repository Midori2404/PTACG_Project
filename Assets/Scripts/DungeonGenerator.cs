using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonGenerator : MonoBehaviourPunCallbacks
{
    public class Cell
    {
        public bool visited = false;
        public bool[] status = new bool[4];
    }

    [System.Serializable]
    public class Rule
    {
        public GameObject room;
        public Vector2Int minPosition;
        public Vector2Int maxPosition;

        public bool obligatory;

        public int ProbabilityOfSpawning(int x, int y)
        {
            // 0 - cannot spawn 1 - can spawn 2 - HAS to spawn

            if (x >= minPosition.x && x <= maxPosition.x && y >= minPosition.y && y <= maxPosition.y)
            {
                return obligatory ? 2 : 1;
            }

            return 0;
        }

    }

    public Vector2Int size;
    public int startPos = 0;
    public Rule[] rooms;
    public Vector2 offset;

    List<Cell> board;

    public static DungeonGenerator Instance { get; private set; }
    RoomBehaviour[,] roomGrid; // A 2D array to hold RoomBehaviour references
    public PhotonView photonView;

    [Header("UI Management")]
    public Transform mapContainer; // Assign the UI Panel with GridLayoutGroup
    public GameObject mapTilePrefab; // Assign the MapTile prefab
    public List<GameObject> generatedMapTiles;

    List<Color> tileColors = new List<Color>();

    private void Awake()
    {
        Instance = this;
        photonView = GetComponent<PhotonView>();
    }

    // Start is called before the first frame update
    void Start()
    {
        roomGrid = new RoomBehaviour[size.x, size.y]; // Ensure roomGrid exists on all clients

        if (PhotonNetwork.IsMasterClient)
        {
            MazeGenerator();
        }
        

        AdjustMapContainerSize();
    }

    [PunRPC]
    void GenerateDungeon()
    {
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                Cell currentCell = board[(i + j * size.x)];
                if (currentCell.visited)
                {
                    int randomRoom = -1;
                    List<int> availableRooms = new List<int>();

                    for (int k = 0; k < rooms.Length; k++)
                    {
                        int p = rooms[k].ProbabilityOfSpawning(i, j);

                        if (p == 2)
                        {
                            randomRoom = k;
                            break;
                        }
                        else if (p == 1)
                        {
                            availableRooms.Add(k);
                        }
                    }

                    if (randomRoom == -1)
                    {
                        if (availableRooms.Count > 0)
                        {
                            randomRoom = availableRooms[Random.Range(0, availableRooms.Count)];
                        }
                        else
                        {
                            randomRoom = 0;
                        }
                    }

                    string prefabPath = "Rooms/" + rooms[randomRoom].room.name;
                    // var newRoom = Instantiate(rooms[randomRoom].room, new Vector3(i * offset.x, 0, -j * offset.y), Quaternion.identity, transform).GetComponent<RoomBehaviour>();
                    var newRoom = PhotonNetwork.Instantiate(prefabPath, new Vector3(i * offset.x, 0, -j * offset.y), Quaternion.identity).GetComponent<RoomBehaviour>();
                    newRoom.transform.parent = transform;
                    photonView.RPC("SetupRoomRPC", RpcTarget.All, newRoom.GetComponent<PhotonView>().ViewID, currentCell.status, i, j);
                    SetupMinimap(i, j, true, randomRoom);
                }
                else
                {
                    SetupMinimap(i, j, false, -1);
                }
            }
        }
    }

    [PunRPC]
    void SetupRoomRPC(int roomViewID, bool[] status, int i, int j)
    {
        RoomBehaviour newRoom = PhotonView.Find(roomViewID).GetComponent<RoomBehaviour>();
        newRoom.UpdateRoom(status);

        roomGrid[i, j] = newRoom; // Store the room reference

        newRoom.name += $" {i}-{j}"; // Proper Naming for GameObject

        photonView.RPC("LinkDoorsRPC", RpcTarget.All, i, j, newRoom.GetComponent<PhotonView>().ViewID); // Assign neighbors for each room
    }

    [PunRPC]
    void LinkDoorsRPC(int x, int y, int roomViewID)
    {
        RoomBehaviour currentRoom = PhotonView.Find(roomViewID).GetComponent<RoomBehaviour>();

        if (x > 0 && roomGrid[x - 1, y] != null) // Left neighbor
        {
            photonView.RPC("ConnectRoomsRPC", RpcTarget.All, currentRoom.GetComponent<PhotonView>().ViewID, roomGrid[x - 1, y].GetComponent<PhotonView>().ViewID, 3, 2);
        }

        if (x < size.x - 1 && roomGrid[x + 1, y] != null) // Right neighbor
        {
            photonView.RPC("ConnectRoomsRPC", RpcTarget.All, currentRoom.GetComponent<PhotonView>().ViewID, roomGrid[x + 1, y].GetComponent<PhotonView>().ViewID, 2, 3);
        }

        if (y > 0 && roomGrid[x, y - 1] != null) // Up neighbor
        {
            photonView.RPC("ConnectRoomsRPC", RpcTarget.All, currentRoom.GetComponent<PhotonView>().ViewID, roomGrid[x, y - 1].GetComponent<PhotonView>().ViewID, 0, 1);
        }

        if (y < size.y - 1 && roomGrid[x, y + 1] != null) // Down neighbor
        {
            photonView.RPC("ConnectRoomsRPC", RpcTarget.All, currentRoom.GetComponent<PhotonView>().ViewID, roomGrid[x, y + 1].GetComponent<PhotonView>().ViewID, 1, 0);
        }
    }

    [PunRPC]
    void ConnectRoomsRPC(int room1ViewID, int room2ViewID, int door1Index, int door2Index)
    {
        RoomBehaviour room1 = PhotonView.Find(room1ViewID).GetComponent<RoomBehaviour>();
        RoomBehaviour room2 = PhotonView.Find(room2ViewID).GetComponent<RoomBehaviour>();

        var door1 = room1.doors[door1Index].GetComponentInChildren<DoorBehaviour>();
        var door2 = room2.doors[door2Index].GetComponentInChildren<DoorBehaviour>();

        door1.currentRoom = room1;
        door1.connectedRoom = room2;

        door2.currentRoom = room2;
        door2.connectedRoom = room1;
    }

    void MazeGenerator()
    {
        board = new List<Cell>();

        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                board.Add(new Cell());
            }
        }

        int currentCell = startPos;

        Stack<int> path = new Stack<int>();

        int k = 0;

        while (k < 1000)
        {
            k++;

            board[currentCell].visited = true;

            if (currentCell == board.Count - 1)
            {
                break;
            }

            //Check the cell's neighbors
            List<int> neighbors = CheckNeighbors(currentCell);

            if (neighbors.Count == 0)
            {
                if (path.Count == 0)
                {
                    break;
                }
                else
                {
                    currentCell = path.Pop();
                }
            }
            else
            {
                path.Push(currentCell);

                int newCell = neighbors[Random.Range(0, neighbors.Count)];

                if (newCell > currentCell)
                {
                    //down or right
                    if (newCell - 1 == currentCell)
                    {
                        board[currentCell].status[2] = true;
                        currentCell = newCell;
                        board[currentCell].status[3] = true;
                    }
                    else
                    {
                        board[currentCell].status[1] = true;
                        currentCell = newCell;
                        board[currentCell].status[0] = true;
                    }
                }
                else
                {
                    //up or left
                    if (newCell + 1 == currentCell)
                    {
                        board[currentCell].status[3] = true;
                        currentCell = newCell;
                        board[currentCell].status[2] = true;
                    }
                    else
                    {
                        board[currentCell].status[0] = true;
                        currentCell = newCell;
                        board[currentCell].status[1] = true;
                    }
                }

            }

        }
        GenerateDungeon();
    }

    List<int> CheckNeighbors(int cell)
    {
        List<int> neighbors = new List<int>();

        //check up neighbor
        if (cell - size.x >= 0 && !board[(cell - size.x)].visited)
        {
            neighbors.Add((cell - size.x));
        }

        //check down neighbor
        if (cell + size.x < board.Count && !board[(cell + size.x)].visited)
        {
            neighbors.Add((cell + size.x));
        }

        //check right neighbor
        if ((cell + 1) % size.x != 0 && !board[(cell + 1)].visited)
        {
            neighbors.Add((cell + 1));
        }

        //check left neighbor
        if (cell % size.x != 0 && !board[(cell - 1)].visited)
        {
            neighbors.Add((cell - 1));
        }

        return neighbors;
    }

    #region Minimap
    // This is for minimap sync
    void SetupMinimap(int x, int y, bool visited, int roomIndex)
    {
        photonView.RPC("CreateOrUpdateMinimapTileRPC", RpcTarget.All, x, y, visited, roomIndex);
    }

    [PunRPC]
    void CreateOrUpdateMinimapTileRPC(int x, int y, bool visited, int roomIndex)
    {
        GameObject mapTile = Instantiate(mapTilePrefab, mapContainer);
        mapTile.GetComponent<Image>().color = visited ? Color.white : Color.black;

        if (x == 0 && y == 0) mapTile.GetComponent<Image>().color = Color.green; // Start room
        if (x == size.x - 1 && y == size.y - 1) mapTile.GetComponent<Image>().color = Color.red; // End room

        generatedMapTiles.Add(mapTile);

        // Store tile data for syncing
        tileColors.Add(mapTile.GetComponent<Image>().color);

        // Ensure the room exists before assigning data
        if (visited && roomIndex >= 0)
        {
            RoomBehaviour room = roomGrid[x, y];
            room.SetRoom(mapTile); // This method will likely handle the reference to the UI element
        }
    }

    // This is for minimap 
    void AdjustMapContainerSize()
    {
        Vector2 cellSize = mapContainer.GetComponent<GridLayoutGroup>().cellSize;
        Vector2 spacing = mapContainer.GetComponent<GridLayoutGroup>().spacing;
        RectTransform mapRect = mapContainer.GetComponent<RectTransform>();

        // Calculate the required size of the map container
        float width = size.x * cellSize.x + cellSize.x * 1.5f + offset.x / 2 + spacing.x / 2; // Reconsider about spacing
        float height = size.y * cellSize.y + cellSize.y * 1.5f + spacing.y / 2;

        // Apply the calculated size to the RectTransform
        mapRect.sizeDelta = new Vector2(width, height);

        // Set the anchor to the top-left corner for proper positioning
        mapRect.anchorMin = new Vector2(1, 1);
        mapRect.anchorMax = new Vector2(1, 1);
        mapRect.pivot = new Vector2(1, 1);

        // Optionally, reset the position
        //mapRect.anchoredPosition = new Vector2(-width, -height);
        mapRect.anchoredPosition = Vector2.zero;
    }

    [PunRPC]
    public void UpdateMinimapRPC(float x, float z, float r, float g, float b)
    {
        // Find the corresponding minimap tile based on the room position
        foreach (GameObject tile in generatedMapTiles)
        {
            Vector3 tilePosition = tile.transform.localPosition;
            if (Mathf.Approximately(tilePosition.x, x) && Mathf.Approximately(tilePosition.y, z))
            {
                // Update the tile color
                tile.GetComponent<Image>().color = new Color(r, g, b);
                break;
            }
        }
    }
    #endregion

}