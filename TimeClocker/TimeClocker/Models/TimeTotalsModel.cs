using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace TimeClocker.Models
{
    public class TimeTotalsModel
    {
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd hh:mm tt}")]
        public DateTime InTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd hh:mm tt}")]
        public DateTime OutTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}")]
        public double TimeDelta { get; set; }        
    }
}