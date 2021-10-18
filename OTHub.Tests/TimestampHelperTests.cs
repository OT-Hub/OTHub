using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OTHub.Settings.Helpers;
using Xunit;

namespace OTHub.Tests
{
    public class TimestampHelperTests
    {
        [Fact]
        public void Timestamp_ValidDate()
        {
            DateTime date = TimestampHelper.UnixTimeStampToDateTime(1625319664);

            var expected = new DateTime(2021, 07, 03, 14, 41, 04, DateTimeKind.Local);

            Assert.Equal(expected, date);
        }
    }
}