﻿namespace Klinika.MedicalRecords.Models
{
    public class Disease
    {
        public int id { get; set; }
        public int patientID { get; set; }
        public DateTime dateDiagnosed { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }

        public Disease() { }
    }
}
