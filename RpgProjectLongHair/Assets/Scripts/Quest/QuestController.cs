using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.VolumeComponent;

public class QuestController : NetworkBehaviour
{
    private QuestDataSO _currentQuest;
    private List<QuestController> _partyMembers = new(); //  nueva lista

    //Bug 1: El host no puede reintentar una quest ya completada
    //En RPC_StartMission hay esta línea que bloquea permanentemente al host:
    //
    //if (_completedQuests.Contains(missionId)) return;
    //
    //Como CompleteQuest guarda el ID en _completedQuests, la segunda vez que Player 1 intenta iniciar la misión,
    //el host se rechaza a sí mismo.
    //Fix: Eliminar ese bloqueo.El HashSet puede seguir existiendo para tracking/estadísticas, pero no debe impedir el reinicio.
    
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

        var missionData = Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}");
        if (missionData == null) return;

        // FIX: Bloquear solo si es NO repetible y ya fue completada
        if (!missionData.isRepeatable && _completedQuests.Contains(missionId))
        {
            Debug.Log($"[QuestController] Quest {missionId} ya completada y no es repetible.");
            return;
        }

        _activeMissionOwner = this;
        _activeMissionId = missionId;
        _partyMembers.Clear();

        StartNewQuest(missionData);
        RPC_NotifyMissionStarted(missionId);

        if (!missionData.allowPartyInvite)
        {
            Debug.Log($"[QuestController] Mission {missionId} started without party invites");
            return;
        }

        var runnerManager = FindFirstObjectByType<RunnerManager>();

        if (runnerManager == null)
            return;

        foreach (var playerObj in runnerManager.SpawnedPlayers.Values)
        {
            var questController = playerObj.GetComponent<QuestController>();

            if (questController == null)
                continue;

            if (questController == this)
                continue;

            // FIX: Removido el bloqueo por completadas para permitir misiones repetibles
            // if (questController.HasCompletedQuest(missionId)) continue;

            questController.RPC_InviteToQuest(missionId);
        }
    }
    #endregion

    #region Client

    private void TeleportPlayer(Vector3 destination)
    {
        var player = GetComponent<Player>();

        if (player != null)
        {
            player.TeleportTo(destination);

            Debug.Log($"[QuestController] TP aplicado correctamente a {name}");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyMissionComplete()
    {
        Debug.Log("[QuestController] RPC_NotifyMissionComplete recibido");
        if (_currentQuest == null) return;

        // FIX #2: Limpieza completa del estado local en el cliente
        TrackEvents.OnTrackEvent -= TrackStep;
        MissionEvents.OnMissionComplete?.Invoke(_currentQuest);

        // FIX #2b: Condición corregida de == null a != null
        if (_currentQuest != null)
        {
            DestroyImmediate(_currentQuest);
            _currentQuest = null;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyMissionFailed()
    {
        Debug.Log("[QuestController] RPC_NotifyMissionFailed recibido");
        var quest = _currentQuest;
        if (quest == null) return;

        // FIX: Limpiar completa del estado local en el cliente
        TrackEvents.OnTrackEvent -= TrackStep;
        MissionEvents.OnMissionFailed?.Invoke(quest);

        
        if (_currentQuest != null)
        {
            DestroyImmediate(_currentQuest);
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
        // FIX: Ignorar duplicados si ya tenemos esta misión activa
        if (_currentQuest != null && _currentQuest.questId == missionId) return;

        // FIX: Permitir reinvitación para misiones repetibles
        // if (_completedQuests.Contains(missionId)) return;

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
            DestroyImmediate(_currentQuest);
            _currentQuest = null;
        }

        RPC_NotifyMissionFailed();

        _activeMissionOwner = null;
        _activeMissionId = null;
    }
    #endregion

    #endregion

    // FIX: Nuevo método para filtrar kills por membresía del party
    public void ReportKill(PlayerRef killer)
    {
        if (!Object.HasStateAuthority) return;
        if (_currentQuest == null) return;

        bool isPartyMember = false;
        if (Runner.TryGetPlayerObject(killer, out var killerObj))
        {
            var killerQC = killerObj.GetComponent<QuestController>();
            if (killerQC == this) isPartyMember = true;
            else if (_partyMembers.Contains(killerQC)) isPartyMember = true;
        }

        if (!isPartyMember) return;

        TrackStep(QuestIds.KILL_MISSION_ENEMY, 1);
    }

    public void StartNewQuest(QuestDataSO questData)
    {
        Debug.Log($"[QuestTester] Misión iniciada: {questData.questName}.");

        // FIX: limpieza previa de suscripciones
        TrackEvents.OnTrackEvent -= TrackStep;

        // FIX: Solo el host (StateAuthority) procesa eventos de tracking globales
        if (Object.HasStateAuthority)
        {
            TrackEvents.OnTrackEvent += TrackStep;
        }
                
        if (_currentQuest != null)
            DestroyImmediate(_currentQuest);

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

        // FIX: Solo el host ejecuta la lógica de completitud/fallo
        if (Object.HasStateAuthority)
        {
            if (!_currentQuest.UpdateProgress(stepId, progress, out var isSuccess))
            {
                Debug.Log($"[QuestController] Progreso actualizado, mision en curso");
                MissionEvents.OnUpdateProgress?.Invoke(_currentQuest);

                // FIX: Sincronizar progreso a miembros del party para que actualicen su UI
                foreach (var member in _partyMembers)
                {
                    if (member == null) continue;
                    member.RPC_UpdateQuestUI(stepId, progress);
                }
                return;
            }

            Debug.Log($"[QuestController] Mision terminada, isSuccess={isSuccess}");

            if (isSuccess)
                CompleteQuest();
            else
                FailureQuest();
        }
        else
        {
            // Cliente: actualizar datos locales para UI (vía RPC o evento local)
            _currentQuest.UpdateProgress(stepId, progress, out _);
            MissionEvents.OnUpdateProgress?.Invoke(_currentQuest);
        }
    }

    // FIX: RPC para sincronizar progreso de UI a clientes sin lógica de completado
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_UpdateQuestUI(string stepId, int progress)
    {
        if (_currentQuest == null) return;
        _currentQuest.UpdateProgress(stepId, progress, out _);
        MissionEvents.OnUpdateProgress?.Invoke(_currentQuest);
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
                // FIX: Destruir la instancia del miembro, no la del host
                DestroyImmediate(member._currentQuest);
                member._currentQuest = null;
            }
        }

        _partyMembers.Clear();
        TrackEvents.OnTrackEvent -= TrackStep;
        if (_currentQuest != null)
        {
            DestroyImmediate(_currentQuest);
            _currentQuest = null;
        }
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
                // FIX: Destruir la instancia del miembro, no la del host
                DestroyImmediate(member._currentQuest);
                member._currentQuest = null;
            }
        }

        _partyMembers.Clear();

        TrackEvents.OnTrackEvent -= TrackStep;
        if (_currentQuest != null)
        {
            DestroyImmediate(_currentQuest);
            _currentQuest = null;
        }

        _activeMissionOwner = null;
        _activeMissionId = null;
    }

    private void OnDestroy()
    {
        // FIX: Asegurar limpieza de suscripciones al destruir el objeto
        TrackEvents.OnTrackEvent -= TrackStep;
        if (_currentQuest != null)
            DestroyImmediate(_currentQuest);
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
        if (!Object.HasStateAuthority)
            return;

        if (_activeMissionOwner == null)
            return;

        if (_activeMissionId != missionId)
            return;

        var missionData =
            Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}");

        if (missionData == null)
            return;

        if (_activeMissionOwner._partyMembers.Contains(this))
            return;

        _activeMissionOwner._partyMembers.Add(this);

        RPC_NotifyMissionStarted(missionId);

        if (missionData.allowTeleportParty)
        {
            TeleportPlayer(missionData.teleportDestination);
        }

        Debug.Log(
            $"[QuestController] {name} unido y teletransportado");
    }

    public static QuestController GetMissionOwner()
    {
        return _activeMissionOwner;
    }
    public static bool HasActiveMission()
    {
        return _activeMissionOwner != null;
    }

    /// <summary>
    /// Llamado desde el cliente cuando craftea un item. 
    /// El host verifica si tiene la quest activa y actualiza progreso.
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ReportCraft(string questTrackId)
    {
        if (!Object.HasStateAuthority) return;
        if (_currentQuest == null) return;
        if (string.IsNullOrEmpty(questTrackId)) return;

        Debug.Log($"[QuestController] ReportCraft recibido: {questTrackId} de {name}");

        // El TrackStep ya verifica internamente si el questTrackId coincide 
        // con algún QuestSteps.targetId de la quest actual
        TrackStep(questTrackId, 1);
    }
}