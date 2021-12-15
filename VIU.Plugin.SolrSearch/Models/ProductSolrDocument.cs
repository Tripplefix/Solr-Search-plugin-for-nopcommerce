using SolrNet.Attributes;
using System.Collections.Generic;

namespace VIU.Plugin.SolrSearch.Models
{
    public class ProductSolrDocument
    {
        public const string SOLRFIELD_TEXTFIELD_EXTENSION = "_t";
        public const string SOLRFIELD_DEFAULT_TEXTFIELD_PART = "_default";
        public const string SOLRFIELD_TEXTFIELD_IDENTIFIER = "_txt_"; // <--

        public const string SOLRFIELD_INTEGERFIELD_EXTENSION = "_i";
        public const string SOLRFIELD_DOUBLEFIELD_EXTENSION = "_d";
        public const string SOLRFIELD_BOOLEANFIELD_EXTENSION = "_b";

        public const string SOLRFIELD_MULITVALUETEXT_EXTENSION = "attr_";

        public const string SOLRFIELD_SE_NAME = "sename";
        public const string SOLRFIELD_SKU = "sku";
        public const string SOLRFIELD_GTIN = "gtin";
        public const string SOLRFIELD_NAME = "name";
        public const string SOLRFIELD_SHORTDESCRIPTION = "shortdescription";
        public const string SOLRFIELD_FULLDESCRIPTION = "fulldescription";
        public const string SOLRFIELD_ALLCATEGORIES = "allcategories";
        public const string SOLRFIELD_ALLMANUFACTURERS = "allmanufacturers";

        public const string SOLRFIELD_PREFIX_SPECIFICATION_ATTRIBUTES = "SA";

        [SolrUniqueKey("id")]
        public int Id { get; set; }

        [SolrField(SOLRFIELD_SKU + SOLRFIELD_TEXTFIELD_EXTENSION)]
        public string Sku { get; set; }

        [SolrField(SOLRFIELD_GTIN + SOLRFIELD_TEXTFIELD_EXTENSION)]
        public string Gtin { get; set; }

        [SolrField("producttype" + SOLRFIELD_TEXTFIELD_EXTENSION)]
        public string ProductType { get; set; }

        [SolrField("manufacturer" + SOLRFIELD_TEXTFIELD_EXTENSION)]
        public string Manufacturer { get; set; }

        [SolrField("defaultimageurl" + SOLRFIELD_TEXTFIELD_EXTENSION)]
        public string DefaultImageUrl { get; set; }

        [SolrField("thumbimageurl" + SOLRFIELD_TEXTFIELD_EXTENSION)]
        public string ThumbImageUrl { get; set; }

        [SolrField("fullsizeimageurl" + SOLRFIELD_TEXTFIELD_EXTENSION)]
        public string FullSizeImageUrl { get; set; }

        [SolrField("disablebuybutton" + SOLRFIELD_BOOLEANFIELD_EXTENSION)]
        public bool DisableBuyButton { get; set; }

        [SolrField("disablewishlistbutton" + SOLRFIELD_BOOLEANFIELD_EXTENSION)]
        public bool DisableWishlistButton { get; set; }

        [SolrField("disablewishaddtocomparebutton" + SOLRFIELD_BOOLEANFIELD_EXTENSION)]
        public bool DisableAddToCompareListButton { get; set; }

        [SolrField(SOLRFIELD_MULITVALUETEXT_EXTENSION + SOLRFIELD_ALLCATEGORIES)]
        public List<string> AllCategories { get; set; }

        [SolrField(SOLRFIELD_MULITVALUETEXT_EXTENSION + SOLRFIELD_ALLMANUFACTURERS)]
        public List<string> AllManufacturers { get; set; }

        [SolrField("*")]
        public IDictionary<string, object> OtherFields { get; set; }
    }
}
