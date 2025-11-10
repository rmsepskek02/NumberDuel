using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== Firebase 연결 테스트 시작 ===");
        CheckFirebaseDependencies();
    }

    private void CheckFirebaseDependencies()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("✅ Firebase 초기화 성공!");
                Debug.Log($"✅ Firebase App Name: {FirebaseApp.DefaultInstance.Name}");
                Debug.Log("✅ Firebase Auth와 Firestore를 사용할 준비가 되었습니다.");
            }
            else
            {
                Debug.LogError($"❌ Firebase 초기화 실패: {task.Result}");
                Debug.LogError("Firebase 설정을 다시 확인해주세요.");
            }
        });
    }
}
