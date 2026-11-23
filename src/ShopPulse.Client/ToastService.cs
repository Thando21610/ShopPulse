namespace ShopPulse.Client;

public sealed class ToastService
{
    public event Action? OnChange;
    public string Message { get; private set; } = "";
    public string Type { get; private set; } = "success";

    public void ShowSuccess(string message) => Show(message, "success");
    public void ShowError(string message) => Show(message, "error");

    public void Clear()
    {
        Message = "";
        OnChange?.Invoke();
    }

    private void Show(string message, string type)
    {
        Message = message;
        Type = type;
        OnChange?.Invoke();
    }
}
