using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

//This could theoretically be a pure class which controls a grid cell prefab instance
//This would be better for performance but I felt it was a bit out of scope and didn't have time
public class ImageGridCell : MonoBehaviour
{
    [SerializeField] private Image m_image;
    //Using locators like this allows easy changes like padding etc and doesn't break under many different UI configurations
    [SerializeField] private Transform m_lowerLeftLocator;
    [SerializeField] private Transform m_upperRightLocator;
    //Used for easy edition of visual effects and other feedback without code changes as well as base system functionality
    [SerializeField] private UnityEvent<Vector2Int> m_onGridPositionChanged;
    [SerializeField] private float m_snapBackTime;
    
    public Image CellImage => m_image;
    //Used to store how much the cell is elongated in any direction relatively to its source cell
    public Vector2Int CellExtents { get; set; } = Vector2Int.zero;
    public Vector2Int CellGridPosition { get; set; }
    public UnityEvent<Vector2Int> OnGridPositionChanged => m_onGridPositionChanged;
    
    private bool m_initialized;
    private Vector2 m_cellDimensions;
    private Vector2 m_cellFloatingGridPosition;
    
    public Vector2 CellFloatingGridPosition
    {
        set
        {
            Vector2 delta = value - m_cellFloatingGridPosition;
            
            transform.position += new Vector3(delta.x * m_cellDimensions.y, delta.y * m_cellDimensions.y, 0f);

            m_cellFloatingGridPosition = value;
            
            Vector2Int calculatedPosition = new Vector2Int(Mathf.RoundToInt(m_cellFloatingGridPosition.x),
                Mathf.RoundToInt(m_cellFloatingGridPosition.y));
            
            if (calculatedPosition != CellGridPosition)
            {
                CellGridPosition = calculatedPosition;
                m_onGridPositionChanged?.Invoke(CellGridPosition);
            }
        }
        get => m_cellFloatingGridPosition;
    }
    
    public async void SnapToGrid()
    {
        float returnTime = m_snapBackTime;
        Vector2 startPosition = CellFloatingGridPosition;
        while (returnTime > 0f)
        {
            float delta = Time.deltaTime;
            returnTime -= delta;
            CellFloatingGridPosition = CellGridPosition + (startPosition - CellGridPosition) * (returnTime / m_snapBackTime);
            
            //This isn't a great solution and I would usually use either UniTask or my own MonoProcess custom solution for this kind of logic
            await Task.Delay((int)(1000 * delta));
        }
        CellFloatingGridPosition = CellGridPosition;
    }

    private void Awake()
    {
        Initialize();
    }
    
    public void Initialize()
    {
        if(m_initialized) { return; }

        m_initialized = true;
        transform.localPosition = Vector3.zero;
        Vector3 cornerDelta = m_upperRightLocator.position - m_lowerLeftLocator.position;
        SetCellDimensions(new Vector2(cornerDelta.x, cornerDelta.y));
    }

    private void SetCellDimensions(Vector2 cellDimensions)
    {
        m_cellDimensions = cellDimensions;
    }
}
