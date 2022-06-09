namespace Screenbox.Core.Messages
{
    internal class ErrorMessage
    {
        public string Title { get; set; }

        public string Message { get; set; }

        public ErrorMessage(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
}
