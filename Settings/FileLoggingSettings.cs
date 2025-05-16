namespace org.wakanda.framework.Settings
{

    public class FileLoggingSettings
    {
  
        public bool EnableFileLogging { get; set; } = false;

 
        public string? LogFilePath { get; set; } = "basequc-audit.log";
    }
}