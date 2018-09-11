using System;
using System.IO;
using System.Linq;

namespace Alturos.Yolo
{
    public class ConfigurationDetector
    {
        string[] _files = null;
        public ConfigurationDetector()
        {

        }

        public ConfigurationDetector(string configFile, string weightsFile, string namesFile)
        {
            _files = new string[3];
            _files[0] = configFile;
            _files[1] = weightsFile;
            _files[2] = namesFile;
        }

        public ConfigurationDetector(YoloConfiguration config)
        {
            _files = new string[3];
            _files[0] = config.ConfigFile;
            _files[1] = config.WeightsFile;
            _files[2] = config.NamesFile;
        }

        public YoloConfiguration Detect()
        {
            if (_files == null)
            {
                _files = this.GetYoloFiles();
            }
            var yoloConfiguration = this.MapFiles(_files);
            var configValid = this.AreValidYoloFiles(yoloConfiguration);

            if (configValid)
            {
                return yoloConfiguration;
            }

            throw new FileNotFoundException("Cannot found pre-trained model, check all config files available (.cfg, .weights, .names)");
        }

        private string[] GetYoloFiles()
        {
            return Directory.GetFiles(".", "*.*", SearchOption.TopDirectoryOnly).Where(o => o.EndsWith(".names") || o.EndsWith(".cfg") || o.EndsWith(".weights")).ToArray();
        }

        private YoloConfiguration MapFiles(string[] files)
        {
            var configurationFile = files.Where(o => o.EndsWith(".cfg")).FirstOrDefault();
            var weightsFile = files.Where(o => o.EndsWith(".weights")).FirstOrDefault();
            var namesFile = files.Where(o => o.EndsWith(".names")).FirstOrDefault();

            return new YoloConfiguration(configurationFile, weightsFile, namesFile);
        }

        private bool AreValidYoloFiles(YoloConfiguration config)
        {
            if (string.IsNullOrEmpty(config.ConfigFile) ||
                string.IsNullOrEmpty(config.WeightsFile) ||
                string.IsNullOrEmpty(config.NamesFile))
            {
                return false;
            }

            /*if (Path.GetFileNameWithoutExtension(config.ConfigFile) != Path.GetFileNameWithoutExtension(config.WeightsFile))
            {
                return false;
            }*/

            return true;
        }
    }
}
