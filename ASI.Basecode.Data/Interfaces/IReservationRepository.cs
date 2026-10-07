using ASI.Basecode.Data.Models;
using System.Linq;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IReservationRepository
    {
        IQueryable<Reservation> GetReservations();
        void AddReservation(Reservation model);
        void UpdateReservation(Reservation model);
    }
}
