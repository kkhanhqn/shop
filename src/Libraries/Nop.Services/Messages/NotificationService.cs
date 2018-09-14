using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Nop.Core;
using Nop.Services.Logging;

namespace Nop.Services.Messages
{
    /// <summary>
    /// Notification service
    /// </summary>
    public partial class NotificationService : INotificationService
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public NotificationService(IHttpContextAccessor httpContextAccessor,
            ILogger logger,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            IWorkContext workContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _tempDataDictionaryFactory = tempDataDictionaryFactory;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="context">HttpContext</param>
        public virtual void ErrorNotification(string message, HttpContext context = null)
        {
            PrepareTempData(context ?? _httpContextAccessor.HttpContext, NotifyType.Error, message);
        }

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="logException">A value indicating whether exception should be logged</param>
        /// <param name="context">HttpContext</param>
        public virtual void ErrorNotification(Exception exception, bool logException = true, HttpContext context = null)
        {
            if (logException)
                LogException(exception);

            ErrorNotification(exception.Message, context);
        }

        /// <summary>
        /// Display success notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="context">HttpContext</param>
        public virtual void SuccessNotification(string message, HttpContext context = null)
        {
            PrepareTempData(context ?? _httpContextAccessor.HttpContext, NotifyType.Success, message);
        }

        /// <summary>
        /// Display warning notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="context">HttpContext</param>
        public virtual void WarningNotification(string message, HttpContext context = null)
        {
            PrepareTempData(context ?? _httpContextAccessor.HttpContext, NotifyType.Warning, message);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Save message into TempData
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <param name="type">Notification type</param>
        /// <param name="message">Message</param>
        protected virtual void PrepareTempData(HttpContext context, NotifyType type, string message)
        {
            ITempDataDictionary tempData = _tempDataDictionaryFactory.GetTempData(context);

            //If key undefined, create empty list
            if (tempData[NopMessageDefaults.NotificationListKey] == null)
                tempData[NopMessageDefaults.NotificationListKey] = new List<NotifyData>();

            //If key already exists, return
            if (!(tempData[NopMessageDefaults.NotificationListKey] is IList<NotifyData>))
                return;

            var lst = (IList<NotifyData>)tempData[NopMessageDefaults.NotificationListKey];
            lst.Add(new NotifyData
            {
                Message = message,
                Type = type
            });

            tempData[NopMessageDefaults.NotificationListKey] = lst;
        }

        /// <summary>
        /// Log exception
        /// </summary>
        /// <param name="exception">Exception</param>
        protected virtual void LogException(Exception exception)
        {
            var customer = _workContext.CurrentCustomer;
            _logger.Error(exception.Message, exception, customer);
        }

        #endregion
    }
}
