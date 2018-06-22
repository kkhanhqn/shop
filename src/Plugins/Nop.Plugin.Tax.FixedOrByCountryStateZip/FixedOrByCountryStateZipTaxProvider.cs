using System;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Core.Plugins;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Data;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Infrastructure.Cache;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Tax;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip
{
    /// <summary>
    /// Fixed or by country & state & zip rate tax provider
    /// </summary>
    public class FixedOrByCountryStateZipTaxProvider : BasePlugin, ITaxProvider
    {
        #region Fields

        private readonly CountryStateZipObjectContext _objectContext;
        private readonly FixedOrByCountryStateZipTaxSettings _countryStateZipSettings;
        private readonly ICountryStateZipService _taxRateService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ISettingService _settingService;
        private readonly IStaticCacheManager _cacheManager;
        private readonly IStoreContext _storeContext;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly TaxSettings _taxSettings;

        #endregion

        #region Ctor

        public FixedOrByCountryStateZipTaxProvider(CountryStateZipObjectContext objectContext,
            FixedOrByCountryStateZipTaxSettings countryStateZipSettings,
            ICountryStateZipService taxRateService,
            IPriceCalculationService priceCalculationService,
            ISettingService settingService,
            IStaticCacheManager cacheManager,
            IStoreContext storeContext,
            ITaxCategoryService taxCategoryService,
            ITaxService taxService,
            IWebHelper webHelper,
            TaxSettings taxSettings)
        {
            this._objectContext = objectContext;
            this._countryStateZipSettings = countryStateZipSettings;
            this._taxRateService = taxRateService;
            this._priceCalculationService = priceCalculationService;
            this._settingService = settingService;
            this._cacheManager = cacheManager;
            this._storeContext = storeContext;
            this._taxCategoryService = taxCategoryService;
            this._taxService = taxService;
            this._webHelper = webHelper;
            this._taxSettings = taxSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        public CalculateTaxResult GetTaxRate(CalculateTaxRequest calculateTaxRequest)
        {
            var result = new CalculateTaxResult();

            //the tax rate calculation by fixed rate
            if (!_countryStateZipSettings.CountryStateZipEnabled)
            {
                //use the special rule for shipping tax when it depends on the shopping cart items (related to EU VAT on postage and delivery)
                //see details at the https://www.gov.uk/guidance/vat-on-postage-delivery-and-direct-marketing-notice-70024
                if (calculateTaxRequest.TaxCategoryId > 0 && calculateTaxRequest.TaxCategoryId == _taxSettings.ShippingTaxClassId
                    && _countryStateZipSettings.ShippingTaxDependsOnShoppingCart)
                {
                    //get all tax rates applied to customer shopping cart
                    var shoppingCart = calculateTaxRequest.Customer.ShoppingCartItems
                        .Where(item => item.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
                    var taxRates = shoppingCart.Select(shoppingCartItem =>
                    {
                        var shoppingCartItemPrice = _priceCalculationService.GetSubTotal(shoppingCartItem);
                        _taxService.GetProductPrice(shoppingCartItem.Product, shoppingCartItemPrice, calculateTaxRequest.Customer, out decimal taxRate);
                        return taxRate;
                    }).ToList();

                    //use max tax rate as the shipping one
                    result.TaxRate = taxRates.Max();
                    return result;
                }

                result.TaxRate = _settingService.GetSettingByKey<decimal>(string.Format(FixedOrByCountryStateZipDefaults.FixedRateSettingsKey, calculateTaxRequest.TaxCategoryId));
                return result;
            }

            //the tax rate calculation by country & state & zip 
            if (calculateTaxRequest.Address == null)
            {
                result.Errors.Add("Address is not set");
                return result;
            }

            //first, load all tax rate records (cached) - loaded only once
            var cacheKey = ModelCacheEventConsumer.ALL_TAX_RATES_MODEL_KEY;
            var allTaxRates = _cacheManager.Get(cacheKey, () => _taxRateService.GetAllTaxRates().Select(taxRate => new TaxRateForCaching
            {
                Id = taxRate.Id,
                StoreId = taxRate.StoreId,
                TaxCategoryId = taxRate.TaxCategoryId,
                CountryId = taxRate.CountryId,
                StateProvinceId = taxRate.StateProvinceId,
                Zip = taxRate.Zip,
                Percentage = taxRate.Percentage
            }).ToList());

            var storeId = _storeContext.CurrentStore.Id;
            var taxCategoryId = calculateTaxRequest.TaxCategoryId;
            var countryId = calculateTaxRequest.Address.Country?.Id ?? 0;
            var stateProvinceId = calculateTaxRequest.Address.StateProvince?.Id ?? 0;
            var zip = calculateTaxRequest.Address.ZipPostalCode?.Trim() ?? string.Empty;

            var existingRates = allTaxRates.Where(taxRate => taxRate.CountryId == countryId && taxRate.TaxCategoryId == taxCategoryId);

            //filter by store
            var matchedByStore = existingRates.Where(taxRate => storeId == taxRate.StoreId || taxRate.StoreId == 0);

            //filter by state/province
            var matchedByStateProvince = matchedByStore.Where(taxRate => stateProvinceId == taxRate.StateProvinceId || taxRate.StateProvinceId == 0);

            //filter by zip
            var matchedByZip = matchedByStateProvince.Where(taxRate => string.IsNullOrWhiteSpace(taxRate.Zip) || taxRate.Zip.Equals(zip, StringComparison.InvariantCultureIgnoreCase));

            //sort from particular to general, more particular cases will be the first
            var foundRecords = matchedByZip.OrderBy(r => r.StoreId == 0).ThenBy(r => r.StateProvinceId == 0).ThenBy(r => string.IsNullOrEmpty(r.Zip));

            var foundRecord = foundRecords.FirstOrDefault();

            if (foundRecord != null)
                result.TaxRate = foundRecord.Percentage;

            return result;
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/FixedOrByCountryStateZip/Configure";
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //database objects
            _objectContext.Install();

            //settings
            _settingService.SaveSetting(new FixedOrByCountryStateZipTaxSettings());

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fixed", "Fixed rate");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.TaxByCountryStateZip", "By Country");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.TaxCategoryName", "Tax category");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Rate", "Rate");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Store", "Store");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Store.Hint", "If an asterisk is selected, then this shipping rate will apply to all stores.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Country", "Country");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Country.Hint", "The country.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.StateProvince", "State / province");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.StateProvince.Hint", "If an asterisk is selected, then this tax rate will apply to all customers from the given country, regardless of the state.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Zip", "Zip");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Zip.Hint", "Zip / postal code. If zip is empty, then this tax rate will apply to all customers from the given country or state, regardless of the zip code.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.TaxCategory", "Tax category");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.TaxCategory.Hint", "The tax category.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Percentage", "Percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Percentage.Hint", "The tax rate.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.AddRecord", "Add tax rate");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.AddRecordTitle", "New tax rate");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.ShippingTaxDependsOnShoppingCart", "Shipping tax depends on the shopping cart items (EU VAT)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.ShippingTaxDependsOnShoppingCart.Hint", "Check if you deliver goods to customers (delivery is included in the contract) and make an additional charge. In this case the VAT liability of delivered goods is based on the liability of those goods, so shipping tax depends on the shopping cart items. In order to use this functionality you have to set 'Shipping tax category' on Tax settings. If you want to use standard VAT rate for the shipping, uncheck this setting and set rate for 'Shipping tax category' in the table above.");

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<FixedOrByCountryStateZipTaxSettings>();

            //fixed rates
            var fixedRates = _taxCategoryService.GetAllTaxCategories()
                .Select(taxCategory => _settingService.GetSetting(string.Format(FixedOrByCountryStateZipDefaults.FixedRateSettingsKey, taxCategory.Id)))
                .Where(setting => setting != null).ToList();
            _settingService.DeleteSettings(fixedRates);

            //database objects
            _objectContext.Uninstall();

            //locales
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fixed");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.TaxByCountryStateZip");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.TaxCategoryName");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Rate");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Store");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Store.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Country");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Country.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.StateProvince");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.StateProvince.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Zip");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Zip.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.TaxCategory");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.TaxCategory.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Percentage");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.Percentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.AddRecord");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.AddRecordTitle");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.ShippingTaxDependsOnShoppingCart");
            this.DeletePluginLocaleResource("Plugins.Tax.FixedOrByCountryStateZip.Fields.ShippingTaxDependsOnShoppingCart.Hint");

            base.Uninstall();
        }

        #endregion
    }
}