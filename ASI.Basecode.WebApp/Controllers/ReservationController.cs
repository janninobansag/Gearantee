using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels.Reservations;
using ASI.Basecode.Services.Utilities;
using ASI.Basecode.WebApp.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace ASI.Basecode.WebApp.Controllers
{
    /// <summary>
    /// Borrower reservations: submit a request (WBS 35.00).
    /// </summary>
    [Authorize(Roles = DomainValues.Roles.Borrower)]
    [Authorize(Policy = DomainValues.Permissions.ReservationCreate)]
    public class ReservationController : ControllerBase<ReservationController>
    {
        private readonly IReservationService _reservationService;
        private readonly ICatalogService _catalogService;

        /// <summary>
        /// Constructor
        /// </summary>
        public ReservationController(IHttpContextAccessor httpContextAccessor,
                                     ILoggerFactory loggerFactory,
                                     IConfiguration configuration,
                                     IReservationService reservationService,
                                     ICatalogService catalogService) : base(httpContextAccessor, loggerFactory, configuration)
        {
            _reservationService = reservationService;
            _catalogService = catalogService;
        }

        // GET: /Reservation/Create?equipmentId=5&from=...&to=...
        [HttpGet]
        public IActionResult Create(long equipmentId, DateTime? from, DateTime? to)
        {
            var item = _catalogService.RetrieveCatalogItem(equipmentId);
            if (item == null) return NotFound();

            var model = new ReservationCreateViewModel
            {
                EquipmentId = equipmentId,
                ReservationStart = from,
                ReservationEnd = to,
                Item = item
            };

            SetPageData();
            return View(model);
        }

        // POST: /Reservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ReservationCreateViewModel model)
        {
            model.Item = _catalogService.RetrieveCatalogItem(model.EquipmentId);
            if (model.Item == null) return NotFound();

            if (!ModelState.IsValid)
            {
                SetPageData();
                return View(model);
            }

            var result = _reservationService.SubmitReservation(model, UserId);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                SetPageData();
                return View(model);
            }

            // WBS 36 will send the borrower to My Reservations instead.
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Details", "Catalog", new { id = model.EquipmentId });
        }

        private void SetPageData()
        {
            ViewData["Title"] = "Request a reservation";
            ViewData["Eyebrow"] = "Borrower workspace";
            ViewData["PageDate"] = ManilaClock.NowLocal.ToString("ddd d MMM yyyy");
            ViewData["EligibilityError"] = _reservationService.CheckBorrowerEligibility(UserId);
        }
    }
}
