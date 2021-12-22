using System;
using Nop.Core.Domain.Localization;
using Nop.Plugin.SolrSearch.Models;

namespace Nop.Plugin.SolrSearch.Tools
{
    public static class SolrTools
    {
        public static string GetStaticTextFieldName(string fieldName)
        {
            return fieldName + ProductSolrDocument.SOLRFIELD_TEXTFIELD_EXTENSION;
        }

        public static string GetLocalizedTextFieldName(string fieldName, string languageKey, bool isDefault = false)
        {
            return fieldName + (isDefault ? ProductSolrDocument.SOLRFIELD_DEFAULT_TEXTFIELD_PART : "") + ProductSolrDocument.SOLRFIELD_TEXTFIELD_IDENTIFIER + languageKey;
        }

        public static string GetLanguageKey(Language language)
        {
            return language.LanguageCulture[..(language.LanguageCulture.IndexOf("-", StringComparison.Ordinal))];
        }
    }
}
