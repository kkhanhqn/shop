using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Security;
using Nop.Services.Discounts;

namespace Nop.Web.Controllers
{
    [HttpsRequirement(SslRequirement.NoMatter)]
    [WwwRequirement]
    [CheckAccessPublicStore]
    [CheckAccessClosedStore]
    [CheckLanguageSeoCode]
    [CheckDiscountCoupon]
    [CheckAffiliate]
    public abstract partial class BasePublicController : BaseController
    {
        protected virtual IActionResult InvokeHttp404()
        {
            Response.StatusCode = 404;
            return new EmptyResult();
        }

        /// <summary>
        /// Called after the action executes, before the action result
        /// </summary>
        /// <param name="context">A context for action controllers</param>
        [NonAction]
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            if (HttpContext.Items[NopDiscountDefaults.DiscountCouponQueryParameter] is IList<string>)
                foreach (var discount in HttpContext.Items[NopDiscountDefaults.DiscountCouponQueryParameter] as IList<string>)
                    SuccessNotification(string.Format(discount));
        }
    }
}