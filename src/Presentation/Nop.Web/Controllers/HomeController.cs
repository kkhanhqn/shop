using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Security;
using Nop.Services.Discounts;
using Nop.Services.Localization;

namespace Nop.Web.Controllers
{
    public partial class HomeController : BasePublicController
    {
        #region Fields 

        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public HomeController(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        #endregion

        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult Index()
        {
            if (HttpContext.Items[NopDiscountDefaults.DiscountCouponQueryParameter] is IList<string>)
                foreach (var discount in HttpContext.Items[NopDiscountDefaults.DiscountCouponQueryParameter] as IList<string>)
                    SuccessNotification(string.Format(_localizationService.GetResource("Enums.Nop.Core.Domain.Discounts.DiscountNotifications"), discount));

            return View();
        }
    }
}