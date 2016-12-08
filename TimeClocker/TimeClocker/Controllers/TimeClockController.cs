using System;
using System.Web.Mvc;
using TimeClocker.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Web;
using System.Globalization;

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

            return View(ctm);
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
            ClockTimeModel ctm = new ClockTimeModel() { ClockTime = DateTime.Now, IsClockIn = false };
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

        public ActionResult Hours(int span)
        {
            List<TimeTotals> timeTotals = new List<TimeTotals>();
            

            var clockTimes = ReadJsonClockTimes();
            List<ClockTimeModel> prunedTimeList = new List<ClockTimeModel>();
            var curDTG = DateTime.Now;
            CultureInfo myCI = new CultureInfo("en-US");
            Calendar myCal = myCI.Calendar;

            switch (span)
            {
                case 7:
                    // get values for the current week (not past 7 days)
                    var curWeek = myCal.GetWeekOfYear(curDTG, CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);

                    // prune the tree of times NOT in the current week
                    foreach (var item in clockTimes)
                    {
                        var checkWeek = myCal.GetWeekOfYear(item.ClockTime, CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);

                        if (curWeek == checkWeek)
                        {
                            prunedTimeList.Add(item);
                        }
                    }

                    timeTotals = ComputeTotalHours(prunedTimeList);

                    break;

                case 30:
                    // get values for the month (not last 30 days)
                    foreach (var item in clockTimes)
                    {
                        if (DateTime.Now.Month == item.ClockTime.Month)
                        {
                            prunedTimeList.Add(item);
                        }
                    }

                    timeTotals = ComputeTotalHours(prunedTimeList);
                    break;

                default:
                    // assuming only today's clock in/out
                    foreach (var item in clockTimes)
                    {
                        if (curDTG.Date.CompareTo(item.ClockTime.Date) == 0)
                        {
                            prunedTimeList.Add(item);
                        }
                    }

                    timeTotals = ComputeTotalHours(prunedTimeList);
                    break;
            }

            return View(timeTotals);
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

        private void AddClockEvent(bool IsClockIn)
        {
            ClockTimeModel clockTime = new ClockTimeModel() {Id = Guid.NewGuid() , ClockTime = DateTime.Now, IsClockIn = false };
            if (IsClockIn)
            {
                clockTime.IsClockIn = IsClockIn;
            }

            if (!System.IO.File.Exists(_filepath))
            {
                System.IO.File.Create(_filepath).Close();
            }

            var clockTimes = ReadJsonClockTimes();
            clockTimes.Add(clockTime);

            WriteClockTimesToFile(clockTimes);
        }

        private List<TimeTotals> ComputeTotalHours(List<ClockTimeModel> prunedTimeList)
        {
            List<TimeTotals> timeList = new List<TimeTotals>();
            TimeTotals currentTimeTotals = new TimeTotals();

            for (int i = 0; i < prunedTimeList.Count; i++)
            {
                if (prunedTimeList[i].IsClockIn)
                {
                    currentTimeTotals.InTime = prunedTimeList[i].ClockTime;

                    if ((i + 1) == prunedTimeList.Count)
                    {
                        // Clock in without a matching clock out.  Compute time on clock from clock in to now.
                        currentTimeTotals = new TimeTotals() { InTime = currentTimeTotals.InTime, OutTime = DateTime.Now.ToUniversalTime().AddHours(-6), RunningTotal = (DateTime.Now.ToUniversalTime().AddHours(-6).Subtract(currentTimeTotals.InTime)).TotalHours };
                        timeList.Add(currentTimeTotals);
                    }
                }
                else
                {
                    currentTimeTotals.OutTime = prunedTimeList[i].ClockTime;
                    // currentTimeTotals.RunningDailyTotal += (currentTimeTotals.OutTime.Subtract(currentTimeTotals.InTime)).TotalHours;
                    currentTimeTotals.RunningTotal = (currentTimeTotals.OutTime.Subtract(currentTimeTotals.InTime)).TotalHours;
                    timeList.Add(currentTimeTotals);

                    currentTimeTotals = new TimeTotals();
                }
            }

            return timeList;
        }
        #endregion
    }
}