using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class QuestController : NetworkBehaviour
{
    private QuestDataSO _currentQuest;
    private List<QuestController> _partyMembers = new(); //  nueva lista

    public QuestDataSO CurrentQuest => _currentQuest;

    private const string MISSION_PATH = "Quest/";

    #region Networking

    #region Server
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_StartMission(string missionId, RpcInfo info)
    {
        if (!Object.HasStateAuthority) return;

        if (string.IsNullOrEmpty(missionId))
        {
            RPC_ClientHandleError("Se recibió un id nulo al intentar iniciar una mission");
            return;
        }

        var missionData = Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}");
        if (missionData == null)
        {
            RPC_ClientHandleError($"No se encontró la misión {missionId}");
            return;
        }

        StartNewQuest(missionData);
        RPC_NotifyMissionStarted(missionId);

        if (missionData.allowTeleportParty)
        {
            Debug.Log($"[QuestController] allowTeleportParty=true - players={FindFirstObjectByType<RunnerManager>()?.SpawnedPlayers.Count}");

            var runnerManager = FindFirstObjectByType<RunnerManager>();
            foreach (var playerObj in runnerManager.SpawnedPlayers.Values)
            {
                var questController = playerObj.GetComponent<QuestController>();
                if (questController == null) continue;
                if (questController == this) continue;

                Debug.Log($"[QuestController] Invitando a: {playerObj.name}");
                _partyMembers.Add(questController); //  guardar invitado
                questController.RPC_InviteToQuest(missionId);
            }
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
        if (_currentQuest == null) return;
        MissionEvents.OnMissionFailed?.Invoke(_currentQuest);
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
        Debug.Log($"[QuestController] RPC_InviteToQuest llegó al cliente");
        if (_currentQuest != null) return;

        var missionData = Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}");
        if (missionData == null) return;

        MissionEvents.OnQuestInviteReceived?.Invoke(missionData);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Unreliable)]
    private void RPC_ClientHandleError(string error, RpcInfo info = default)
    {
        //TODO: implementar manejo de errores
    }

    #endregion

    #endregion

    public void StartNewQuest(QuestDataSO questData)
    {
        Debug.Log($"[QuestTester] Misión inisiada: {questData.questName}.");

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
        Debug.Log($"[QuestController] ¡Misión fallida!: {_currentQuest.questName}");
        Debug.Log($"[QuestController] FailureQuest - partyMembers={_partyMembers.Count}");

        RPC_NotifyMissionFailed(); // avisa al dueño

        //  avisa a todos los invitados
        foreach (var member in _partyMembers)
        {
            if (member != null)
                member.RPC_NotifyMissionFailed();
        }
        _partyMembers.Clear();

        Destroy(_currentQuest);
        _currentQuest = null;
        TrackEvents.OnTrackEvent -= TrackStep;
    }

    private void CompleteQuest()
    {
        Debug.Log($"[QuestController] ¡Misión completada!: {_currentQuest.questName}");

        RPC_NotifyMissionComplete(); //  avisa al dueño

        foreach (var member in _partyMembers) //  avisa a los invitados
        {
            if (member != null)
                member.RPC_NotifyMissionComplete();
        }
        _partyMembers.Clear();

        var playerExp = GetComponent<PlayerExp>();
        playerExp.AddExperience(_currentQuest.xp);

        Destroy(_currentQuest);
        _currentQuest = null;
        TrackEvents.OnTrackEvent -= TrackStep;
    }

    private void OnDestroy()
    {
        if (_currentQuest != null)
            Destroy(_currentQuest);

        TrackEvents.OnTrackEvent -= TrackStep;
    }
}