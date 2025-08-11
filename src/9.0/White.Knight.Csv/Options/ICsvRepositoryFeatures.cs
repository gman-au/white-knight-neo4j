using White.Knight.Interfaces;

namespace White.Knight.Csv.Options
{
    public interface ICsvRepositoryFeatures<T> : IRepositoryFeatures
    {
        public ICsvLoader<T> CsvLoader { get; set; }
    }
}