using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Used a scriptable object for this to make the collection easily replaceable
//this could also be used as a base class for other strategies of sprite management 
[CreateAssetMenu(fileName = "ImageCollectionAsset", menuName = "Setting/ImageCollectionAsset")]
public class ImageCollectionAsset : ScriptableObject
{
    [SerializeField] private List<Sprite> m_sprites;

    //Not in use but potentially useful to have
    public List<Sprite> AvailableSpriteList => m_availableSprites.ToList();

    public Sprite AvailableSprite
    {
        get
        {
            Initialize(false);
            if (m_availableSprites.Count > 0)
            {
                return m_availableSprites.Dequeue();
            }
            return null;
        }
    }

    //Using a queue enables plenty of different strategies for image management if needed
    //like dynamically adding them or returning them for reuse
    private Queue<Sprite> m_availableSprites = new();
    private bool m_initialized;

    public void ReturnSprite(Sprite sprite)
    {
        Initialize(false);
        m_availableSprites.Enqueue(sprite);
    }
    
    public void Initialize(bool force)
    {
        if (m_initialized && !force)
        {
            return;
        }

        m_initialized = true;
        m_availableSprites = new Queue<Sprite>();
        foreach (Sprite sprite in m_sprites)
        {
            m_availableSprites.Enqueue(sprite);
        }
    }
}
