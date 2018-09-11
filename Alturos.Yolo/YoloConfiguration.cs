using System.IO;

namespace Alturos.Yolo
{
    public class YoloConfiguration
    {
        public string ConfigFile { get; set; }
        public string WeightsFile { get; set; }
        public string NamesFile { get; set; }

        public YoloConfiguration(string configFile, string weightsFile, string namesFile)
        {
            this.ConfigFile = configFile;
            this.WeightsFile = weightsFile;
            this.NamesFile = namesFile;
        }

        public bool IsValid => File.Exists(ConfigFile) && File.Exists(WeightsFile) && File.Exists(NamesFile);
    }
}
