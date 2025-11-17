using ExitGames.Client.Photon;
using Objects;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 빠른 매칭 시스템을 관리하는 매니저
    /// JoinRandomRoom 방식으로 매칭 구현
    /// 첫 번째 플레이어가 매칭 전용 방 생성, 두 번째 플레이어가 입장
    /// </summary>
    public class MatchmakingManager : Singleton<MatchmakingManager>
    {
        #region Fields and Properties
        /// <summary>
        /// 현재 매칭 상태
        /// </summary>
        public MatchmakingState CurrentState { get; private set; } = MatchmakingState.Idle;

        /// <summary>
        /// 매칭 상태 변경 이벤트
        /// </summary>
        public event Action<MatchmakingState> OnMatchmakingStateChanged;

        /// <summary>
        /// 매칭 중인지 여부
        /// </summary>
        public bool IsSearching => CurrentState == MatchmakingState.Searching;

        /// <summary>
        /// 매칭 방 생성 중 플래그 (중복 요청 방지)
        /// </summary>
        private bool isCreatingMatchmakingRoom = false;

        /// <summary>
        /// 방 생성 중 취소 요청 플래그
        /// </summary>
        private bool pendingCancel = false;

        /// <summary>
        /// 매칭 완료 검증 코루틴
        /// </summary>
        private Coroutine matchVerificationCoroutine = null;

        /// <summary>
        /// 매칭 완료 검증 딜레이 (1초)
        /// </summary>
        private const float MATCH_VERIFICATION_DELAY = 1f;
        #endregion

        #region Public Methods
        /// <summary>
        /// 빠른 매칭 시작
        /// </summary>
        public void StartMatchmaking()
        {
            // 이미 매칭 중이면 무시
            if (CurrentState == MatchmakingState.Searching)
            {
                Debug.LogWarning("[MatchmakingManager] 이미 매칭 중입니다.");
                return;
            }

            // Photon 연결 확인
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogError("[MatchmakingManager] Photon 서버에 연결되지 않았습니다.");
                SystemMessageManager.Instance?.ShowMessage("NetworkError");
                return;
            }

            // 로비에 있는지 확인
            if (!PhotonNetwork.InLobby)
            {
                Debug.LogError("[MatchmakingManager] 로비에 입장하지 않았습니다.");
                SystemMessageManager.Instance?.ShowMessage("NetworkError");
                return;
            }

            Debug.Log("[MatchmakingManager] 빠른 매칭 시작");
            ChangeState(MatchmakingState.Searching);

            // 매칭 전용 방 검색
            Hashtable expectedProps = new Hashtable
            {
                { "isMatchmaking", true }  // 매칭 전용 방만 검색
            };

            PhotonNetwork.JoinRandomRoom(expectedProps, 2);
        }

        /// <summary>
        /// 매칭 취소
        /// 로딩 스크린 없이 버튼과 시스템 메시지만 변경
        /// </summary>
        public void CancelMatchmaking()
        {
            if (CurrentState != MatchmakingState.Searching)
            {
                Debug.LogWarning("[MatchmakingManager] 매칭 중이 아닙니다.");
                return;
            }

            // 방 생성 중이면 취소 요청만 예약
            if (isCreatingMatchmakingRoom)
            {
                Debug.Log("[MatchmakingManager] 방 생성 중이므로 취소 예약됨");
                pendingCancel = true;
                return;
            }

            Debug.Log("[MatchmakingManager] 매칭 취소");

            // 방에서 나가기
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            ChangeState(MatchmakingState.Idle);
            SystemMessageManager.Instance?.ShowMessage("MatchmakingCancelled");
        }
        #endregion

        #region Photon Callbacks
        /// <summary>
        /// 랜덤 매칭 실패 시 호출 (매칭 방이 없음)
        /// 자동으로 매칭 전용 방 생성
        /// </summary>
        public void OnJoinRandomMatchmakingFailed(short returnCode, string message)
        {
            if (CurrentState != MatchmakingState.Searching)
            {
                return;
            }

            // 중복 방 생성 방지
            if (isCreatingMatchmakingRoom)
            {
                Debug.LogWarning("[MatchmakingManager] 이미 매칭 방 생성 중입니다.");
                return;
            }

            Debug.Log($"[MatchmakingManager] 매칭 방을 찾지 못했습니다. 새로운 매칭 방 생성. (Code: {returnCode})");
            isCreatingMatchmakingRoom = true;

            // 매칭 전용 방 생성
            CreateMatchmakingRoom();
        }

        /// <summary>
        /// 매칭 방 생성 성공 시 호출
        /// </summary>
        public void OnCreatedMatchmakingRoom()
        {
            if (CurrentState != MatchmakingState.Searching)
            {
                return;
            }

            isCreatingMatchmakingRoom = false;

            // 방 생성 중 취소 요청이 있었으면 즉시 방 나가기
            if (pendingCancel)
            {
                Debug.Log("[MatchmakingManager] 방 생성 완료 후 예약된 취소 실행");
                pendingCancel = false;

                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.LeaveRoom();
                }

                ChangeState(MatchmakingState.Idle);
                SystemMessageManager.Instance?.ShowMessage("MatchmakingCancelled");
                return;
            }

            Debug.Log("[MatchmakingManager] 매칭 방 생성 완료. 상대방 대기 중...");

            // "매칭 중..." 메시지 표시
            SystemMessageManager.Instance?.ShowMessage("Matching");
        }

        /// <summary>
        /// 매칭 방 생성 실패 시 호출
        /// </summary>
        public void OnCreateMatchmakingRoomFailed(short returnCode, string message)
        {
            isCreatingMatchmakingRoom = false;
            pendingCancel = false; // 취소 예약도 초기화

            Debug.LogError($"[MatchmakingManager] 매칭 방 생성 실패: {message} (Code: {returnCode})");
            ChangeState(MatchmakingState.Idle);
            SystemMessageManager.Instance?.ShowMessage("NetworkError");
        }

        /// <summary>
        /// 방에 입장했을 때 호출
        /// </summary>
        public void OnJoinedMatchmakingRoom()
        {
            if (CurrentState != MatchmakingState.Searching)
            {
                return;
            }

            // 매칭 방에 입장 성공 (두 번째 플레이어)
            Debug.Log("[MatchmakingManager] 매칭 방에 입장했습니다.");

            // 이미 2명이면 즉시 매칭 완료 처리
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
            {
                OnMatchingComplete();
            }
        }

        /// <summary>
        /// 플레이어가 방에 입장했을 때 호출 (방장 입장에서 호출)
        /// </summary>
        public void OnPlayerEnteredMatchmakingRoom(Player newPlayer)
        {
            if (CurrentState != MatchmakingState.Searching)
            {
                return;
            }

            Debug.Log($"[MatchmakingManager] 플레이어 입장: {newPlayer.NickName}");

            // 2명이 되면 매칭 완료
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
            {
                OnMatchingComplete();
            }
        }

        /// <summary>
        /// 방을 떠났을 때 호출 (매칭 취소)
        /// </summary>
        public void OnLeftMatchmakingRoom()
        {
            isCreatingMatchmakingRoom = false;
            pendingCancel = false; // 취소 예약 초기화

            // 매칭 검증 코루틴 중지
            if (matchVerificationCoroutine != null)
            {
                StopCoroutine(matchVerificationCoroutine);
                matchVerificationCoroutine = null;
            }

            Debug.Log("[MatchmakingManager] 매칭 방에서 퇴장했습니다.");
            // 상태 변경은 CancelMatchmaking에서 이미 처리됨
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 매칭 전용 방 생성
        /// IsVisible = true로 설정하여 JoinRandomRoom이 찾을 수 있도록 함
        /// 방 목록 UI에서는 isMatchmaking 속성으로 필터링
        /// 카드 색상도 미리 설정하여 게임 시작 시 동기화 보장
        /// </summary>
        private void CreateMatchmakingRoom()
        {
            // 고유한 방 이름 생성
            string roomName = $"QM_{PhotonNetwork.LocalPlayer.UserId}_{UnityEngine.Random.Range(1000, 9999)}";

            // 카드 색상 미리 선택
            string masterColor = "";
            string guestColor = "";

            if (ResourcesManager.Instance != null)
            {
                var (color1, color2) = ResourcesManager.Instance.SelectRandomColors();
                masterColor = color1;
                guestColor = color2;
                Debug.Log($"[MatchmakingManager] 매칭 방 색상 선택: Master={masterColor}, Guest={guestColor}");
            }

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 2,
                IsVisible = true,   // JoinRandomRoom이 찾을 수 있도록 true (중요!)
                IsOpen = true,
                EmptyRoomTtl = 0,   // 빈 방 즉시 삭제 안 함
                PlayerTtl = 0,      // 연결 끊긴 플레이어 타임아웃 없음

                CustomRoomProperties = new Hashtable
                {
                    { "isMatchmaking", true },          // 매칭 전용 방 표시
                    { "roomPassword", "" },             // 비밀번호 없음
                    { "roomName", "Quick Match" },      // 표시용 이름
                    { "currentPlayers", 1 },
                    { "masterPlayerColor", masterColor }, // 방장 색상
                    { "guestPlayerColor", guestColor }    // 게스트 색상
                },

                CustomRoomPropertiesForLobby = new[] { "isMatchmaking", "roomPassword", "currentPlayers" }
            };

            Debug.Log($"[MatchmakingManager] 매칭 방 생성 시도: {roomName}");
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        /// <summary>
        /// 매칭 완료 처리 (1초 딜레이 후 검증)
        /// </summary>
        private void OnMatchingComplete()
        {
            Debug.Log("[MatchmakingManager] 매칭 감지. 검증 시작...");

            // 기존 검증 코루틴이 있으면 중지
            if (matchVerificationCoroutine != null)
            {
                StopCoroutine(matchVerificationCoroutine);
            }

            // 검증 코루틴 시작
            matchVerificationCoroutine = StartCoroutine(VerifyAndCompleteMatch());
        }

        /// <summary>
        /// 매칭 완료 검증 후 게임 시작
        /// 1초 딜레이 후 플레이어 수와 서로 다른 UserId 검증
        /// </summary>
        private System.Collections.IEnumerator VerifyAndCompleteMatch()
        {
            Debug.Log($"[MatchmakingManager] {MATCH_VERIFICATION_DELAY}초 후 매칭 검증 예정");

            // 1초 대기
            yield return new WaitForSeconds(MATCH_VERIFICATION_DELAY);

            // 방에 여전히 있는지 확인
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[MatchmakingManager] 검증 중 방에서 나갔습니다. 매칭 취소");
                matchVerificationCoroutine = null;
                yield break;
            }

            // 플레이어 수 확인
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            if (playerCount < 2)
            {
                Debug.LogWarning($"[MatchmakingManager] 검증 실패: 플레이어 수 부족 ({playerCount}/2)");
                matchVerificationCoroutine = null;
                yield break;
            }

            // 서로 다른 플레이어인지 검증
            var players = PhotonNetwork.CurrentRoom.Players.Values;
            var playerArray = new Photon.Realtime.Player[players.Count];
            players.CopyTo(playerArray, 0);

            if (playerArray.Length >= 2)
            {
                string userId1 = playerArray[0].UserId;
                string userId2 = playerArray[1].UserId;

                if (userId1 == userId2)
                {
                    Debug.LogError($"[MatchmakingManager] 검증 실패: 동일한 UserId 감지 ({userId1}). 혼자 매칭 차단!");

                    // 방 나가기 및 매칭 취소
                    if (PhotonNetwork.InRoom)
                    {
                        PhotonNetwork.LeaveRoom();
                    }

                    ChangeState(MatchmakingState.Idle);
                    SystemMessageManager.Instance?.ShowMessage("MatchmakingCancelled");
                    matchVerificationCoroutine = null;
                    yield break;
                }

                Debug.Log($"[MatchmakingManager] 검증 성공: 서로 다른 플레이어 확인 ({userId1} vs {userId2})");
            }

            // 검증 통과: 매칭 완료 처리
            Debug.Log("[MatchmakingManager] 매칭 완료! 게임 시작");
            ChangeState(MatchmakingState.Matched);

            // "상대를 찾았습니다!" 메시지 표시
            SystemMessageManager.Instance?.ShowMessage("MatchingComplete");

            // 게임 씬으로 전환 (LoadingScreen 사용)
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.FadeInThenAction(() =>
                {
                    PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.GameScene));
                });
            }
            else
            {
                PhotonNetwork.LoadLevel(SceneNameExtensions.GetSceneName(SceneName.GameScene));
            }

            matchVerificationCoroutine = null;
        }

        /// <summary>
        /// 매칭 상태 변경
        /// </summary>
        private void ChangeState(MatchmakingState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            Debug.Log($"[MatchmakingManager] 상태 변경: {CurrentState} -> {newState}");
            CurrentState = newState;
            OnMatchmakingStateChanged?.Invoke(newState);
        }
        #endregion
    }
}
