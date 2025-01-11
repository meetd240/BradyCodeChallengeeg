using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EnergyServiceCalculator
{
    [XmlRoot("GenerationOutput")]
    public class GenerationOutput
    {
        [XmlElement("Totals")]
        public Totals Totals { get; set; }

        [XmlElement("MaxEmissionGenerators")]
        public MaxEmissionGenerators MaxEmissionGenerators { get; set; }

        [XmlElement("ActualHeatRates")]
        public ActualHeatRates ActualHeatRates { get; set; }
    }

    public class Totals
    {
        [XmlElement("Generator")]
        public List<GeneratorTotal> GeneratorTotals { get; set; }
    }

    public class GeneratorTotal
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Total")]
        public double Total { get; set; }
    }

    public class MaxEmissionGenerators
    {
        [XmlElement("Day")]
        public List<MaxEmissionDay> Days { get; set; }
    }

    public class MaxEmissionDay
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Date")]
        public DateTime Date { get; set; }

        [XmlElement("Emission")]
        public double Emission { get; set; }
    }

    public class ActualHeatRates
    {
        [XmlElement("ActualHeatRate")]
        public List<ActualHeatRate> HeatRates { get; set; }
    }

    public class ActualHeatRate
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("HeatRate")]
        public double HeatRate { get; set; }
    }

}
