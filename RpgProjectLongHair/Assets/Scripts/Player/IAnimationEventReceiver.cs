public interface IAnimationEventReceiver
{
    void OnHitFrame();          // Momento exacto para aplicar daño
    void OpenComboWindow();     // Inicia ventana para encadenar
    void CloseComboWindow();    // Cierra ventana
    void EndAttack();           // Fin de la animación de ataque

    public void OnShootFrame();
    public void OnShootAnimationEnd();
    public void OnReloadComplete();

}