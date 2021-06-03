using AMS.DevLauncher.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AMS.DevLauncher
{

	class Program
	{
		static void Main(string[] args)
		{

			var configuration = GetConfigRoot();
			var projLaunchConfigs = configuration.GetSection("ProjConfigs").Get<List<ProjLaunchConfig>>();
			var launcherConfig = configuration.GetSection("DevEnv").Get<DevEnvConfig>();

			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine("First select projects to open in IDE, then supporting applications to run in the background \n");
			var debugProjects = GetUserConsoleInput("Select projects to open in development env.", projLaunchConfigs);
			var supportingProjects = GetUserConsoleInput("Select supporting projects you would like to run in the background. " +
														 "\nIf the project does not support .net core it will be opened in the development enviroment.",
														   projLaunchConfigs);

			LaunchProjects(launcherConfig, debugProjects, launchIDE: true);
			LaunchProjects(launcherConfig, supportingProjects);

		}

		private static void LaunchProjects(DevEnvConfig launcherConfig, IEnumerable<ProjLaunchConfig> runProjects, bool launchIDE = false)
		{

			if (!launchIDE && !launcherConfig.UseWindowsTerminal)
			{
				LaunchWithCmd(runProjects);
			}

			if (!launchIDE && launcherConfig.UseWindowsTerminal)
			{
				LaunchWithWt(runProjects);
			}
			
			if (launchIDE) { 
				foreach (var proj in runProjects)
				{
					LaunchDevEnvWithProject(proj.SolutionPath, launcherConfig);
				}
			}

		}

		private static void LaunchWithWt(IEnumerable<ProjLaunchConfig> runProjects)
		{
			var projectsToLaunch = runProjects.Where(p => p.IsDotNetCore).ToList();
			var args = projectsToLaunch.Select(p => $@"new-tab --title {p.Name} dotnet run --project {p.ProjectPath} --launch-profile {p.LaunchProfile}").ToList();
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "wt.exe",
					Arguments = $"{string.Join("; ", args)}",
					UseShellExecute = true
				}
			};
			process.Start();
		}

		private static void LaunchWithCmd(IEnumerable<ProjLaunchConfig> runProjects)
		{
			foreach (var proj in runProjects.Where(p => p.IsDotNetCore))
			{
				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "cmd.exe",
						Arguments = $@"/K dotnet run --project {proj.ProjectPath} --launch-profile {proj.LaunchProfile}",
						UseShellExecute = true
					}
				};
				process.Start();
			}
		}

		private static bool LaunchDevEnvWithProject(string solutionPath, DevEnvConfig devEnv)
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = devEnv.DevEnvPath,
					Arguments = $"{solutionPath} {devEnv.DevEnvParams}"
				}
			};
			return process.Start();
		}

		private static IEnumerable<ProjLaunchConfig> GetUserConsoleInput(string consoleMessage, List<ProjLaunchConfig> projects)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"{consoleMessage}".ToUpper());
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("-------------------------------------------------------------------------------");
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine($"Select projects by entering comma separated project index e.g.: 1,2,3\n");

			int input = projects.Count + 1;

			var indexedProjects = projects.Select((Project, Index) => new { Project, Index });
			string[] userSelections = null;

			while (input >= projects.Count && userSelections == null)
			{
				foreach (var proj in indexedProjects)
				{
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine($"\t{proj.Index}. {proj.Project.Name}");
				}
				Console.WriteLine($"\n\t{projects.Count}. SKIP");
				Console.WriteLine();

				var userInput = Console.ReadLine();
				userSelections = userInput.Split(",").ToArray();
			}

			return indexedProjects.Where(p => userSelections.Contains(p.Index.ToString())).Select(x => x.Project);
		}

		private static IConfigurationRoot GetConfigRoot()
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json")
				.AddJsonFile("appsettings.Production.json", optional: true);

			return builder.Build();
		}

	}
}
