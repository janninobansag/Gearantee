using ASI.Basecode.Services.ServiceModels.Reservations;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IReservationService
    {
        /// <summary>Returns null when the user may request equipment, otherwise the reason they can't.</summary>
        string CheckBorrowerEligibility(string userId);

        ReservationSubmitResult SubmitReservation(ReservationCreateViewModel model, string userId);
    }
}
