﻿namespace Klinika.Users.Models
{
    public class Specialization
    {
        public int id { get; set; }
        public string name { get; set; }

        public override string ToString()
        {
            return name;
        }

        public Specialization()
        {
            id = -1;
            name = "";
        }
        public Specialization(int _id, string _name)
        {
            id = _id;
            name = _name;
        }
    }
}
