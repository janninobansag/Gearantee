using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
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
            string categoryStatus = null,
            int categoryPage = 1)
        {
            if (!await _categoryService.CanManageAsync(UserId))
            {
                return Forbid();
            }

            var model = await _categoryService.GetEquipmentManagementAsync(
                UserId,
                tab,
                categorySearch,
                categoryStatus,
                categoryPage);

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

            if (Request.HasFormContentType && Request.Form.TryGetValue(nameof(model.IsActive), out var activeValues))
            {
                model.IsActive = activeValues.Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));
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

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            if (!await _categoryService.CanManageAsync(UserId))
            {
                return Forbid();
            }

            var category = await _categoryService.GetCategoryByIdAsync(UserId, id);
            if (category == null)
            {
                return NotFound();
            }

            var model = new EditCategoryViewModel
            {
                CategoryId = category.CategoryId,
                CategoryCode = category.CategoryCode,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive
            };

            ViewData["Title"] = "Edit category";
            ViewData["Eyebrow"] = "Master data";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCategoryViewModel model)
        {
            if (!await _categoryService.CanManageAsync(UserId))
            {
                return Forbid();
            }

            if (Request.HasFormContentType && Request.Form.TryGetValue(nameof(model.IsActive), out var activeValues))
            {
                model.IsActive = activeValues.Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit category";
                ViewData["Eyebrow"] = "Master data";
                return View(model);
            }

            var result = await _categoryService.UpdateCategoryAsync(UserId, model);
            if (result.Forbidden)
            {
                return Forbid();
            }

            if (!result.Succeeded)
            {
                if (result.Message == "Category not found.")
                {
                    return NotFound();
                }

                ModelState.AddModelError(string.Empty, result.Message);
                ViewData["Title"] = "Edit category";
                ViewData["Eyebrow"] = "Master data";
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
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
