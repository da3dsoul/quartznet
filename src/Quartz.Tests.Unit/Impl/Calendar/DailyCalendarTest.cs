#region License

/*
 * All content copyright Marko Lahma, unless otherwise indicated. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy
 * of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 *
 */

#endregion

using FluentAssertions;

using NUnit.Framework;

using Quartz.Impl.Calendar;
using Quartz.Impl.Triggers;
using Quartz.Simpl;
using Quartz.Spi;
using Quartz.Util;

namespace Quartz.Tests.Unit.Impl.Calendar;

/// <summary>
/// Unit test for DailyCalendar.
/// </summary>
/// <author>Marko Lahma (.NET)</author>
[TestFixture(typeof(BinaryObjectSerializer))]
[TestFixture(typeof(JsonObjectSerializer))]
public class DailyCalendarTest : SerializationTestSupport<DailyCalendar, ICalendar>
{
    public DailyCalendarTest(Type serializerType) : base(serializerType)
    {
    }

    [Test]
    public void TestStringStartEndTimes()
    {
        DailyCalendar dailyCalendar = new DailyCalendar("1:20", "14:50");
        var toString = dailyCalendar.ToString();
        Assert.That(toString, Does.Contain("01:20:00:000 - 14:50:00:000"));

        dailyCalendar = new DailyCalendar("1:20:1:456", "14:50:15:2");
        toString = dailyCalendar.ToString();
        Assert.That(toString, Does.Contain("01:20:01:456 - 14:50:15:002"));
    }

    [Test]
    public void TestStartEndTimes()
    {
        // Grafit found a copy-paste problem from ending time, it was the same as starting time

        DateTime d = DateTime.Now;
        DailyCalendar dailyCalendar = new DailyCalendar("1:20", "14:50");
        DateTime expectedStartTime = new DateTime(d.Year, d.Month, d.Day, 1, 20, 0);
        DateTime expectedEndTime = new DateTime(d.Year, d.Month, d.Day, 14, 50, 0);

        Assert.AreEqual(expectedStartTime, dailyCalendar.GetTimeRangeStartingTimeUtc(d).DateTime);
        Assert.AreEqual(expectedEndTime, dailyCalendar.GetTimeRangeEndingTimeUtc(d).DateTime);
    }

    [Test]
    public void TestStringInvertTimeRange()
    {
        DailyCalendar dailyCalendar = new DailyCalendar("1:20", "14:50");
        dailyCalendar.InvertTimeRange = true;
        Assert.IsTrue(dailyCalendar.ToString().IndexOf("inverted: True") > 0);

        dailyCalendar.InvertTimeRange = false;
        Assert.IsTrue(dailyCalendar.ToString().IndexOf("inverted: False") > 0);
    }

    [Test]
    public void TestTimeZone()
    {
        TimeZoneInfo tz = TimeZoneUtil.FindTimeZoneById("Eastern Standard Time");

        DailyCalendar dailyCalendar = new DailyCalendar("12:00:00", "14:00:00");
        dailyCalendar.InvertTimeRange = true; //inclusive calendar
        dailyCalendar.TimeZone = tz;

        // 11/2/2012 17:00 (utc) is 11/2/2012 13:00 (est)
        DateTimeOffset timeToCheck = new DateTimeOffset(2012, 11, 2, 17, 0, 0, TimeSpan.FromHours(0));
        Assert.IsTrue(dailyCalendar.IsTimeIncluded(timeToCheck));
    }

    private static readonly object[] StartTimes = Enumerable.Range(0, 24)
        .SelectMany(a => Enumerable.Range(-12, 24).Select(b => new object[] {
            new DateTimeOffset(new DateTime(2024, 01, 02).AddHours(a),
                TimeSpan.FromHours(b)), TimeSpan.FromHours(b)
        }))
        .Cast<object>().ToArray();

    [TestCaseSource(nameof(StartTimes))]
    public void TestTimeZone2(DateTimeOffset startTime, TimeSpan timeZoneOffset)
    {
        DailyCalendar dailyCalendar = new DailyCalendar("00:00:00", "04:00:00");
        dailyCalendar.TimeZone = TimeZoneInfo.Utc;

        var trigger = (IOperableTrigger)TriggerBuilder
            .Create()
            .WithIdentity("TestTimeZone2Trigger")
            .StartAt(startTime)
            .WithSimpleSchedule(s => s
                .WithIntervalInMinutes(1)
                .RepeatForever())
            .Build();

        var fireTimes = TriggerUtils.ComputeFireTimes(trigger, dailyCalendar, (int)TimeSpan.FromDays(1).TotalMinutes);

        // Trigger need to fire during the period when the local timezone and utc timezone are on different day
        if (timeZoneOffset > TimeSpan.Zero)
        {
            // Trigger must fire between midnight and utc offset if positive offset.
            fireTimes.Where(t => t.Hour >= 0 && t.Hour <= timeZoneOffset.Hours).Should().NotBeEmpty();
        }
        else if (timeZoneOffset < TimeSpan.Zero)
        {
            // Trigger must fire between midnight minus utc offset and midnight if negative offset.
            fireTimes.Where(t => t.Hour >= 24 - timeZoneOffset.Hours && t.Hour <= 23).Should().NotBeEmpty();
        }
        else
        {
            // Trigger must not fire between midnight and utc offset if offset is UTC (zero)
            fireTimes.Where(t => t.Hour >= 0 && t.Hour <= timeZoneOffset.Hours).Should().BeEmpty();
        }
    }

    [Test]
    public void ShouldAllowExactMidnight()
    {
        var calendar = new DailyCalendar("01:00", "05:00");

        var trigger = (CronTriggerImpl) TriggerBuilder.Create()
            .WithIdentity("TestJobTrigger", "group1")
            .StartNow()
            .WithCronSchedule("0 0 0 * * ? *")
            .ModifiedByCalendar("CustomCalendar")
            .Build();

        var fireTimeUtc = trigger.ComputeFirstFireTimeUtc(calendar);
        fireTimeUtc.Should().NotBeNull();
    }

    protected override DailyCalendar GetTargetObject()
    {
        DailyCalendar c = new DailyCalendar("01:20:01:456", "14:50:15:002");
        c.Description = "description";
        c.InvertTimeRange = true;
        return c;
    }

    protected override void VerifyMatch(DailyCalendar original, DailyCalendar deserialized)
    {
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.Description, deserialized.Description);
        Assert.AreEqual(original.InvertTimeRange, deserialized.InvertTimeRange);
        Assert.AreEqual(original.TimeZone, deserialized.TimeZone);
        Assert.AreEqual(original.ToString(), deserialized.ToString());
    }
}