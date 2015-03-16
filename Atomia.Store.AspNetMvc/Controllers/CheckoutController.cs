﻿using Atomia.Store.AspNetMvc.Models;
using Atomia.Store.AspNetMvc.Ports;
using Atomia.Store.Core;
using System.Collections.Generic;
using System.Web.Mvc;
using Atomia.Store.AspNetMvc.Filters;
using System.Linq;
using System;

namespace Atomia.Store.AspNetMvc.Controllers
{
    public sealed class CheckoutController : Controller
    {
        private readonly IEnumerable<PaymentMethodForm> paymentMethodForms = DependencyResolver.Current.GetServices<PaymentMethodForm>();
        private readonly ICartProvider cartProvider = DependencyResolver.Current.GetService<ICartProvider>();
        private readonly IContactDataProvider contactDataProvider = DependencyResolver.Current.GetService<IContactDataProvider>();
        private readonly IOrderPlacementService orderPlacementService = DependencyResolver.Current.GetService<IOrderPlacementService>();
        private readonly ICartPricingService cartPricingService = DependencyResolver.Current.GetService<ICartPricingService>();
        private readonly ITermsOfServiceProvider tosProvider = DependencyResolver.Current.GetService<ITermsOfServiceProvider>();

        [OrderFlowFilter]
        [HttpGet]
        public ActionResult Index()
        {
            // Make sure cart is properly calculated.
            var cart = cartProvider.GetCart();
            cartPricingService.CalculatePricing(cart);

            var model = DependencyResolver.Current.GetService<CheckoutViewModel>();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(CheckoutViewModel model)
        {
            var cart = cartProvider.GetCart();
            var contactDataCollection = contactDataProvider.GetContactData();

            if (cart.IsEmpty())
            {
                ModelState.AddModelError("cart", "Cart is empty");
            }
            else if (contactDataCollection == null)
            {
                ModelState.AddModelError("contactData", "Contact data is empty");
            }

            if (ModelState.IsValid)
            {
                // Recalculate cart one last time, to make sure e.g. setup fees are still there.
                cartPricingService.CalculatePricing(cart);

                var orderContext = new OrderContext(cart, contactDataCollection, model.SelectedPaymentMethod, new object[] { Request });
                var result = orderPlacementService.PlaceOrder(orderContext);

                return Redirect(result.RedirectUrl);
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult Success()
        {
            contactDataProvider.ClearContactData();
            cartProvider.ClearCart();

            return View();
        }

        [HttpGet]
        public ActionResult Failure()
        {
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>The parameters correspond to the standard parameters sent by Atomia payment HTTP handlers</remarks>
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Payment(string amount, string transactionReference, int transactionReferenceType, string status)
        {
            var action = "Failure";

            switch (status.ToUpper())
	        {
                case PaymentTransaction.Ok:
                case PaymentTransaction.InProgress:
                    action = "Success";
                    break;

                case PaymentTransaction.Failed:
		        default:
                    action = "Failure";
                    break;
	        }

            return RedirectToAction(action);
        }

        [HttpGet]
        public ActionResult TermsOfService(string id)
        {
            var tos = tosProvider.GetTermsOfService(id);

            if (tos == null)
            {
                return HttpNotFound();
            }

            var model = new TermsOfServiceViewModel
            {
                Id = tos.Id,
                Name = tos.Name,
                Terms = tos.Terms
            };

            return View(model);
        }
    }
}
