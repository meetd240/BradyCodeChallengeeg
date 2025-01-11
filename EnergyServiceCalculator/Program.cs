using Microsoft.Extensions.Configuration;
using System.IO;
using System.Xml.Serialization;

namespace EnergyServiceCalculator
{
    internal class Program
    {
        private static readonly IConfiguration _configuration;
        private static readonly string _referenceDataPath;
        private static readonly string _inputFilePath;
        private static readonly string _outputFilePath;
        private static readonly ReferenceData _referenceData;

        static Program()
        {
            _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Set the base path to current directory
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

            _referenceDataPath = _configuration["ReferenceDataPath"] ?? throw new ArgumentNullException("Path of reference data is undefined");    
            _inputFilePath = _configuration["Input"] ?? throw new ArgumentNullException("Path of input file is undefined");
            _outputFilePath = _configuration["Output"] ?? throw new ArgumentNullException("Path of output file is undefined"); 

            if(string.IsNullOrWhiteSpace(_referenceDataPath) || string.IsNullOrWhiteSpace(_inputFilePath) || string.IsNullOrWhiteSpace(_outputFilePath))
            {
                throw new ArgumentException("Please specify path for reference data file, input file and output file in app settings");
            }

            _referenceData = DeserializeXml<ReferenceData>(_referenceDataPath);
        }
        static void Main(string[] args)
        {
            
            WatchInputDirectory();

        }

        public static void WatchInputDirectory()
        {
            var inputPath = _configuration["Input"];
            if (!Directory.Exists(inputPath))
            {
                throw new DirectoryNotFoundException("Input directory does not exist");
            }
            //Todo: check against null

            FileSystemWatcher watcher = new FileSystemWatcher
            {
                Path = inputPath,
                Filter = "*.xml", // Only watch for XML files
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite, // Trigger on new file or changes
                EnableRaisingEvents = true // Start watching
            };

            watcher.Created += OnNewFileCreated;

            Console.WriteLine($"Watching for new XML files in {inputPath}...");
            // Keep the application running to monitor files
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        static void OnNewFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"New file detected: {e.FullPath}");
            // Process the new file
            ProcessXmlFile(e.FullPath);
        }

        static void ProcessXmlFile(string filePath)
        {
            try
            {
               
                // Attempt to deserialize and validate the XML structure
                GenerationReport generationReport = DeserializeXml<GenerationReport>(filePath);

                if (generationReport != null)
                {
                    //Console.WriteLine($"Successfully deserialized the XML. Wind Generators Count: {report.Wind.WindGenerators.Count}");
                    var generationOutput = new GenerationOutput
                    {
                        Totals = CalculateTotals(generationReport, _referenceData),
                        MaxEmissionGenerators = CalculateMaxEmissions(generationReport),
                        ActualHeatRates = CalculateActualHeatRates(generationReport)
                    };
                    if (!Directory.Exists(_outputFilePath))
                    {
                        Directory.CreateDirectory(_outputFilePath);
                    }
                    string fileName = "GenerationOutput.xml";
                    string fullPath = Path.Combine(_outputFilePath, fileName);
                    SerializeToXml(generationOutput, fullPath);
                    // Further processing can be done here
                }
                else
                {
                    Console.WriteLine("XML structure is invalid, skipping file.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing XML file: {ex.Message}");
            }
        }

        static Totals CalculateTotals(GenerationReport report, ReferenceData referenceData)
        {
            List<GeneratorTotal> generatorTotals = new List<GeneratorTotal>();

            // For each generator (wind, gas, coal), calculate the total generation value
            foreach (var windGenerator in report.Wind.WindGenerators)
            {
                double total = windGenerator.Generation.Days.Sum(day =>
                    day.Energy * day.Price * GetValueFactorByLocation(windGenerator.Location, referenceData));
                generatorTotals.Add(new GeneratorTotal { Name = windGenerator.Name, Total = total });
            }

            foreach (var gasGenerator in report.Gas.GasGenerators)
            {
                double total = gasGenerator.Generation.Days.Sum(day =>
                    day.Energy * day.Price * referenceData.Factors.ValueFactor.Medium); // Gas has Medium ValueFactor
                generatorTotals.Add(new GeneratorTotal { Name = gasGenerator.Name, Total = total });
            }

            foreach (var coalGenerator in report.Coal.CoalGenerators)
            {
                double total = coalGenerator.Generation.Days.Sum(day =>
                    day.Energy * day.Price * referenceData.Factors.ValueFactor.Medium); // Coal has Medium ValueFactor
                generatorTotals.Add(new GeneratorTotal { Name = coalGenerator.Name, Total = total });
            }

            return new Totals { GeneratorTotals = generatorTotals };
        }

        static MaxEmissionGenerators CalculateMaxEmissions(GenerationReport report)
        {
            var maxEmissionDays = new List<MaxEmissionDay>();

            
            foreach (var gasGenerator in report.Gas.GasGenerators)
            {
                foreach (var day in gasGenerator.Generation.Days)
                {
                    double emissions = day.Energy * gasGenerator.EmissionsRating * _referenceData.Factors.EmissionsFactor.Medium; // Medium emission factor
                    maxEmissionDays.Add(new MaxEmissionDay
                    {
                        Name = gasGenerator.Name,
                        Date = day.Date,
                        Emission = emissions
                    });
                }
            }

            foreach (var coalGenerator in report.Coal.CoalGenerators)
            {
                foreach (var day in coalGenerator.Generation.Days)
                {
                    double emissions = day.Energy * coalGenerator.EmissionsRating * _referenceData.Factors.EmissionsFactor.High; // High emission factor
                    maxEmissionDays.Add(new MaxEmissionDay
                    {
                        Name = coalGenerator.Name,
                        Date = day.Date,
                        Emission = emissions
                    });
                }
            }

            return new MaxEmissionGenerators { Days = maxEmissionDays.GroupBy( m => m.Date).Select(group=> group.OrderByDescending(g=>g.Emission).FirstOrDefault())
                //.Take(1)
                .ToList() };
        }

        static ActualHeatRates CalculateActualHeatRates(GenerationReport report)
        {
            var heatRates = new List<ActualHeatRate>();

            foreach (var coalGenerator in report.Coal.CoalGenerators)
            {
                double heatRate = coalGenerator.TotalHeatInput / coalGenerator.ActualNetGeneration;
                heatRates.Add(new ActualHeatRate { Name = coalGenerator.Name, HeatRate = heatRate });
            }

            return new ActualHeatRates { HeatRates = heatRates };
        }

        static double GetValueFactorByLocation(string location, ReferenceData referenceData)
        {
            switch (location)
            {
                case "Offshore": return referenceData.Factors.ValueFactor.Low;
                case "Onshore": return referenceData.Factors.ValueFactor.High;
                default: return 0;
            }
        }

        static GenerationReport DeserializeXmlToReport(string xmlContent)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GenerationReport));
                using (StringReader reader = new StringReader(xmlContent))
                {
                    // Attempt deserialization
                    return (GenerationReport)serializer.Deserialize(reader);
                }
            }
            catch (InvalidOperationException)
            {
                // Catch deserialization errors and return null if the structure is invalid
                return null;
            }
        }

        static void SerializeToXml<T>(T data, string outputPath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                serializer.Serialize(writer, data);
            }
        }

        static T DeserializeXml<T>(string path) where T:class
        {
            if (!File.Exists(path)) {
                throw new FileNotFoundException("File not found");
            }
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    return (T)serializer.Deserialize(fs);
                }
            }
            catch (InvalidOperationException)
            {
                // Catch deserialization errors and return null if the structure is invalid
                return null;
            }
        }

    }
}
