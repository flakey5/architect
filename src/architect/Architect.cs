using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using HandlebarsDotNet;
using architect.template;

namespace architect
{
    internal class Architect
    {
        private static readonly string AssemblyDirectoy = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        private static readonly string TemplateDirectory = Path.Join(AssemblyDirectoy, "templates");

        public static void Run(string[] args)
        {
            if (!Directory.Exists(TemplateDirectory))
                Directory.CreateDirectory(TemplateDirectory);

            if (args.Length != 2)
            {
                PrintHelp();
                return;
            }

            switch (args[0].ToLower())
            {
                case "grab":
                    Clone(args[1]);
                    break;
                case "update":
                    Update(args[1]);
                    break;
                case "new":
                    New(args[1]);
                    break;
                default:
                    Console.WriteLine("Invalid option!");
                    PrintHelp();
                    break;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Help:");
            Console.WriteLine("  grab <Git repository>\tDownload a template");
            Console.WriteLine("  update <Template name>\tUpdate a template");
            Console.WriteLine("  new <Template name>\tCreate a new project using a template");
        }

        private static void Clone(string url)
        {
            Console.WriteLine($"Cloning repository {url}");

            Process process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C git clone {url} --recursive",
                    RedirectStandardOutput = false,
                    WorkingDirectory = TemplateDirectory
                }
            };
            process.Start();
            process.WaitForExit();

            Console.WriteLine("Finished!");
        }

        private static void Update(string template)
        {
            Console.WriteLine($"Updating template {template}");

            string directory = Path.Join(TemplateDirectory, template);
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Could not find template {template}!");
                return;
            }

            Process process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C git pull",
                    RedirectStandardOutput = false,
                    WorkingDirectory = directory
                }
            };
            process.Start();
            process.WaitForExit();

            Console.WriteLine("Finished!");
        }

        private static void New(string template)
        {
            Console.WriteLine($"Creating new {template}");

            string templateDirectory = Path.Join(TemplateDirectory, template);
            if (!Directory.Exists(templateDirectory))
            {
                Console.WriteLine($"Could not find template {template}!");
                return;
            }

            ArchitectManifest manifest = new ArchitectManifest();
            string manifestFile = Path.Join(templateDirectory, "architect-manifest.yml");
            if (File.Exists(manifestFile))
            {
                string manifestText = File.ReadAllText(manifestFile);

                YamlDotNet.Serialization.Deserializer deserializer = new YamlDotNet.Serialization.Deserializer();
                manifest = deserializer.Deserialize<ArchitectManifest>(manifestText);
            }

            if (!manifest.TemplateVariables.ContainsKey("name"))
                manifest.TemplateVariables.Add("name", new TemplateVariable { Description = "Name of the project" });

            Dictionary<string, string> filledVariables = new Dictionary<string, string>();
            foreach (var variable in manifest.TemplateVariables)
            {
                bool isOptional = variable.Value.DefaultValue != null;
                string value;
                do
                {
                    Console.WriteLine($"Please enter {variable.Key} ({variable.Value.Description}) " + (isOptional ? "(Optional)" : ""));
                    value = Console.ReadLine()!;
                } while (value.Length == 0 && !isOptional);

                // If we've exited the loop the only reason the value would be empty
                //  is if there's a default value
                if (value.Length == 0)
                    value = variable.Value.DefaultValue!;

                filledVariables.Add(variable.Key, value);
            }

            Dictionary<string, string> templateFiles = FindTemplateFiles(templateDirectory);

            string outputDirectory = Path.Join(Directory.GetCurrentDirectory(), filledVariables["name"]);
            if (Directory.Exists(outputDirectory))
            {
                Console.WriteLine($"Directory for {filledVariables["name"]} already exists!");
                return;
            }

            Directory.CreateDirectory(outputDirectory);

            foreach (KeyValuePair<string, string> templateFile in templateFiles)
            {
                string newPath = Path.Join(outputDirectory, templateFile.Key).Replace(templateDirectory + @"\", "").Replace(".template", "");

                Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);

                var contentTemplate = Handlebars.Compile(templateFile.Value);
                string renderedContent = contentTemplate(filledVariables);

                File.WriteAllText(newPath, renderedContent);
            }

            InitGitRepository(outputDirectory);

            if (manifest.PostCreationJobs != null)
            {
                manifest.PostCreationJobs.ForEach(job =>
                {
                    Console.WriteLine($"Running job {job.Name}");

                    Process process = new Process
                    {
                        StartInfo =
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/C {job.Command}",
                            RedirectStandardOutput = false,
                            WorkingDirectory = outputDirectory
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                });
            }

            Console.WriteLine("Finished!");
        }

        private static Dictionary<string, string> FindTemplateFiles(string directory)
        {
            Dictionary<string, string> files = new Dictionary<string, string>();

            foreach (string childDirectory in Directory.GetDirectories(directory))
            {
                if (childDirectory.EndsWith(".git"))
                    continue;

                FindTemplateFiles(childDirectory).ToList().ForEach(x => files.Add(x.Key, x.Value));
            }

            foreach (string file in Directory.GetFiles(directory))
            {
                if (!Path.GetExtension(file).Equals(".template"))
                    continue;

                string content = File.ReadAllText(file);
                files.Add(file, content);
            }

            return files;
        }

        private static void InitGitRepository(string directory)
        {
            Process process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    Arguments = "/C git init",
                    RedirectStandardOutput = false,
                    WorkingDirectory = directory
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }
}
