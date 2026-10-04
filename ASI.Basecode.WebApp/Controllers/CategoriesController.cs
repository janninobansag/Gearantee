using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(
        Roles = DomainValues.Roles.Administrator,
        Policy = DomainValues.Permissions.EquipmentManage)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<IActionResult> Index(
            string tab = "equipment",
            string categorySearch = null,
            string categoryStatus = null)
        {
            if (!await _categoryService.CanManageAsync(UserId))
            {
                return Forbid();
            }

            var model = await _categoryService.GetEquipmentManagementAsync(
                UserId,
                tab,
                categorySearch,
                categoryStatus);

            if (model == null)
            {
                return Forbid();
            }

            ViewData["Title"] = "Equipments & categories";
            ViewData["Eyebrow"] = "Master data";
            ViewData["PageDate"] = DateTime.Now.ToString("MMM d, yyyy");
            ViewData["SuccessMessage"] = TempData["SuccessMessage"];
            ViewData["ErrorMessage"] = TempData["ErrorMessage"];

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await _categoryService.CanManageAsync(UserId))
            {
                return Forbid();
            }

            ViewData["Title"] = "Create category";
            ViewData["Eyebrow"] = "Master data";
            return View(new CreateCategoryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCategoryViewModel model)
        {
            if (!await _categoryService.CanManageAsync(UserId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Create category";
                ViewData["Eyebrow"] = "Master data";
                return View(model);
            }

            var result = await _categoryService.CreateCategoryAsync(UserId, model);
            if (result.Forbidden)
            {
                return Forbid();
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewData["Title"] = "Create category";
                ViewData["Eyebrow"] = "Master data";
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCategoryViewModel model)
        {
            if (!await _categoryService.CanManageAsync(UserId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed: please provide a valid category code and name.";
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            var result = await _categoryService.UpdateCategoryAsync(UserId, model);
            if (result.Forbidden)
            {
                return Forbid();
            }

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(SetCategoryActiveModel model)
        {
            if (!await _categoryService.CanManageAsync(UserId))
            {
                return Forbid();
            }

            var result = await _categoryService.SetCategoryActiveAsync(UserId, model.CategoryId, model.IsActive);
            if (result.Forbidden)
            {
                return Forbid();
            }

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }
    }
}
