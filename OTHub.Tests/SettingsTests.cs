using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OTHub.Settings;
using Xunit;

namespace OTHub.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void Settings_ThrowExceptionIfNotSetup()
        {
            OTHubSettings settings = new OTHubSettings();

            Assert.Throws<Exception>(() => settings.Validate());
        }

        [Fact]
        public void Settings_ThrowExceptionIfNotFullySetup()
        {
            OTHubSettings settings = new OTHubSettings
            {
                MariaDB = new MariaDBSettings
                {
                    Database = "test", Server = "test", UserID = "test", Password = "test"
                }
            };

            Assert.Throws<Exception>(() => settings.Validate());
        }

        [Fact]
        public void Settings_NoExceptionThrownIfSetup()
        {
            OTHubSettings settings = new OTHubSettings
            {
                MariaDB = new MariaDBSettings
                {
                    Database = "test",
                    Server = "test",
                    UserID = "test",
                    Password = "test"
                },
                WebServer = new WebServerSettings
                {
                    AccessControlAllowOrigin = "test"
                }
            };
        }
    }
}