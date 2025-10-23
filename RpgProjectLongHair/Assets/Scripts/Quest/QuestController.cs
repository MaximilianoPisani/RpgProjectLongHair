using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using Unity.VisualScripting;
using System;

public class QuestController : NetworkBehaviour
{
    private QuestDataSO _currentQuest; // en el caso que se acepte más de una se hace con lista!

    public QuestDataSO currentQuest => _currentQuest;

    public void StartNewQuest(QuestDataSO questData)
    {
        TrackEvents.OnTrackEvent += TrackStep;
        if (_currentQuest != null)
        {
            Destroy(_currentQuest);
        }
        _currentQuest = Instantiate(questData);
    }

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
        Destroy(_currentQuest);
        _currentQuest = null;
        // llamar a evento de UI
        TrackEvents.OnTrackEvent -= TrackStep;
    }

    private void CompleteQuest()
    {
        Destroy(_currentQuest);
        _currentQuest = null;
        //Actualizar UI
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
