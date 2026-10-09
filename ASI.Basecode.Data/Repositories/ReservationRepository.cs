using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;

namespace ASI.Basecode.Data.Repositories
{
    public class ReservationRepository : BaseRepository, IReservationRepository
    {
        private const int SubmissionLockTimeoutMs = 15000;

        public ReservationRepository(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        public IQueryable<Reservation> GetReservations()
        {
            return this.GetDbSet<Reservation>();
        }

        public void AddReservation(Reservation model)
        {
            this.GetDbSet<Reservation>().Add(model);
            UnitOfWork.SaveChanges();
        }

        public void UpdateReservation(Reservation model)
        {
            this.GetDbSet<Reservation>().Update(model);
            UnitOfWork.SaveChanges();
        }

        public IDbContextTransaction BeginSubmissionLock(long borrowerProfileId, long equipmentId)
        {
            var transaction = Context.Database.BeginTransaction();
            if (Context.Database.IsSqlServer())
            {
                // Same app-lock approach as UserAdministrationService. The lock is released
                // when the transaction ends, after the new row is committed, so the next
                // request for this borrower and item sees it in its duplicate check.
                var resource = $"Gearantee.ReservationSubmit:{borrowerProfileId}:{equipmentId}";
                Context.Database.ExecuteSqlInterpolated(
                    $@"DECLARE @lockResult int;
EXEC @lockResult = sys.sp_getapplock
    @Resource = {resource},
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = {SubmissionLockTimeoutMs};
IF @lockResult < 0 THROW 51000, 'Could not acquire the reservation submission lock.', 1;");
            }

            return transaction;
        }
    }
}
