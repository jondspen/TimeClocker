using System;
using System.ComponentModel.DataAnnotations;

namespace TimeClocker.Models
{
    public class ClockTimeModel
    {
        public Guid Id { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd hh:mm tt}")]
        public DateTime ClockTime { get; set; }
        public bool IsClockIn { get; set; }
    }
}