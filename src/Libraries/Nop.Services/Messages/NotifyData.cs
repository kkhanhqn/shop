using System;
using System.Collections.Generic;
using System.Text;


namespace Nop.Services.Messages
{
    public struct NotifyData
    {
        public NotifyType Type;
        public string Message;
        public bool PersistForTheNextRequest;

    }
}
