using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimeClocker.Models
{
    public class TimeTotals
    {
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd hh:mm tt}")]
        public DateTime InTime { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd hh:mm tt}")]
        public DateTime OutTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}")]
        public double RunningTotal { get; set; }
    }
}