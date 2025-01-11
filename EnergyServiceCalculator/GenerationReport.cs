using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyServiceCalculator
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("GenerationReport")]
    public class GenerationReport
    {
        [XmlElement("Wind")]
        public Wind Wind { get; set; }

        [XmlElement("Gas")]
        public Gas Gas { get; set; }

        [XmlElement("Coal")]
        public Coal Coal { get; set; }
    }

    public class Wind
    {
        [XmlElement("WindGenerator")]
        public List<WindGenerator> WindGenerators { get; set; }
    }

    public class WindGenerator
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Generation")]
        public Generation Generation { get; set; }

        [XmlElement("Location")]
        public string Location { get; set; }
    }

    public class Generation
    {
        [XmlElement("Day")]
        public List<Day> Days { get; set; }
    }

    public class Day
    {
        [XmlElement("Date")]
        public DateTime Date { get; set; }

        [XmlElement("Energy")]
        public double Energy { get; set; }

        [XmlElement("Price")]
        public double Price { get; set; }
    }

    public class Gas
    {
        [XmlElement("GasGenerator")]
        public List<GasGenerator> GasGenerators { get; set; }
    }

    public class GasGenerator
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Generation")]
        public Generation Generation { get; set; }

        [XmlElement("EmissionsRating")]
        public double EmissionsRating { get; set; }
    }

    public class Coal
    {
        [XmlElement("CoalGenerator")]
        public List<CoalGenerator> CoalGenerators { get; set; }
    }

    public class CoalGenerator
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Generation")]
        public Generation Generation { get; set; }

        [XmlElement("TotalHeatInput")]
        public double TotalHeatInput { get; set; }

        [XmlElement("ActualNetGeneration")]
        public double ActualNetGeneration { get; set; }

        [XmlElement("EmissionsRating")]
        public double EmissionsRating { get; set; }
    }

}
