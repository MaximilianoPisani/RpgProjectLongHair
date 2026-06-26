using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Singleton: solo existe uno y no se destruye al cambiar de escena
    public static AudioManager Instance { get; private set; }

    [Header("UI")]
    public EventReference uiClickEvent;
    public EventReference levelUpEvent;
    public EventReference checkPointEvent;
    public EventReference stoneEvent;
    public EventReference gateEvent;
    public EventReference chestEvent;

    [Header("Player Fungi Actions")]
    public EventReference playerJumpEvent;
    public EventReference playerThrowEvent;
    public EventReference playerBlandirEvent;
    public EventReference playerTakeDamageEvent;
    public EventReference pickUpEvent;
    public EventReference drawSwordEvent;
    public EventReference playerAgitation;

    [Header("Player Mecano Actions")]
    //public EventReference playerMecMele;
    public EventReference playerMecSteps;
    public EventReference playerMecRango;
    public EventReference playerMecTakeDamage;
    public EventReference playerMecJump;

    [Header("Combat")]
    public EventReference swordAirEvent;
    public EventReference attackMeleeEvent;
    public EventReference attackRangoEvent;
    public EventReference attackRangoReloadEvent;

    [Header("Fungi-Steps")]
    public EventReference stepGraveEvent;
    public EventReference stepGraveCaveEvent;
    public EventReference stepWoodEvent;
    public EventReference stepWoodCaveEvent;

    [Header("Enemy")]
    public EventReference attackRangoEnemyEvent;
    public EventReference attackEnemyKamikazeEvent;

    [Header("Ambient")]
    //public EventReference ambWindEvent;
    public EventReference ambBirdEvent;

    [Header("Music")]
    public EventReference musicEvent;

    [Header("Quest")]
    public EventReference stgVictoryEvent;
    public EventReference stgDeffeatEvent;
    public EventReference takeQuestEvent;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ============================================
    // UI (2D - sin posición)
    // ============================================
    public void PlayUIClick()
    {
        if (!uiClickEvent.IsNull)
            RuntimeManager.PlayOneShot(uiClickEvent);
    }

    public void PlayLevelUp()
    {
        if (!levelUpEvent.IsNull)
            RuntimeManager.PlayOneShot(levelUpEvent);
    }

    public void PlayCheckPoint()
    {
        if (!checkPointEvent.IsNull)
            RuntimeManager.PlayOneShot(checkPointEvent);
    }

    public void PlayStone()
    {
        if (!stoneEvent.IsNull)
            RuntimeManager.PlayOneShot(stoneEvent);
    }
    public void PlayGate()
    {
        if (!gateEvent.IsNull)
            RuntimeManager.PlayOneShot(gateEvent);
    }

    public void PlayChest()
    {
        if (!chestEvent.IsNull)
            RuntimeManager.PlayOneShot(chestEvent);
    }

    // ============================================
    // Player (2D - sin posición)
    // ============================================
    public void PlayJump(CharacterType type)
    {
        if (type == CharacterType.Mecano)
        {
            if (!playerMecJump.IsNull)
               RuntimeManager.PlayOneShot(playerMecJump);
        }
        else
        {
            if (!playerJumpEvent.IsNull)
                RuntimeManager.PlayOneShot(playerJumpEvent);
        }
    }

    public void PlayThrow()
    {
        if (!playerThrowEvent.IsNull)
            RuntimeManager.PlayOneShot(playerThrowEvent);
    }

    public void PlayBlandir(CharacterType type)
    {
        if (type == CharacterType.Mecano)
        {
            //if (!playerBlandirEvent.IsNull)
            //    RuntimeManager.PlayOneShot(playerBlandirEvent); EL blandir es el esfuerzo verbal
            return;
        }
        else
        {
            if (!playerBlandirEvent.IsNull)
                RuntimeManager.PlayOneShot(playerBlandirEvent);
        }
    }

    public void PlayTakeDamage(CharacterType type)
    {
        if (type == CharacterType.Mecano)
        {
            if (!playerMecTakeDamage.IsNull)
                RuntimeManager.PlayOneShot(playerMecTakeDamage);
        }
        else
        {
            if (!playerTakeDamageEvent.IsNull)
                RuntimeManager.PlayOneShot(playerTakeDamageEvent);
        }
    }

    public void PlayPickUp()
    {
        if (!pickUpEvent.IsNull)
            RuntimeManager.PlayOneShot(pickUpEvent);
    }

    public void PlayDrawSword()
    {
        if (!drawSwordEvent.IsNull)
            RuntimeManager.PlayOneShot(drawSwordEvent);
    }

    public void PlayAgitation()
    {
        if (!playerAgitation.IsNull)
            RuntimeManager.PlayOneShot(playerAgitation);
    }

    // ============================================
    // Combat (2D - sin posición)
    // ============================================
    public void PlaySwordAir()
    {
        if (!swordAirEvent.IsNull)
            RuntimeManager.PlayOneShot(swordAirEvent);
    }

    public void PlayAttackMelee()
    {
        if (!attackMeleeEvent.IsNull)
            RuntimeManager.PlayOneShot(attackMeleeEvent);
    }

    public void PlayAttackRango()
    {
        if (!attackRangoEvent.IsNull)
            RuntimeManager.PlayOneShot(attackRangoEvent);
    }

    public void PlayAttackRangoReload()
    {
        if (!attackRangoReloadEvent.IsNull)
            RuntimeManager.PlayOneShot(attackRangoReloadEvent);
    }

    public void PlayAttackEnemyRango()
    {
        if (!attackRangoEnemyEvent.IsNull)
            RuntimeManager.PlayOneShot(attackRangoEnemyEvent);
    }

    public void PlayAttackEnemyKamikaze()
    {
        if (!attackEnemyKamikazeEvent.IsNull)
            RuntimeManager.PlayOneShot(attackEnemyKamikazeEvent);
    }

    // ============================================
    // Steps (2D - sin posición)
    // ============================================
    public void PlayStepGrave()
    {
        if (!stepGraveEvent.IsNull)
            RuntimeManager.PlayOneShot(stepGraveEvent);
    }

    public void PlayStepGraveCave()
    {
        if (!stepGraveCaveEvent.IsNull)
            RuntimeManager.PlayOneShot(stepGraveCaveEvent);
    }

    public void PlayStepWood()
    {
        if (!stepWoodEvent.IsNull)
            RuntimeManager.PlayOneShot(stepWoodEvent);
    }

    public void PlayStepWoodCave()
    {
        if (!stepWoodCaveEvent.IsNull)
            RuntimeManager.PlayOneShot(stepWoodCaveEvent);
    }

    // ============================================
    // Ambient (2D - sin posición)
    // NOTA: Si son loops, más adelante se cambiarán a Play/Stop controlado
    // ============================================
    //public void PlayAmbWind()
    //{
    //    if (!ambWindEvent.IsNull)
    //        RuntimeManager.PlayOneShot(ambWindEvent);
    //}

    public void PlayAmbBird()
    {
        if (!ambBirdEvent.IsNull)
            RuntimeManager.PlayOneShot(ambBirdEvent);
    }

    // ============================================
    // Music (2D - sin posición)
    // NOTA: La música probablemente sea un loop. 
    // Esto es temporal para probar; luego se hará con start/stop.
    // ============================================
    public void PlayMusic()
    {
        if (!musicEvent.IsNull)
            RuntimeManager.PlayOneShot(musicEvent);
    }

    // ============================================
    // Quest / Stingers (2D - sin posición)
    // ============================================
    public void PlayVictoryQuest()
    {
        if (!stgVictoryEvent.IsNull)
            RuntimeManager.PlayOneShot(stgVictoryEvent);
    }

    public void PlayDeffeatQuest()
    {
        if (!stgDeffeatEvent.IsNull)
            RuntimeManager.PlayOneShot(stgDeffeatEvent);
    }

    public void PlayTakeQuest()
    {
        if (!takeQuestEvent.IsNull)
            RuntimeManager.PlayOneShot(takeQuestEvent);
    }
}
