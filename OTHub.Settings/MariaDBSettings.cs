using System;

namespace OTHub.Settings
{
    public class MariaDBSettings
    {
        public String Server { get; set; }
        public String Database { get; set; }
        public String UserID { get; set; }
        public String Password { get; set; }

        public string TempBountyKey { get; set; }

        public String ConnectionString
        {
            get { return $"Server={Server};User ID={UserID};Password={Password};Database={Database};Allow User Variables=True;"; }
        }

        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(Server))
            {
                throw new Exception("Missing Server for MariaDB");
            }

            if (String.IsNullOrWhiteSpace(Database))
            {
                throw new Exception("Missing Database for MariaDB");
            }

            if (String.IsNullOrWhiteSpace(UserID))
            {
                throw new Exception("Missing UserID for MariaDB");
            }

            if (String.IsNullOrWhiteSpace(Password))
            {
                throw new Exception("Missing Password for MariaDB");
            }
        }
    }
}