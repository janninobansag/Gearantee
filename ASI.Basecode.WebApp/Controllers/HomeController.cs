using ASI.Basecode.Data;
using ASI.Basecode.Data.Models;
using ASI.Basecode.WebApp.Models;
using ASI.Basecode.WebApp.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASI.Basecode.WebApp.Controllers
{
    public class HomeController : ControllerBase<HomeController>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AsiBasecodeDBContext _dbContext;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            AsiBasecodeDBContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
            : base(
                httpContextAccessor,
                loggerFactory,
                configuration)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                return Challenge();
            }

            var borrowerProfile = await _dbContext.BorrowerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            var reservations = borrowerProfile == null
                ? new List<Reservation>()
                : await _dbContext.Reservations
                    .AsNoTracking()
                    .Include(x => x.EquipmentItem)
                    .Include(x => x.ReleaseRecord)
                        .ThenInclude(x => x.ReturnRecord)
                    .Where(x => x.BorrowerProfileId == borrowerProfile.BorrowerProfileId)
                    .OrderByDescending(x => x.UpdatedAt)
                    .ToListAsync();

            var now = DateTime.UtcNow;
            var activeLoans = reservations
                .Where(x => x.ReleaseRecord != null && x.ReleaseRecord.ReturnRecord == null)
                .ToList();
            var pendingRequests = reservations
                .Where(x => x.Status == DomainValues.ReservationStatuses.Pending)
                .ToList();
            var readyToCollect = reservations
                .Where(x => x.Status == DomainValues.ReservationStatuses.Approved && x.ReleaseRecord == null)
                .ToList();

            var model = new BorrowerDashboardViewModel
            {
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName)
                    ? user.UserCode
                    : user.DisplayName,
                FirstName = string.IsNullOrWhiteSpace(user.FirstName)
                    ? user.UserCode
                    : user.FirstName,
                UserCode = user.UserCode,
                SchoolId = borrowerProfile?.SchoolId ?? user.UserCode,
                Department = borrowerProfile?.Department,
                Greeting = GetGreeting(),
                IsEligible = borrowerProfile?.IsEligible == true,
                EligibilityMessage = borrowerProfile?.IsEligible == true
                    ? "No open damage reports"
                    : "Administrator review required",
                OnLoanCount = activeLoans.Count,
                PendingRequestCount = pendingRequests.Count,
                ReadyToCollectCount = readyToCollect.Count,
                OverdueCount = activeLoans.Count(x => x.ReservationEnd < now),
                Loans = activeLoans
                    .OrderBy(x => x.ReservationEnd)
                    .Take(4)
                    .Select(x => new BorrowerLoanViewModel
                    {
                        ItemCode = x.EquipmentItem?.ItemCode,
                        ItemName = x.EquipmentItem?.ItemName ?? "Equipment",
                        ImageUrl = x.EquipmentItem?.ImageUrl,
                        StatusLabel = x.ReservationEnd < now ? "Overdue" : "Borrowed",
                        DueAt = x.ReservationEnd,
                        ReleasedAt = x.ReleaseRecord.ActualReleaseAt,
                        IsOverdue = x.ReservationEnd < now
                    })
                    .ToList(),
                Requests = reservations
                    .Where(x => x.Status != DomainValues.ReservationStatuses.Cancelled &&
                                x.Status != DomainValues.ReservationStatuses.Expired)
                    .Take(3)
                    .Select(MapRequest)
                    .ToList()
            };

            ViewBag.DashboardView = true;
            return View(model);
        }

        private static BorrowerRequestViewModel MapRequest(Reservation reservation)
        {
            var status = reservation.Status ?? "Pending";
            var statusLabel = status == DomainValues.ReservationStatuses.Approved
                ? "Awaiting Release"
                : status;

            return new BorrowerRequestViewModel
            {
                ItemName = reservation.EquipmentItem?.ItemName ?? "Equipment request",
                ItemCode = reservation.EquipmentItem?.ItemCode,
                ReservationStart = reservation.ReservationStart,
                ReservationEnd = reservation.ReservationEnd,
                Purpose = reservation.Purpose,
                StatusLabel = statusLabel,
                StatusClass = status switch
                {
                    DomainValues.ReservationStatuses.Approved => "is-ready",
                    DomainValues.ReservationStatuses.Rejected => "is-rejected",
                    _ => "is-pending"
                },
                Details = status == DomainValues.ReservationStatuses.Rejected
                    ? reservation.RejectionReason ?? "Request rejected"
                    : status == DomainValues.ReservationStatuses.Approved
                        ? "Approved — collect at the equipment counter"
                        : "Awaiting custodian decision"
            };
        }

        private static string GetGreeting()
        {
            var hour = DateTime.Now.Hour;
            return hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
        }
    }
}
