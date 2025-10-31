using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using Unity.VisualScripting;
using System;

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

        var missionData = Resources.Load<QuestDataSO>($"{MISSION_PATH}{missionId}.assets"); //debería estar en la carpeta Resources?  
        if(missionData == null)
        {
            RPC_ClientHandleError($"No se encontró la misión {missionId}");
            return;
        }

        StartNewQuest(missionData);
    }
    #endregion

    #region Client

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Unreliable)]
    private void RPC_ClientHandleError(string error, RpcInfo info = default)
    {
        //TODO: implementar manejo de errores
    }

    #endregion

    #endregion



    public void StartNewQuest(QuestDataSO questData)
    {
        TrackEvents.OnTrackEvent += TrackStep;
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
        if (_currentQuest == null) return;

        //Obtener todos los steps que tengan el id del track que me llegó
        if (!_currentQuest.UpdateProgress(stepId, progress, out var isSuccess)) return;

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
        MissionEvents.OnMissionFailed?.Invoke(_currentQuest);
        Destroy(_currentQuest);
        _currentQuest = null;
        //Actualizar UI - en la 2° clase hace una clase MissionHud  se suscribe al observer
        TrackEvents.OnTrackEvent -= TrackStep;
    }

    private void CompleteQuest()
    {
        MissionEvents.OnMissionComplete?.Invoke(_currentQuest);
        Destroy(_currentQuest);
        _currentQuest = null;
        //Actualizar UI - en la 2° clase hace una clase MissionHud  se suscribe al observer
        //Guardar el estado de las misiones
        //Obtener recompensas (xp. coins)
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
