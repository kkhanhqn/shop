using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Nop.Services.Messages
{
    public partial interface INotificationService
    {
        /// <summary>
        /// Display success notification
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        void SuccessNotification(HttpContext context, string message, bool persistForTheNextRequest = true);

        /// <summary>
        /// Display warning notification
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        void WarningNotification(HttpContext context, string message, bool persistForTheNextRequest = true);

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        void ErrorNotification(HttpContext context, string message, bool persistForTheNextRequest = true);

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="exception">Exception</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="logException">A value indicating whether exception should be logged</param>
        void ErrorNotification(HttpContext context, Exception exception, bool persistForTheNextRequest = true, bool logException = true);
    }
}
