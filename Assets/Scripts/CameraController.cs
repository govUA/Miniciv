using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public MapGenerator mapGenerator;
    public Tilemap tilemap;

    [Header("Movement Settings")] public float moveSpeed = 15f;

    [Header("Zoom Settings")] public float zoomSpeed = 20f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    private float mapWorldWidth;
    private float centerMinX;
    private float centerMaxX;
    private float minY;
    private float maxY;

    private bool boundsInitialized = false;

    public void InitializeBounds()
    {
        int width = mapGenerator.mapWidth;
        int height = mapGenerator.mapHeight;

        Vector3 cellZero = tilemap.CellToWorld(new Vector3Int(0, 0, 0));
        Vector3 cellMaxX = tilemap.CellToWorld(new Vector3Int(0, width, 0));
        Vector3 cellMaxY = tilemap.CellToWorld(new Vector3Int(height - 1, 0, 0));

        mapWorldWidth = cellMaxX.x - cellZero.x;
        float mapWorldHeight = cellMaxY.y - cellZero.y;

        centerMinX = cellZero.x;
        centerMaxX = cellMaxX.x;

        minY = cellZero.y;
        maxY = cellMaxY.y;

        // --- DYNAMIC ZOOM LIMITS ---
        float maxAllowedZoomX;

        if (mapGenerator.wrapWorld)
        {
            maxAllowedZoomX = mapWorldWidth / Camera.main.aspect;
        }
        else
        {
            maxAllowedZoomX = (mapWorldWidth / 2f) / Camera.main.aspect;
        }

        float maxAllowedZoomY = mapWorldHeight / 2f;

        float calculatedMaxZoom = Mathf.Min(maxAllowedZoomX, maxAllowedZoomY);
        maxZoom = Mathf.Min(maxZoom, calculatedMaxZoom);
        minZoom = Mathf.Min(minZoom, maxZoom);

        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);

        boundsInitialized = true;
    }

    void Update()
    {
        if (!boundsInitialized) return;

        HandleZoom();
        HandleMovement();
    }

    void HandleZoom()
    {
        float zoomDelta = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.zKey.isPressed) zoomDelta = -1f;
            if (Keyboard.current.xKey.isPressed) zoomDelta = 1f;
        }

        if (zoomDelta != 0)
        {
            Camera.main.orthographicSize += zoomDelta * zoomSpeed * Time.deltaTime;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);
        }
    }

    void HandleMovement()
    {
        float moveX = 0f;
        float moveY = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveY += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveY -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;
        }

        Vector3 move = new Vector3(moveX, moveY, 0) * moveSpeed * Time.deltaTime;
        Vector3 newPos = transform.position + move;

        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

        if (mapGenerator.wrapWorld)
        {
            if (newPos.x > centerMaxX) newPos.x -= mapWorldWidth;
            else if (newPos.x < centerMinX) newPos.x += mapWorldWidth;
        }
        else
        {
            newPos.x = Mathf.Clamp(newPos.x, centerMinX, centerMaxX);
        }

        transform.position = newPos;
    }
}