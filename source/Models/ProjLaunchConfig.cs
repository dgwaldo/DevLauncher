namespace AMS.DevLauncher.Models {
    public class ProjLaunchConfig {
        public ProjLaunchConfig() { }
        public bool IsDotNetCore { get; set; }

        public string LaunchProfile { get; set; }
        public string Name { get; set; }
        public string SolutionPath { get; set; }
        //Path to .csproj of startup project
        public string ProjectPath { get; set; }
    }
}
