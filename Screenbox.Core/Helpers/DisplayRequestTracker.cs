using System;
using Windows.System.Display;

namespace Screenbox.Core.Helpers
{
    internal class DisplayRequestTracker
    {
        public bool IsActive => _requestCount > 0;

        private readonly DisplayRequest _displayRequest;
        private int _requestCount;

        public DisplayRequestTracker()
        {
            _displayRequest = new DisplayRequest();
        }

        public void RequestActive()
        {
            lock (_displayRequest)
            {
                try
                {
                    _displayRequest.RequestActive();
                    _requestCount++;
                }
                catch (Exception)
                {
                    // pass
                }
            }
        }

        public void RequestRelease()
        {
            lock (_displayRequest)
            {
                if (_requestCount <= 0) return;
                try
                {
                    _displayRequest.RequestRelease();
                    _requestCount--;
                }
                catch (Exception)
                {
                    // pass
                }
            }
        }
    }
}
