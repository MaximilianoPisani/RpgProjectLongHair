using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class QuestController : NetworkBehaviour
{
    private QuestDataSO _currentQuest;
    private List<QuestController> _partyMembers = new(); //  nueva lista

    private readonly HashSet<string> _completedQuests = new();

    public QuestDataSO CurrentQuest => _currentQuest;

    private const string MISSION_PATH = "Quest/";

    private static QuestController _activeMissionOwner;
    private static string _activeMissionId;

    #region Networking

    #region Server

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_StartMission(string missionId, RpcInfo info)
    {
        if (!Object.HasStateAuthority) return;
        if (string.IsNullOrEmpty(missionId)) return;
        if (_completedQuests.Contains(missionId)) return;

        var missionData = Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}");
        if (missionData == null) return;

        _activeMissionOwner = this;
        _activeMissionId = missionId;
        _partyMembers.Clear();

        StartNewQuest(missionData);
        RPC_NotifyMissionStarted(missionId);

        var runnerManager = FindFirstObjectByType<RunnerManager>();
        if (runnerManager == null) return;

        foreach (var playerObj in runnerManager.SpawnedPlayers.Values)
        {
            var questController = playerObj.GetComponent<QuestController>();
            if (questController == null) continue;
            if (questController == this) continue;
            if (questController.HasCompletedQuest(missionId)) continue;

            questController.RPC_InviteToQuest(missionId);
        }
    }
    #endregion

    #region Client

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestTeleport(Vector3 destination)
    {
        if (!Object.HasStateAuthority) return;

        var cc = Object.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Object.transform.position = destination;

        if (cc != null) cc.enabled = true;

        Debug.Log($"[QuestController] Teleport ejecutado a {destination}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyMissionComplete()
    {
        Debug.Log("[QuestController] RPC_NotifyMissionComplete recibido");
        if (_currentQuest == null) return;
        MissionEvents.OnMissionComplete?.Invoke(_currentQuest);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyMissionFailed()
    {
        Debug.Log("[QuestController] RPC_NotifyMissionFailed recibido");
        var quest = _currentQuest;
        if (quest == null) return;
        MissionEvents.OnMissionFailed?.Invoke(quest);

        // Limpiar estado del cliente
        TrackEvents.OnTrackEvent -= TrackStep;
        if (_currentQuest != null)
        {
            Destroy(_currentQuest);
            _currentQuest = null;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyMissionStarted(string missionId)
    {
        Debug.Log($"[QuestController] RPC_NotifyMissionStarted recibido");
        if (_currentQuest != null) return;

        var missionData = Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}");
        if (missionData == null) return;

        StartNewQuest(missionData);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_InviteToQuest(string missionId)
    {
        if (_completedQuests.Contains(missionId)) return;

        var missionData = Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}");
        if (missionData == null) return;

        MissionEvents.OnQuestInviteReceived?.Invoke(missionData);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Unreliable)]
    private void RPC_ClientHandleError(string error, RpcInfo info = default)
    {
        //TODO: implementar manejo de errores
    }

    public void HandlePlayerDeath()
    {
        if (!Object.HasStateAuthority) return;

        if (_activeMissionOwner == this)
            FailureQuest();
        else
            LeaveQuestAsMember();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_LeaveQuest()
    {
        if (!Object.HasStateAuthority) return;
        HandlePlayerDeath();
    }

    private void LeaveQuestAsMember()
    {
        if (_activeMissionOwner != null)
            _activeMissionOwner._partyMembers.Remove(this);

        TrackEvents.OnTrackEvent -= TrackStep;

        if (_currentQuest != null)
        {
            Destroy(_currentQuest);
            _currentQuest = null;
        }

        RPC_NotifyMissionFailed();
    }
    #endregion

    #endregion

    public void StartNewQuest(QuestDataSO questData)
    {
        Debug.Log($"[QuestTester] Misión iniciada: {questData.questName}.");

        TrackEvents.OnTrackEvent -= TrackStep;
        TrackEvents.OnTrackEvent += TrackStep;

        if (_currentQuest != null)
            Destroy(_currentQuest);

        _currentQuest = Instantiate(questData);

        MissionEvents.OnMissionStart?.Invoke(_currentQuest);
    }

    public void TrackStep(string stepId, int progress)
    {
        if (_currentQuest == null)
        {
            Debug.Log("[QuestController] TrackStep: _currentQuest es null");
            return;
        }

        Debug.Log($"[QuestController] TrackStep recibido: id={stepId}, progress={progress}");

        if (!_currentQuest.UpdateProgress(stepId, progress, out var isSuccess))
        {
            Debug.Log($"[QuestController] Progreso actualizado, mision en curso");
            MissionEvents.OnUpdateProgress?.Invoke(_currentQuest);
            return;
        }

        Debug.Log($"[QuestController] Mision terminada, isSuccess={isSuccess}");

        if (isSuccess)
            CompleteQuest();
        else
            FailureQuest();
    }

    public void FailureQuest()
    {
        if (_currentQuest == null)
            return;

        RPC_NotifyMissionFailed();

        foreach (var member in _partyMembers)
        {
            if (member == null) continue;

            member.RPC_NotifyMissionFailed();
            TrackEvents.OnTrackEvent -= member.TrackStep;

            if (member._currentQuest != null)
            {
                Destroy(member._currentQuest);
                member._currentQuest = null;
            }
        }

        _partyMembers.Clear();
        TrackEvents.OnTrackEvent -= TrackStep;
        Destroy(_currentQuest);
        _currentQuest = null;
        _activeMissionOwner = null;
        _activeMissionId = null;
    }

    private void CompleteQuest()
    {
        Debug.Log($"[QUEST COMPLETE] Owner={name} Members={_partyMembers.Count}");

        if (_currentQuest == null)
            return;

        string questId = _currentQuest.questId;
        int xp = _currentQuest.xp; 

        MarkQuestCompleted(questId);

        var playerExp = GetComponent<PlayerExp>();
        if (playerExp != null)
            playerExp.AddExperience(xp);

        RPC_NotifyMissionComplete();

        foreach (var member in _partyMembers)
        {
            if (member == null) continue;

            member.MarkQuestCompleted(questId);

            var memberExp = member.GetComponent<PlayerExp>();
            if (memberExp != null)
                memberExp.AddExperience(xp);

            member.RPC_NotifyMissionComplete();
            TrackEvents.OnTrackEvent -= member.TrackStep;

            if (member._currentQuest != null)
            {
                Destroy(member._currentQuest);
                member._currentQuest = null;
            }
        }

        _partyMembers.Clear();

        TrackEvents.OnTrackEvent -= TrackStep;
        Destroy(_currentQuest);
        _currentQuest = null;

        _activeMissionOwner = null;
        _activeMissionId = null;
    }

    private void OnDestroy()
    {
        if (_currentQuest != null)
            Destroy(_currentQuest);

        TrackEvents.OnTrackEvent -= TrackStep;
    }

    public bool HasCompletedQuest(string questId)
    {
        return _completedQuests.Contains(questId);
    }

    public void MarkQuestCompleted(string questId)
    {
        if (string.IsNullOrEmpty(questId))
            return;

        _completedQuests.Add(questId);

        Debug.Log($"Quest completada guardada: {questId}");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RegisterAcceptedPlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;

        var runnerManager = FindFirstObjectByType<RunnerManager>();

        if (runnerManager == null)
            return;

        if (!runnerManager.SpawnedPlayers.TryGetValue(
                player,
                out var playerObject))
            return;

        var questController =
            playerObject.GetComponent<QuestController>();

        if (questController == null)
            return;

        if (_partyMembers.Contains(questController))
            return;

        _partyMembers.Add(questController);

        Debug.Log(
            $"Jugador aceptó misión: {questController.name}");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestJoinMission(string missionId)
    {
        if (!Object.HasStateAuthority) return;

        if (_activeMissionOwner == null || _activeMissionId != missionId)
        {
            Debug.LogWarning($"[QuestController] RPC_RequestJoinMission: no hay owner activo para {missionId}");
            return;
        }

        var runnerManager = FindFirstObjectByType<RunnerManager>();
        if (runnerManager == null) return;

        if (!runnerManager.SpawnedPlayers.TryGetValue(Object.InputAuthority, out var playerObject))
            return;

        var questController = playerObject.GetComponent<QuestController>();
        if (questController == null) return;

        if (_activeMissionOwner._partyMembers.Contains(questController)) return;

        _activeMissionOwner._partyMembers.Add(questController);

        questController.RPC_NotifyMissionStarted(missionId);

        Debug.Log($"[QuestController] {questController.name} unido a misión {missionId}");
    }

    public static QuestController GetMissionOwner()
    {
        return _activeMissionOwner;
    }
}