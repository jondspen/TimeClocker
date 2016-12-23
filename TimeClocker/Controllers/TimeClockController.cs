using System;
using System.Web.Mvc;
using TimeClocker.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Web;
using System.Globalization;
using TimeClocker.Utilities;
using System.Linq;

namespace TimeClocker.Controllers
{
    public class TimeClockController : Controller
    {
        #region variables
        private string _filepath = HttpRuntime.AppDomainAppPath + @"App_Data\TimeClocker.JSON";
        #endregion

        #region ActionResults
        // GET: TimeClock
        public ActionResult ClockIn()
        {
            AddClockEvent(true);
            return RedirectToAction("ShowClockTimes");
        }

        public ActionResult ClockOut()
        {
            AddClockEvent(false);
            return RedirectToAction("ShowClockTimes");
        }

        public ActionResult ShowClockTimes()
        {
            List<ClockTimeModel> clockTimes = new List<ClockTimeModel>();

            if (System.IO.File.Exists(_filepath))
            {
                clockTimes = JsonConvert.DeserializeObject<List<ClockTimeModel>>(System.IO.File.ReadAllText(_filepath)) ?? new List<ClockTimeModel>();
            }

            return View(clockTimes);
        }

        [HttpGet]
        public ActionResult Edit(Guid id)
        {
            var clockTimes = ReadJsonClockTimes();
            ClockTimeModel editClockTime = null;

            foreach (var item in clockTimes)
            {
                if (item.Id == id)
                {
                    editClockTime = item;
                    break;
                }
            }

            if (editClockTime == null)
            {
                return RedirectToAction("ShowClockTimes", "TimeClock");
            }
            else
            {
                return View(editClockTime);
            }
        }

        [HttpPost]
        public ActionResult Edit (ClockTimeModel ctm)
        {
            var clockTimes = ReadJsonClockTimes();

            foreach (var item in clockTimes)
            {
                if (item.Id == ctm.Id)
                {
                    item.ClockTime = ctm.ClockTime;
                    item.IsClockIn = ctm.IsClockIn;
                    break;
                }
            }

            WriteClockTimesToFile(clockTimes);

            return RedirectToAction("ShowClockTimes", "TimeClock");
        }

        public ActionResult Delete(Guid id)
        {
            var clockTimes = ReadJsonClockTimes();

            foreach (var item in clockTimes)
            {
                if (item.Id == id)
                {
                    clockTimes.Remove(item);
                    break;
                }
            }

            WriteClockTimesToFile(clockTimes);

            return RedirectToAction("ShowClockTimes");

        }

        [HttpGet]
        public ActionResult Create()
        {
            ClockTimeModel ctm = new ClockTimeModel() { ClockTime = CurrentTimeUtilty.ComputeCurrentTimeFromUTC(), IsClockIn = false };
            return View(ctm);
        }

        [HttpPost]
        public ActionResult Create(ClockTimeModel ctm)
        {
            var times = ReadJsonClockTimes();
            ctm.Id = Guid.NewGuid();
            times.Add(ctm);
            WriteClockTimesToFile(times);

            return RedirectToAction("ShowClockTimes");
        }

        public ActionResult Hours(string span = "day")
        {
            List<ClockTimeModel> prunedTimeList = new List<ClockTimeModel>();

            prunedTimeList = PruneTimesFromList(ReadJsonClockTimes(), span);
            var timeTotals = ComputeTotalHours(prunedTimeList);
            var groupedTimes = from time in timeTotals
                             group time by time.InTime.Month into newGroupByMonth
                             from newGroupByDay in (from time in newGroupByMonth
                                                    orderby time.InTime.Day
                                                    group time by time.InTime.Day)
                             group newGroupByDay by newGroupByMonth.Key;

            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
            ViewBag.Span = myTI.ToTitleCase(span);

            return View(groupedTimes);
        }

        [HttpGet]
        public ActionResult WindowHours()
        {
            HoursForWindow myH4W = new HoursForWindow()
            {
                StartDate = DateTime.Now,
                DaySpan = 1,
                TimeTotalsList = new List<TimeTotalsModel>()
            };

            return View(myH4W);
        }

        [HttpPost]
        public ActionResult WindowHours(DateTime startDate, int daySpan)
        {
            HoursForWindow myH4W = new HoursForWindow()
            {
                DaySpan = daySpan,
                StartDate = startDate,
                TimeTotalsList = ComputeTotalHours(PruneTimesFromList(ReadJsonClockTimes(), startDate, daySpan))
            };

            return View(myH4W);
        }
        #endregion

        #region private methods
        private List<ClockTimeModel> ReadJsonClockTimes()
        {
            if (!System.IO.File.Exists(_filepath))
            {
                System.IO.File.Create(_filepath).Close();
            }

            var jsonData = System.IO.File.ReadAllText(_filepath);
            var clockTimes = JsonConvert.DeserializeObject<List<ClockTimeModel>>(jsonData) ?? new List<ClockTimeModel>();
            return clockTimes;
        }

        private void WriteClockTimesToFile(List<ClockTimeModel> clockTimes)
        {
            var jsonData = JsonConvert.SerializeObject(clockTimes);
            System.IO.File.WriteAllText(_filepath, jsonData);
        }

        private void AddClockEvent(bool isClockIn)
        {
            ClockTimeModel clockTime = new ClockTimeModel() {Id = Guid.NewGuid() , ClockTime = CurrentTimeUtilty.ComputeCurrentTimeFromUTC(), IsClockIn = isClockIn };

            if (!System.IO.File.Exists(_filepath))
            {
                System.IO.File.Create(_filepath).Close();
            }

            var clockTimes = ReadJsonClockTimes();
            clockTimes.Add(clockTime);

            WriteClockTimesToFile(clockTimes);
        }

        private List<TimeTotalsModel> ComputeTotalHours(List<ClockTimeModel> prunedTimeList)
        {
            List<TimeTotalsModel> timeList = new List<TimeTotalsModel>();
            TimeTotalsModel currentTimeTotals = new TimeTotalsModel();

            for (int i = 0; i < prunedTimeList.Count; i++)
            {
                if (prunedTimeList[i].IsClockIn)
                {
                    currentTimeTotals.InTime = prunedTimeList[i].ClockTime;

                    if ((i + 1) == prunedTimeList.Count)
                    {
                        // Clock in without a matching clock out.  Compute time on clock from clock in to now.
                        currentTimeTotals = new TimeTotalsModel() { InTime = currentTimeTotals.InTime, OutTime = CurrentTimeUtilty.ComputeCurrentTimeFromUTC(), TimeDelta = (CurrentTimeUtilty.ComputeCurrentTimeFromUTC()).Subtract(currentTimeTotals.InTime).TotalHours };
                        timeList.Add(currentTimeTotals);
                    }
                }
                else
                {
                    currentTimeTotals.OutTime = prunedTimeList[i].ClockTime;
                    // currentTimeTotals.RunningDailyTotal += (currentTimeTotals.OutTime.Subtract(currentTimeTotals.InTime)).TotalHours;
                    currentTimeTotals.TimeDelta = (currentTimeTotals.OutTime.Subtract(currentTimeTotals.InTime)).TotalHours;
                    timeList.Add(currentTimeTotals);

                    currentTimeTotals = new TimeTotalsModel();
                }
            }

            if (prunedTimeList.Count == 0)
            {
                timeList.Add(currentTimeTotals);
            }

            return timeList;
        }

        private List<ClockTimeModel> PruneTimesFromList(List<ClockTimeModel> inputList)
        {
            return PruneTimesFromList(inputList, "day");
        }

        private List<ClockTimeModel> PruneTimesFromList(List<ClockTimeModel> inputList, string period)
        {
            DateTime curDTG = CurrentTimeUtilty.ComputeCurrentTimeFromUTC();
            var prunedList = new List<ClockTimeModel>();

            switch (period.ToLower())
            {
                case "month":
                    ViewBag.PeriodID = CurrentTimeUtilty.ComputeCurrentTimeFromUTC().Month;
                    foreach (var item in inputList)
                    {
                        if (CurrentTimeUtilty.ComputeCurrentTimeFromUTC().Month == item.ClockTime.Month)
                        {
                            prunedList.Add(item);
                        }
                    }
                    break;

                case "week":
                    CultureInfo myCI = new CultureInfo("en-US");
                    Calendar myCal = myCI.Calendar;

                    int curWeek = myCal.GetWeekOfYear(curDTG, CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);
                    ViewBag.PeriodID = curWeek;

                    foreach (var item in inputList)
                    {
                        var checkWeek = myCal.GetWeekOfYear(item.ClockTime, CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);

                        if (curWeek == checkWeek)
                        {
                            prunedList.Add(item);
                        }
                    }
                    break;

                default:
                    foreach (var item in inputList)
                    {
                        if (curDTG.Date.CompareTo(item.ClockTime.Date) == 0)
                        {
                            prunedList.Add(item);
                            ViewBag.PeriodID = item.ClockTime.Day.ToString() + " - " + item.ClockTime.DayOfWeek.ToString();
                        }
                    }
                    break;
            }
            return prunedList;
        }

        private List<ClockTimeModel> PruneTimesFromList(List<ClockTimeModel> inputList, DateTime startDay, int daySpan)
        {
            DateTime curDTG = CurrentTimeUtilty.ComputeCurrentTimeFromUTC();
            var prunedList = new List<ClockTimeModel>();

            DateTime endDay = startDay.AddDays(daySpan);

            foreach (var item in inputList)
            {
                if ((item.ClockTime >= startDay) && (item.ClockTime < endDay))
                {
                    prunedList.Add(item);
                }
            }

            return prunedList;
        }
        #endregion
    }
}