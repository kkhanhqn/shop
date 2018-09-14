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
        public virtual void ErrorNotification(string message, bool persistForTheNextRequest = true, HttpContext context = null)
        {
            AddNotification(context, NotifyType.Error, message, persistForTheNextRequest);
        }

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="logException">A value indicating whether exception should be logged</param>
        /// <param name="context">HttpContext</param>
        public virtual void ErrorNotification(Exception exception, bool persistForTheNextRequest = true, bool logException = true, HttpContext context = null)
        {
            if (logException)
                LogException(exception);

            ErrorNotification(exception.Message, persistForTheNextRequest, context);
        }

        /// <summary>
        /// Display success notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="context">HttpContext</param>
        public virtual void SuccessNotification(string message, bool persistForTheNextRequest = true, HttpContext context = null)
        {
            AddNotification(context, NotifyType.Success, message, persistForTheNextRequest);
        }

        /// <summary>
        /// Display warning notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="context">HttpContext</param>
        public virtual void WarningNotification(string message, bool persistForTheNextRequest = true, HttpContext context = null)
        {
            AddNotification(context, NotifyType.Warning, message, persistForTheNextRequest);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Add notification
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <param name="type">Notification type (success/warning/error)</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void AddNotification(HttpContext context, NotifyType type, string message, bool persistForTheNextRequest)
        {
            //If a message should be persisted for the next request, it saved into a session
            if (persistForTheNextRequest)
                PrepareTempData(context ?? _httpContextAccessor.HttpContext, type, message);
            else
                PrepareContext(context ?? _httpContextAccessor.HttpContext, type, message);
        }

        /// <summary>
        /// Add message information to HttpContext
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <param name="type">Notification type (success/warning/error)</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void PrepareContext(HttpContext context, NotifyType type, string message)
        {
            //Initialize list of messages if dictionary value is null
            if (context.Items[NopMessageDefaults.NotificationListKey] == null)
                context.Items[NopMessageDefaults.NotificationListKey] = new List<NotifyData>();

            //Return if dictionary key is busy
            if (!(context.Items[NopMessageDefaults.NotificationListKey] is IList<NotifyData>))
                return;

            //Add message info into the dictionary
            ((IList<NotifyData>)context.Items[NopMessageDefaults.NotificationListKey]).Add(
                new NotifyData
                {
                    Type = type,
                    Message = message,
                });
        }

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
