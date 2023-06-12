namespace Screenbox.Core.Messages;

public class CriticalErrorMessage
{
    public string Message { get; set; }

    public CriticalErrorMessage(string message)
    {
        Message = message;
    }
}