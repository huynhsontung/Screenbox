namespace ModernVLC.Services
{
    internal class LibVLCService : LibVLCSharp.Shared.LibVLC
    {
        public LibVLCService(bool enableDebugLogs, params string[] options)
            : base(enableDebugLogs, options)
        {
            Log += LibVLCService_Log;
        }

        private void LibVLCService_Log(object sender, LibVLCSharp.Shared.LogEventArgs e)
        {
            LogService.Log(e.FormattedLog, "LibVLC");
        }
    }
}
