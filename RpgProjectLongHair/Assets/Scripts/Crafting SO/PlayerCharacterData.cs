using UnityEngine;


public enum CharacterType
{
    None,
    Fungi,
    Mecano
}

public class PlayerCharacterData : MonoBehaviour
{
    public CharacterType characterType;
}
