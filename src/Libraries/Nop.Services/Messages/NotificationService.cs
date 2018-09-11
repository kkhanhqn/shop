using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Services.Logging;

namespace Nop.Services.Messages
{
    public partial class NotificationService : INotificationService
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public NotificationService(
            ILogger logger,
            IWorkContext workContext)
        {
            _logger = logger;
            _workContext = workContext;
        }

        #endregion

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        public virtual void ErrorNotification(HttpContext context, string message, bool persistForTheNextRequest = true)
        {
            PrepareContext(context, NotifyType.Error, message, persistForTheNextRequest);
        }

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="exception">Exception</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="logException">A value indicating whether exception should be logged</param>
        public virtual void ErrorNotification(HttpContext context, Exception exception, bool persistForTheNextRequest = true, bool logException = true)
        {
            if (logException)
                LogException(exception);

            ErrorNotification(context, exception.Message, persistForTheNextRequest);
        }

        /// <summary>
        /// Display success notification
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        public virtual void SuccessNotification(HttpContext context, string message, bool persistForTheNextRequest = true)
        {
            PrepareContext(context, NotifyType.Success, message, persistForTheNextRequest);
        }

        /// <summary>
        /// Display warning notification
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        public virtual void WarningNotification(HttpContext context, string message, bool persistForTheNextRequest = true)
        {
            PrepareContext(context, NotifyType.Warning, message, persistForTheNextRequest);
        }

        protected virtual void PrepareContext(HttpContext context, NotifyType type, string message, bool persistForTheNextRequest)
        {
            if (context.Items[NotificationSettings.MessageListKey] == null)
                context.Items[NotificationSettings.MessageListKey] = new List<NotifyData>();

            if (!(context.Items[NotificationSettings.MessageListKey] is IList<NotifyData>))
                throw new Exception();

            ((IList<NotifyData>)context.Items[NotificationSettings.MessageListKey]).Add(
                new NotifyData
                {
                    Type = type,
                    Message = message,
                    PersistForTheNextRequest = persistForTheNextRequest
                });
        }

        /// <summary>
        /// Log exception
        /// </summary>
        /// <param name="exception">Exception</param>
        protected void LogException(Exception exception)
        {
            var customer = _workContext.CurrentCustomer;
            _logger.Error(exception.Message, exception, customer);
        }
    }
}
