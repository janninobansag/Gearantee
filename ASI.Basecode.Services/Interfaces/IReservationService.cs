using ASI.Basecode.Services.ServiceModels.Reservations;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IReservationService
    {
        /// <summary>Returns null when the user may request equipment, otherwise the reason they can't.</summary>
        string CheckBorrowerEligibility(string userId);

        ReservationSubmitResult SubmitReservation(ReservationCreateViewModel model, string userId);

        /// <summary>The caller's own reservations, newest first. Status is a stored status, or empty for all.</summary>
        ReservationIndexViewModel GetMyReservations(string userId, string status, int page);

        /// <summary>Returns null when the reservation doesn't exist or belongs to someone else.</summary>
        ReservationDetailsViewModel RetrieveMyReservation(long reservationId, string userId);
    }
}
