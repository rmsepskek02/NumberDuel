using UnityEngine;

//[RequireComponent(typeof(BoxCollider))]
//[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class FitBoxColliderToSprite : MonoBehaviour
{
    void Start()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        var collider = GetComponent<BoxCollider>();

        if (spriteRenderer.sprite != null)
        {
            collider.size = spriteRenderer.sprite.bounds.size;
            collider.center = spriteRenderer.sprite.bounds.center;
        }
    }
}
