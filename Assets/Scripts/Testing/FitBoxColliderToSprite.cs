using UnityEngine;

/// <summary>
/// SpriteRenderer의 Sprite 크기에 맞춰 BoxCollider의 크기 및 중심을 자동으로 설정해주는 스크립트
/// - 실행 시 Sprite의 bounds 정보를 사용하여 BoxCollider의 size/center 동기화
/// - [ExecuteAlways]로 인해 에디터에서도 실시간 반영됨
/// </summary>
[ExecuteAlways]
public class FitBoxColliderToSprite : MonoBehaviour
{
    void Start()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        var collider = GetComponent<BoxCollider>();

        if (spriteRenderer.sprite != null)
        {
            // 스프라이트의 바운드에 맞게 콜라이더 크기 및 위치 설정
            collider.size = spriteRenderer.sprite.bounds.size;
            collider.center = spriteRenderer.sprite.bounds.center;
        }
    }
}
