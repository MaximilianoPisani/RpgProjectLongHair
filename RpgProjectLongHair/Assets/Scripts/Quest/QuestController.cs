using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using Unity.VisualScripting;

public class QuestController : NetworkBehaviour
{
    private QuestDataSO _currentQuest; // en el caso que se acepte m�s de una se hace con lista!

    public QuestDataSO currentQuest => _currentQuest;

    public void StartNewQuest(QuestDataSO questData)
    {
        if (_currentQuest != null)
        {
            Destroy(_currentQuest);
        }
        _currentQuest = Instantiate(questData);
    }

    public void TrackStep(string stepId, int progress)
    {
        // verificaci�n de que haya una misi�n en curso!
        if (_currentQuest == null) return;

        //Obtener todos los steps que tengan el id del track que me lleg�
        var steps = _currentQuest.questSteps.Where(step => step.targetId == stepId)!; //Where pertenece a LinQ

        foreach (var step in steps)
        {
            step.currentAmount += progress;
            if (step.amount >= step.currentAmount)
            {
                step.isComplete = true;
            }

        }
    }

}
