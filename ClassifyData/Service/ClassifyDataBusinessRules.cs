using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vidyano.Core.Extensions;
using Vidyano.Service;
using Vidyano.Service.Repository;

namespace ClassifyData.Service
{
    /// <summary>
    /// This class contains the custom C# business rules for this Vidyano application.
    /// </summary>
    /// <remarks>
    /// Vidyano will replace {0} with the label of the attribute that failed the rule, and any extra {n} arguments with the value of the extra argument for that rule.
    /// </remarks>
    public static class ClassifyDataBusinessRules
    {
        //public static string NotEmpty(BusinessRulesArgs args, string value)
        //{
        //    if (string.IsNullOrWhiteSpace(value))
        //        return "{0} cannot be empty.";
        //
        //    return null;
        //}
    }
}