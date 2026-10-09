using ASI.Basecode.Data.Models;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IReservationRepository
    {
        IQueryable<Reservation> GetReservations();
        void AddReservation(Reservation model);
        void UpdateReservation(Reservation model);

        /// <summary>
        /// Starts a transaction that holds an exclusive lock on one borrower and item until it
        /// commits or is disposed, so overlapping submissions for that pair run one at a time.
        /// </summary>
        IDbContextTransaction BeginSubmissionLock(long borrowerProfileId, long equipmentId);
    }
}
