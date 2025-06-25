using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class ImageGrid : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private ImageGridCell m_gridCellPrefab;
    [Tooltip("How many grid cells should the system prewarm to improve runtime performance")]
    [SerializeField] private int m_startCapacity;
    [SerializeField] private RectTransform m_gridRect;
    [SerializeField] private ImageCollectionAsset m_imageCollectionAsset;
    [SerializeField] private Vector2Int m_gridSize;
    [SerializeField] private float m_directionLockThreshold;
    [Tooltip("How many spaces outside of the frame should a picture still be saved? use -1 for infinity")]
    [SerializeField] private int m_outOfFrameMemory = -1;
    
    //I would usually use an existing custom pooling solution but didn't want to use any outside code
    private Queue<ImageGridCell> m_availableGridCells;
    private Dictionary<Vector2Int, ImageGridCell> m_gridCellDict;
    private ImageGridCell m_pressedImage;
    private bool m_imageDragged;
    private Vector2Int m_dragDirection;
    private Vector2 m_dragStartPosition;
    private Vector2Int m_dragStartCell;
    private Vector2 m_currentDragPosition;

    private void Start()
    {
        m_availableGridCells = new Queue<ImageGridCell>();
        m_gridCellDict = new Dictionary<Vector2Int, ImageGridCell>();
        m_imageCollectionAsset.Initialize(true);

        for (int i = 0; i < m_startCapacity; i++)
        {
            ImageGridCell cell = Instantiate(m_gridCellPrefab, m_gridCellPrefab.transform.position, Quaternion.identity,
                m_gridCellPrefab.transform.parent);
            cell.gameObject.SetActive(false);
            m_availableGridCells.Enqueue(cell);
        }
        
        for (int i = 0; i < m_gridSize.x; i++)
        {
            for (int j = 0; j < m_gridSize.y; j++)
            {
                ImageGridCell cell = CreateNewCell(new Vector2(i - (m_gridSize.x / 2), j - (m_gridSize.y / 2)));
                cell.gameObject.SetActive(true);
            }   
        }
        m_gridCellPrefab.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<Vector2Int,ImageGridCell> cell in m_gridCellDict)
        {
            m_imageCollectionAsset.ReturnSprite(cell.Value.CellImage.sprite);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (ConvertScreenPointToGridPosition(eventData, out Vector2 gridPosition))
        {
            Vector2Int gridSnapPosition = SnapToGridPosition(gridPosition);
            m_pressedImage = m_gridCellDict[gridSnapPosition];
            m_dragStartPosition = gridPosition;
            m_dragStartCell = gridSnapPosition;
            m_imageDragged = false;
        }
        else
        {
            Debug.LogWarning("Failed to convert screen point to local point in image.");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ConvertScreenPointToGridPosition(eventData, out Vector2 gridPosition))
        {
            Vector2 dragDelta = gridPosition - m_dragStartPosition;

            DecideDirectionVector(dragDelta);
            
            Vector2 movement = new Vector2(dragDelta.x * m_dragDirection.x, dragDelta.y * m_dragDirection.y);
            
            m_pressedImage.CellFloatingGridPosition = 
                m_dragStartCell + movement;

            if (movement.sqrMagnitude > 0f)
            {
                m_imageDragged = true;
                MoveTouchingCells(m_dragStartCell, m_dragDirection, movement);
            }
        }
        else
        {
            Debug.LogWarning("Failed to convert screen point to local point in image.");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        m_dragDirection = Vector2Int.zero;
        
        foreach (KeyValuePair<Vector2Int,ImageGridCell> cell in m_gridCellDict)
        {
            cell.Value.SnapToGrid();
        }

        //There's a better solution than this but I decided to focus efforts elsewhere
        //Get nodes
        var nodeList = m_gridCellDict.Values.ToList();
        //Clear indexer
        m_gridCellDict.Clear();
        
        //Repopulate
        foreach (ImageGridCell cell in nodeList)
        {
            //In case we don't use limitless out of frame memory this allows us to reuse images as well as grid cells
            if (m_outOfFrameMemory > -1 && !CoordinateInGrid(cell.CellGridPosition, m_outOfFrameMemory))
            {
                m_imageCollectionAsset.ReturnSprite(cell.CellImage.sprite);
                cell.gameObject.SetActive(false);
                m_availableGridCells.Enqueue(cell);
            }
            else
            {
                m_gridCellDict[cell.CellGridPosition] = cell;       
            }
        }
    }

    private ImageGridCell CreateNewCell(Vector2 gridPosition)
    {
        Sprite sprite = m_imageCollectionAsset.AvailableSprite;
        if (sprite == null)
        {
            return null;
        }

        ImageGridCell cell = m_availableGridCells.Count > 0 ? 
            m_availableGridCells.Dequeue() :
            Instantiate(m_gridCellPrefab, m_gridCellPrefab.transform.position, Quaternion.identity, m_gridCellPrefab.transform.parent);
        
        cell.Initialize();
        cell.CellFloatingGridPosition = gridPosition;
        m_gridCellDict.Add(cell.CellGridPosition, cell);
        cell.CellImage.sprite = sprite;
        return cell;
    }
    
    //This method could return a bool if we wanted to limit movement when more images aren't available
    //this wasn't mentioned in the requirements so I left it out but think it would be worthwhile 
    private void MoveTouchingCells(Vector2Int startCell, Vector2Int direction, Vector2 movement)
    {
        //Scan in direction of movement
        int i = 0;
        for (i = 1; m_gridCellDict.TryGetValue(startCell + direction * i, out var cell) && i < m_gridCellDict.Count; i++) {
            cell.CellFloatingGridPosition =
                startCell + direction * i + movement;
        }
        
        if (CoordinateInGrid(m_pressedImage.CellGridPosition + direction * i, 1) && 
            m_dragDirection.sqrMagnitude > 0.5f && !m_gridCellDict.ContainsKey(startCell + direction * i))
        {
            ImageGridCell cell = CreateNewCell(startCell + direction * i);
            if (cell != null)
            {
                cell.gameObject.SetActive(true);   
            }
        }

        //Scan against direction of movement
        int j = 0;
        for (j = -1; m_gridCellDict.TryGetValue(startCell + direction * j, out var cell) && j > -m_gridCellDict.Count; j--)
        {
            cell.CellFloatingGridPosition =
                startCell + direction * j + movement;
        }
        
        if (CoordinateInGrid(m_pressedImage.CellGridPosition + direction * j, 1) && 
            m_dragDirection.sqrMagnitude > 0.5f && !m_gridCellDict.ContainsKey(startCell + direction * j))
        {
            ImageGridCell cell = CreateNewCell(startCell + direction * j);
            if (cell != null)
            {
                cell.gameObject.SetActive(true);   
            }
        }
    }

    //used to prevent diagonal movement
    private void DecideDirectionVector(Vector2 dragDelta)
    {
        //No direction chosen yet
        if (m_dragDirection.sqrMagnitude < 0.5f)
        {
            if (dragDelta.x * dragDelta.x > m_directionLockThreshold)
            {
                m_dragDirection = Vector2Int.right;
            }
            else if(dragDelta.y * dragDelta.y > m_directionLockThreshold)
            {
                m_dragDirection = Vector2Int.up;
            }
        }
        //Direction chosen check for back to origin point
        else if(dragDelta.x * dragDelta.x < m_directionLockThreshold && dragDelta.y * dragDelta.y < m_directionLockThreshold)
        {
            m_dragDirection = Vector2Int.zero;
        }
    }

    private bool ConvertScreenPointToGridPosition(PointerEventData eventData, out Vector2 gridPosition)
    {
        gridPosition = Vector2.zero;
        //Cast press point to grid to find image pressed
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_gridRect, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            gridPosition = ImageToGridSpace(localPoint, true);
            return true;
        }

        return false;
    }
    
    private Vector2 ImageToGridSpace(Vector2 screenPosition, bool includePivot)
    {
        float width = m_gridRect.rect.width;
        float height = m_gridRect.rect.height;

        Vector2 imagePosition = new Vector2(
            (screenPosition.x - (includePivot ? m_gridRect.rect.xMin: 0f)) / width,
            (screenPosition.y - (includePivot ? m_gridRect.rect.yMin: 0f)) / height
        );
        
        Vector2 gridPosition = new Vector2(
            (imagePosition.x * m_gridSize.x) - (m_gridSize.x / 2),
            (imagePosition.y * m_gridSize.y) - (m_gridSize.y / 2));
        
        return gridPosition;
    }
    
    private Vector2Int SnapToGridPosition(Vector2 gridPosition)
    {
            Vector2Int snappedGridPosition = new Vector2Int(
                Mathf.FloorToInt(gridPosition.x),
                Mathf.FloorToInt(gridPosition.y));
            return snappedGridPosition;
    }
    
    private bool CoordinateInGrid(Vector2Int coordinate, int radius)
    {
        return 
            (coordinate.x <= (m_gridSize.x / 2) + radius) 
            && (coordinate.x >= -(m_gridSize.x / 2) - radius)
            && (coordinate.y <= (m_gridSize.y / 2) + radius) 
            && (coordinate.y >= -(m_gridSize.y / 2) - radius);
    }
}
