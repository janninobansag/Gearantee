using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using System.Linq;

namespace ASI.Basecode.Data.Repositories
{
    public class ReservationRepository : BaseRepository, IReservationRepository
    {
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
    }
}
