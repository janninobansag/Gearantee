using System;
using System.Collections.Generic;

namespace ASI.Basecode.WebApp.Models
{
    public class BorrowerDashboardViewModel
    {
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string UserCode { get; set; }
        public string SchoolId { get; set; }
        public string Department { get; set; }
        public string Greeting { get; set; }
        public bool IsEligible { get; set; }
        public string EligibilityMessage { get; set; }
        public int OnLoanCount { get; set; }
        public int PendingRequestCount { get; set; }
        public int ReadyToCollectCount { get; set; }
        public int OverdueCount { get; set; }
        public IList<BorrowerLoanViewModel> Loans { get; set; } = new List<BorrowerLoanViewModel>();
        public IList<BorrowerRequestViewModel> Requests { get; set; } = new List<BorrowerRequestViewModel>();
    }

    public class BorrowerLoanViewModel
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ImageUrl { get; set; }
        public string StatusLabel { get; set; }
        public DateTime DueAt { get; set; }
        public DateTime ReleasedAt { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class BorrowerRequestViewModel
    {
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public DateTime ReservationStart { get; set; }
        public DateTime ReservationEnd { get; set; }
        public string Purpose { get; set; }
        public string StatusLabel { get; set; }
        public string StatusClass { get; set; }
        public string Details { get; set; }
    }
}
