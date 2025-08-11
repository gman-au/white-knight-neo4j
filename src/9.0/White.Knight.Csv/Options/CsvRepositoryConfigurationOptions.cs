using White.Knight.Abstractions.Options;

namespace White.Knight.Csv.Options
{
    public class CsvRepositoryConfigurationOptions : RepositoryConfigurationOptions
    {
        public string FolderPath { get; set; }
    }
}