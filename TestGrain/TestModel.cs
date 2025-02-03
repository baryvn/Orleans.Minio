using System.ComponentModel;
using Orleans.Persistence.Minio.Core;

namespace TestGrain
{

    [GenerateSerializer]
    public class TestModel
    {
        [Id(0), EsIndex]
        public string Column1 { get; set; } = "Column1";
        [Id(1), EsIndex]
        public string Column2 { get; set; } = "Column2";
        [Id(2), EsIndex]
        public string Column3 { get; set; } = "Column3";
        [Id(3), EsIndex]
        public string Column4 { get; set; } = "Column4";
        [Id(4), EsIndex]
        public string Column5 { get; set; } = "Column5";
        [Id(5), EsIndex]
        public string Column6 { get; set; } = "Column6";
        [Id(6), EsIndex]
        public string Column7 { get; set; } = "Column7";
        [Id(7), EsIndex]
        public string Column8 { get; set; } = "Column8";
    }
}
