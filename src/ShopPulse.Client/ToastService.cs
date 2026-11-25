namespace ShopPulse.Client;

public sealed class ToastService
{
    private static readonly TimeSpan SuccessDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan ErrorDuration = TimeSpan.FromSeconds(6);
    private CancellationTokenSource? _dismissal;

    public event Action? OnChange;
    public string Message { get; private set; } = "";
    public string Type { get; private set; } = "success";

    public void ShowSuccess(string message) => Show(message, "success");
    public void ShowError(string message) => Show(message, "error");

    public void Clear()
    {
        var previous = Interlocked.Exchange(ref _dismissal, null);
        if (previous is not null)
        {
            previous.Cancel();
            previous.Dispose();
        }

        Message = "";
        OnChange?.Invoke();
    }

    private void Show(string message, string type)
    {
        var next = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _dismissal, next);
        if (previous is not null)
        {
            previous.Cancel();
            previous.Dispose();
        }

        Message = message;
        Type = type;
        OnChange?.Invoke();
        _ = DismissAfterDelayAsync(next, message, type == "error" ? ErrorDuration : SuccessDuration);
    }

    private async Task DismissAfterDelayAsync(
        CancellationTokenSource owner,
        string message,
        TimeSpan duration)
    {
        try
        {
            await Task.Delay(duration, owner.Token);
            if (ReferenceEquals(Volatile.Read(ref _dismissal), owner)
                && Message == message)
            {
                Clear();
            }
        }
        catch (OperationCanceledException)
        {
            // A newer notification, manual dismissal, or navigation replaced this one.
        }
    }
}