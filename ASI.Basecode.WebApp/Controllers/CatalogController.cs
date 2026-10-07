using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels.Catalog;
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
    /// Borrower equipment catalog: search and view reservable items (WBS 34.00).
    /// </summary>
    [Authorize(Roles = DomainValues.Roles.Borrower)]
    [Authorize(Policy = DomainValues.Permissions.EquipmentBrowse)]
    public class CatalogController : ControllerBase<CatalogController>
    {
        private readonly ICatalogService _catalogService;

        /// <summary>
        /// Constructor
        /// </summary>
        public CatalogController(IHttpContextAccessor httpContextAccessor,
                                 ILoggerFactory loggerFactory,
                                 IConfiguration configuration,
                                 ICatalogService catalogService) : base(httpContextAccessor, loggerFactory, configuration)
        {
            _catalogService = catalogService;
        }

        // GET: /Catalog
        [HttpGet]
        public IActionResult Index([FromQuery] CatalogFilterViewModel filter)
        {
            var model = _catalogService.GetCatalog(filter);
            SetPageData("Equipment catalog");
            return View(model);
        }

        // GET: /Catalog/Details/5?from=...&to=...
        [HttpGet]
        public IActionResult Details(long id, DateTime? from, DateTime? to)
        {
            var item = _catalogService.RetrieveCatalogItem(id);
            if (item == null) return NotFound();

            SetPageData(item.ItemName);
            ViewData["From"] = from;
            ViewData["To"] = to;
            ViewData["SuccessMessage"] = TempData["SuccessMessage"];
            return View(item);
        }

        private void SetPageData(string title)
        {
            ViewData["Title"] = title;
            ViewData["Eyebrow"] = "Borrower workspace";
            ViewData["PageDate"] = ManilaClock.NowLocal.ToString("ddd d MMM yyyy");
        }
    }
}
