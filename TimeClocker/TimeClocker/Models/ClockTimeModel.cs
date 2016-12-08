using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeClocker.Models
{
    public class ClockTimeModel
    {
        public Guid Id { get; set; }
        public DateTime ClockTime { get; set; }
        public bool IsClockIn { get; set; }
    }
}