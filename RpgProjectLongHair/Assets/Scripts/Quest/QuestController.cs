using UnityEngine;
using Fusion;


public class QuestController : NetworkBehaviour
{
    private QuestDataSO _currentQuest; // en el caso que se acepte más de una se hace con lista!

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

        var missionData = Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}"); //debería estar en la carpeta Resources?  
        if (missionData == null)
        {
            RPC_ClientHandleError($"No se encontró la misión {missionId}");
            return;
        }

        StartNewQuest(missionData);

        if (missionData.allowTeleportParty)
        {
            var runnerManager = FindFirstObjectByType<RunnerManager>();
            foreach (var playerObj in runnerManager.SpawnedPlayers.Values)
            {
                var questController = playerObj.GetComponent<QuestController>();
                if (questController != null)
                    questController.RPC_InviteToQuest(missionId);
            }
        }
    }
    #endregion

    #region Client

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Unreliable)]
    private void RPC_ClientHandleError(string error, RpcInfo info = default)
    {
        //TODO: implementar manejo de errores
    }

    // inicia misión para TODOS los players
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_InviteToQuest(string missionId)
    {
        var missionData = Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}");
        if (missionData == null) return;

        // solo iniciar si este player NO tiene misión activa
        if (_currentQuest != null) return;

        // iniciar la misión
        StartNewQuest(missionData);
    }

    #endregion

    #endregion

    public void StartNewQuest(QuestDataSO questData)
    {
        Debug.Log($"[QuestTester] Misión inisiada: {questData.questName}.");

        //Evitar doble suscripcion
        TrackEvents.OnTrackEvent -= TrackStep; // si ya estaba, me saco
        TrackEvents.OnTrackEvent += TrackStep; // me agrego exactamente una vez

        if (_currentQuest != null)
        {
            Destroy(_currentQuest);
        }
        _currentQuest = Instantiate(questData);
    }

    // seguimiento (pick_XP, 5) <-- ej: lo que llega de parámetros
    public void TrackStep(string stepId, int progress)
    {
        // verificación de que haya una misión en curso!
        if (_currentQuest == null)
        {
            Debug.Log("[QuestController] TrackStep: _currentQuest es null");
            return;
        }

        Debug.Log($"[QuestController] TrackStep recibido: id={stepId}, progress={progress}");

        //Obtener todos los steps que tengan el id del track que me llego
        if (!_currentQuest.UpdateProgress(stepId, progress, out var isSuccess))
        {
            Debug.Log($"[QuestController] Progreso actualizado, mision en curso");
            MissionEvents.OnUpdateProgress?.Invoke(_currentQuest);
            return;
            //UpdateProgress devuelve false  ->  aviso al HUD  ->  return (misión en curso)
        }

        Debug.Log($"[QuestController] Mision terminada, isSuccess={isSuccess}");

        // UpdateProgress devuelve true   ->  ¿éxito o falla?  ->  Complete o Failure
        if (isSuccess)
        {
            CompleteQuest();
        }
        else
        {
            FailureQuest();
        }
    }


    private void FailureQuest()
    {
        Debug.Log($"[QuestController] ¡Misión fallida!: {_currentQuest.questName}");

        MissionEvents.OnMissionFailed?.Invoke(_currentQuest);
        Destroy(_currentQuest);
        _currentQuest = null;
        //Actualizar UI - en la 2° clase hace una clase MissionHud  se suscribe al observer
        TrackEvents.OnTrackEvent -= TrackStep;
    }

    private void CompleteQuest()
    {
        Debug.Log($"[QuestController] ¡Misión completada!: {_currentQuest.questName}");

        MissionEvents.OnMissionComplete?.Invoke(_currentQuest);

        // Recompensa de XP al completar la misión
        var playerExp = GetComponent<PlayerExp>();
        playerExp.AddExperience(_currentQuest.xp);

        Destroy(_currentQuest);
        _currentQuest = null;
        TrackEvents.OnTrackEvent -= TrackStep;
    }

    private void OnDestroy()
    {
        if (_currentQuest != null)
        {
            Destroy(_currentQuest);
        }
        TrackEvents.OnTrackEvent -= TrackStep;
    }

}
