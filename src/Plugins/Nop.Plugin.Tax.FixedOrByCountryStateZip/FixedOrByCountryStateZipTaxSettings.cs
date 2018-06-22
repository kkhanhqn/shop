using Nop.Core.Configuration;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip
{
    /// <summary>
    /// Represents settings of the "Fixed or by country & state & zip" tax plugin
    /// </summary>
    public class FixedOrByCountryStateZipTaxSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether the "tax calculation by country & state & zip" method is selected
        /// </summary>
        public bool CountryStateZipEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shipping tax depends on the shopping cart items (related to EU VAT on postage and delivery)
        /// </summary>
        public bool ShippingTaxDependsOnShoppingCart { get; set; }
    }
}