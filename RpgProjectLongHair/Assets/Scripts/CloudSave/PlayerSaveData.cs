[System.Serializable]
public class PlayerSaveData
{
    public int level;
    public int exp;

    public float checkpointX;
    public float checkpointY;
    public float checkpointZ;
    public bool hasCheckpoint;

    public int[] inventoryItemIds = new int[0];

    public UnityEngine.Vector3 CheckpointPosition =>
        new UnityEngine.Vector3(checkpointX, checkpointY, checkpointZ);

    public void SetCheckpoint(UnityEngine.Vector3 pos)
    {
        checkpointX = pos.x;
        checkpointY = pos.y;
        checkpointZ = pos.z;
        hasCheckpoint = true;
    }
}