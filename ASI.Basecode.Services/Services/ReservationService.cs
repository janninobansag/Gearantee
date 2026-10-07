using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels.Reservations;
using ASI.Basecode.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace ASI.Basecode.Services.Services
{
    public class ReservationService : IReservationService
    {
        private const int PurposeMaxLength = 1000;

        private readonly IReservationRepository _reservationRepository;
        private readonly IEquipmentItemRepository _equipmentItemRepository;
        private readonly IBorrowerProfileRepository _borrowerProfileRepository;
        private readonly TimeProvider _timeProvider;

        public ReservationService(
            IReservationRepository reservationRepository,
            IEquipmentItemRepository equipmentItemRepository,
            IBorrowerProfileRepository borrowerProfileRepository,
            TimeProvider timeProvider)
        {
            _reservationRepository = reservationRepository;
            _equipmentItemRepository = equipmentItemRepository;
            _borrowerProfileRepository = borrowerProfileRepository;
            _timeProvider = timeProvider;
        }

        public string CheckBorrowerEligibility(string userId)
        {
            return EligibilityError(FindProfile(userId));
        }

        private static string EligibilityError(BorrowerProfileSummary profile)
        {
            if (profile == null)
            {
                return "Your account has no borrower profile yet. Ask an administrator to set one up before requesting equipment.";
            }

            if (!profile.IsActive)
            {
                return "Your account is deactivated, so you can't request equipment.";
            }

            if (!profile.IsEligible)
            {
                return "You're not currently eligible to borrow equipment. Ask an administrator to review your borrower profile.";
            }

            return null;
        }

        public ReservationSubmitResult SubmitReservation(ReservationCreateViewModel model, string userId)
        {
            if (model == null)
            {
                return ReservationSubmitResult.Fail("The reservation form is incomplete. Reload and try again.");
            }

            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            var profile = FindProfile(userId);
            var eligibilityError = EligibilityError(profile);
            if (eligibilityError != null)
            {
                return ReservationSubmitResult.Fail(eligibilityError);
            }

            var itemIsReservable = _equipmentItemRepository.GetEquipmentItems()
                .AsNoTracking()
                .Where(ReservationAvailability.IsReservable())
                .Any(item => item.EquipmentId == model.EquipmentId);
            if (!itemIsReservable)
            {
                return ReservationSubmitResult.Fail("This item can't be reserved right now. It may be under maintenance, unavailable, or archived.");
            }

            var windowError = ReservationAvailability.ValidateWindow(model.ReservationStart, model.ReservationEnd, nowUtc);
            if (windowError != null)
            {
                return ReservationSubmitResult.Fail(windowError);
            }

            var purpose = model.Purpose?.Trim();
            if (string.IsNullOrEmpty(purpose))
            {
                return ReservationSubmitResult.Fail("Tell the custodian what the equipment is for.");
            }

            if (purpose.Length > PurposeMaxLength)
            {
                return ReservationSubmitResult.Fail($"The purpose must be {PurposeMaxLength} characters or fewer.");
            }

            var startUtc = ManilaClock.ToUtc(model.ReservationStart.Value);
            var endUtc = ManilaClock.ToUtc(model.ReservationEnd.Value);

            var isBooked = _reservationRepository.GetReservations()
                .Where(reservation => reservation.EquipmentId == model.EquipmentId)
                .Any(ReservationAvailability.Blocks(startUtc, endUtc, nowUtc));
            if (isBooked)
            {
                return ReservationSubmitResult.Fail("This item is already booked or still on loan during that time. Pick a time outside the booked times.");
            }

            var profileId = profile.BorrowerProfileId;

            // D3: one open request per borrower, item, and window. A Pending request whose
            // start has passed is Expired (D2), so it no longer counts.
            var hasDuplicate = _reservationRepository.GetReservations()
                .Any(reservation =>
                    reservation.BorrowerProfileId == profileId &&
                    reservation.EquipmentId == model.EquipmentId &&
                    reservation.Status == DomainValues.ReservationStatuses.Pending &&
                    reservation.ReservationStart > nowUtc &&
                    reservation.ReservationStart < endUtc &&
                    reservation.ReservationEnd > startUtc);
            if (hasDuplicate)
            {
                return ReservationSubmitResult.Fail("You already have a pending request for this item that overlaps this time.");
            }

            var newReservation = new Reservation
            {
                BorrowerProfileId = profileId,
                EquipmentId = model.EquipmentId,
                ReservationStart = startUtc,
                ReservationEnd = endUtc,
                Purpose = purpose,
                Status = DomainValues.ReservationStatuses.Pending,
                RequestedAt = nowUtc,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };

            _reservationRepository.AddReservation(newReservation);

            return new ReservationSubmitResult
            {
                Succeeded = true,
                Message = "Reservation request submitted. A custodian will review it.",
                ReservationId = newReservation.ReservationId
            };
        }

        private BorrowerProfileSummary FindProfile(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return _borrowerProfileRepository.GetBorrowerProfiles()
                .AsNoTracking()
                .Where(profile => profile.UserId == userId)
                .Select(profile => new BorrowerProfileSummary
                {
                    BorrowerProfileId = profile.BorrowerProfileId,
                    IsEligible = profile.IsEligible,
                    IsActive = profile.User.IsActive
                })
                .FirstOrDefault();
        }

        private sealed class BorrowerProfileSummary
        {
            public long BorrowerProfileId { get; set; }
            public bool IsEligible { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
