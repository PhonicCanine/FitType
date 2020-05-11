using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using TypeMagic;
using System.Text.Json;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = "Example.csv";
            using (var reader = new StreamReader(file))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                List<ExpandoObject> records = csv.GetRecords<dynamic>().Cast<ExpandoObject>().ToList();
                var races = records.Select(x => FitType.CoerceFitType<Race>(x)).ToList();
                string raceJson = JsonSerializer.Serialize<List<Race>>(races);
                Console.WriteLine(raceJson);
            }
        }
    }

    public class Race
    {
        [Prefix("Race Number")]
        public int RaceID { get; set; }
        [Prefix("lane*")]
        public List<Lane> lanes { get; set; }
    }

    public class Lane
    {
        public int Place { get; set; }
        public TimeSpan Time { get; set; }
        [Prefix("laptime*")]
        public List<TimeSpan> LapTimes { get; set; }
    }
}
