namespace Orleans.Clustering.Minio
{
    public class TableVersionData
    {
        public int Version { get; set; }
        public string VersionEtag { get; set; } = string.Empty;

    }
}
