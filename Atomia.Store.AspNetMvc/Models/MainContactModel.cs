﻿using Atomia.Common.Validation;
using Atomia.Web.Plugin.Validation.ValidationAttributes;
using System.Collections.Generic;
using Atomia.Web.Plugin.Validation.HtmlHelpers;

namespace Atomia.Store.AspNetMvc.Models
{
    public class MainContactModel : ContactModel
    {
        public override string Id
        {
            get { return "MainContact"; }
        }

        [AtomiaRequired("ValidationErrors,ErrorEmptyField")]
        [CustomerValidation(CustomerValidationType.Email, "CustomerValidation,Email")]
        [AtomiaUsername("ValidationErrors,ErrorUsernameNotAvailable")]
        public override string Email { get; set; }
    }
}