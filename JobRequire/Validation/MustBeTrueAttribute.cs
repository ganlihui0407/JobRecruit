using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace JobRequire.Validation
{
    /// <summary>
    /// Validation attribute that requires a boolean value to be true.
    /// Works correctly with both server-side and client-side validation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
    {
        public MustBeTrueAttribute() : base("The {0} field must be checked.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return false;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-mustbetrue", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        }

        private static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }
            attributes.Add(key, value);
            return true;
        }
    }
}

























































