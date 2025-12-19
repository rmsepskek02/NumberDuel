using System.Threading.Tasks;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// UserProfile 관련 확장 메서드
    /// </summary>
    public static class ProfileExtensions
    {
        /// <summary>
        /// 프로필을 로드하고 null 체크 후 로그아웃 처리
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <param name="onProfileNull">프로필이 null일 때 실행할 콜백 (선택)</param>
        /// <returns>UserProfile 또는 null</returns>
        public static async Task<Objects.Data.UserProfile> LoadProfileWithNullCheck(
            string uid,
            System.Action onProfileNull = null)
        {
            var profile = await Manager.DatabaseManager.Instance.GetUserProfile(uid);

            if (profile == null)
            {
                Debug.LogError($"[ProfileExtensions] 프로필을 찾을 수 없습니다: {uid}");
                Manager.SystemMessageManager.Instance?.ShowMessage("ProfileNotFound");
                Manager.AuthManager.Instance.SignOutWithoutSessionClear();

                onProfileNull?.Invoke();
            }

            return profile;
        }
    }
}
