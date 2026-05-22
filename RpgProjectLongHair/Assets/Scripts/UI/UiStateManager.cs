using UnityEngine;

public static class UiStateManager
{
    private static int _blockingCount;
    public static bool HasBlockingUI => _blockingCount > 0;

    public static void OpenBlockingUI()
    {
        _blockingCount++;
        Debug.Log($"[UiStateManager] OpenBlockingUI — count: {_blockingCount}");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void CloseBlockingUI()
    {
        _blockingCount--;
        Debug.Log($"[UiStateManager] CloseBlockingUI — count: {_blockingCount}");
        if (_blockingCount < 0)
        {
            Debug.LogWarning("[UiStateManager] Blocking count negativo.");
            _blockingCount = 0;
        }
        if (_blockingCount == 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public static void ForceReset()
    {
        _blockingCount = 0;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[UiStateManager] ForceReset ejecutado");
    }
}