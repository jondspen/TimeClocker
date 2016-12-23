using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TimeClocker.Models
{
    public class HoursForWindow
    {
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime StartDate { get; set; }
        public int DaySpan { get; set; }

        public List<TimeTotalsModel> TimeTotalsList { get; set; }
    }
}