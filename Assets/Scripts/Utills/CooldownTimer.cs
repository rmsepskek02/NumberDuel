using System.Diagnostics;
using UnityEngine;

namespace Utills
{
    /// <summary>
    /// 재사용 가능한 쿨다운 타이머
    /// Stopwatch 기반으로 시스템 시간 조작에 강함
    /// </summary>
    public class CooldownTimer
    {
        private Stopwatch stopwatch;
        private float durationSeconds;

        /// <summary>
        /// 쿨다운 타이머 생성
        /// </summary>
        /// <param name="cooldownDuration">쿨다운 시간 (초)</param>
        public CooldownTimer(float cooldownDuration)
        {
            durationSeconds = cooldownDuration;
            stopwatch = new Stopwatch();
        }

        /// <summary>
        /// 쿨다운 시작
        /// </summary>
        public void Start()
        {
            stopwatch.Restart();
            UnityEngine.Debug.Log($"[CooldownTimer] 시작: {durationSeconds}초");
        }

        /// <summary>
        /// 쿨다운 활성 여부
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (!stopwatch.IsRunning)
                    return false;

                float elapsed = (float)stopwatch.Elapsed.TotalSeconds;
                return elapsed < durationSeconds;
            }
        }

        /// <summary>
        /// 남은 시간 (초)
        /// </summary>
        /// <returns>남은 시간 (0 이상)</returns>
        public int GetRemainingSeconds()
        {
            if (!IsActive)
                return 0;

            float elapsed = (float)stopwatch.Elapsed.TotalSeconds;
            float remaining = durationSeconds - elapsed;

            return Mathf.CeilToInt(remaining);
        }

        /// <summary>
        /// 경과 시간 (초)
        /// </summary>
        /// <returns>경과 시간</returns>
        public float GetElapsedSeconds()
        {
            return (float)stopwatch.Elapsed.TotalSeconds;
        }

        /// <summary>
        /// 쿨다운 중지
        /// </summary>
        public void Stop()
        {
            stopwatch.Stop();
            UnityEngine.Debug.Log("[CooldownTimer] 중지");
        }

        /// <summary>
        /// 쿨다운 리셋
        /// </summary>
        public void Reset()
        {
            stopwatch.Reset();
            UnityEngine.Debug.Log("[CooldownTimer] 리셋");
        }

        /// <summary>
        /// 쿨다운이 실행 중인지 확인 (활성 여부와 무관)
        /// </summary>
        public bool IsRunning => stopwatch.IsRunning;

        /// <summary>
        /// 쿨다운 시간 변경
        /// </summary>
        /// <param name="newDuration">새로운 쿨다운 시간 (초)</param>
        public void SetDuration(float newDuration)
        {
            durationSeconds = newDuration;
        }

        /// <summary>
        /// 현재 설정된 쿨다운 시간
        /// </summary>
        public float Duration => durationSeconds;
    }
}
