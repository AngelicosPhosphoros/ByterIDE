using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace CompilerLib
{
    public class ByterProject
    {
        public bool ProjectChanged { get; protected set; }
        /// <summary>
        /// Filename of .bytpr file
        /// </summary>
        public string ProjectPath;
        public string MainMethod;
        public Dictionary<string, bool> FilesChanged { get; protected set; }
        public Dictionary<string, char[,]> Sources { get; protected set; } //Key is method name; 
        public Dictionary<string, string> FileNames { get; protected set; }
        public Dictionary<string, string> Comments { get; protected set; }

        public ByterProject()
        {
            FilesChanged = new Dictionary<string, bool>();
            Sources = new Dictionary<string, char[,]>();
            FileNames = new Dictionary<string, string>();
            Comments = new Dictionary<string, string>();
        }
        char[,] LoadSource(string filename, out string comment)
        {
            StreamReader sr = File.OpenText(filename);
            char[,] res = new char[16, 16];
            comment = string.Empty;
            for (int i = 0; i < 16; i++)
            {
                string s = sr.ReadLine();
                for (int j = 0; j < 16; j++) res[i, j] = s[j];
            }
            while (!sr.EndOfStream)
                comment += sr.ReadLine() + "\n";
            sr.Close();
            return res;
        }
        public void LoadFiles(string projectFileName)
        {
            StreamReader sr=null;
            ProjectPath = projectFileName;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(projectFileName));
            try
            {
                string pattern = @"\[\w+\|(\(|\)|\w|\.)[\w\\\.\(\)\s]+\]";// this is [methodName|path]
                //string pattern = @"\[\w+\|[\w\\\.]+\]";// this is [methodName|path]
                sr = File.OpenText(projectFileName);
                int moduleNumber = int.Parse(sr.ReadLine());
                if (moduleNumber <= 0)
                    throw new FileLoadException("Empty project can't be loaded.");
                Dictionary<string, char[,]> methods = new Dictionary<string, char[,]>(); // method names used as Keys;
                Dictionary<string, string> fileNames = new Dictionary<string, string>();
                Dictionary<string, string> comments = new Dictionary<string, string>();
                for (int i = 0; i < moduleNumber; i++)
                {
                    string s;
                    do
                    {
                        s = sr.ReadLine();
                        s = s.Trim(' ');
                    }
                    while (!Regex.IsMatch(s, pattern));
                    s = s.Trim('[', ']');
                    string[] str = s.Split('|');
                    string name;
                    if (str[0].ToUpper() != "MAIN")
                        name = str[0][0].ToString();// Названия всех методов, кроме главного, односимвольные
                    else
                    {
                        name = str[0];
                        this.MainMethod = name;
                    }
                    if (methods.Keys.Count(n => { return n == name; }) > 0 || str.Length > 2)
                        throw new Exception("Duplicate method Name: " + name);
                    string newComment;
                    char[,] source = LoadSource(Path.GetFullPath(str[1]), out newComment);
                    methods.Add(name, source);
                    fileNames.Add(name, Path.GetFullPath(str[1]));
                    comments.Add(name, newComment);
                }
                sr.Close();
                Sources = methods;
                FileNames = fileNames;
                Comments = comments;
                ProjectChanged = false;
                FilesChanged = new Dictionary<string, bool>();
                foreach (var a in FileNames) FilesChanged.Add(a.Key, false);
            }
            catch
            {
                if (sr != null)
                    sr.Close();
                throw new FileLoadException("Error in loading project.");
            }
        }
        public void SaveFile(string name)
        {
            if (!Sources.ContainsKey(name)) throw new ArgumentException("Unknown method name");
            if (!Directory.Exists(Path.GetDirectoryName(FileNames[name])))
                Directory.CreateDirectory(Path.GetDirectoryName(FileNames[name]));
            StreamWriter sw = File.CreateText(FileNames[name]);
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                    sw.Write(Sources[name][i, j]);
                sw.WriteLine();
            }
            sw.Write(Comments[name]);
            sw.Close();
            FilesChanged[name] = false;
        }
        public void SaveProjectFile()
        {
            if (!Directory.Exists(Path.GetDirectoryName(ProjectPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(ProjectPath));
            Directory.SetCurrentDirectory(Path.GetDirectoryName(ProjectPath));
            string mainPath = Path.GetDirectoryName(ProjectPath) + @"\";
            //Uri mainPath = new Uri(Path.GetDirectoryName(ProjectPath) + @"\");
            StreamWriter sw = File.CreateText(ProjectPath);
            sw.WriteLine(Sources.Count);
            List<string> keys = Sources.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                //Uri tmpUri = new Uri(FileNames[keys[i]]);
                //Uri relativeUri = mainPath.MakeRelativeUri(tmpUri);
                string tmp = Path.GetFullPath(FileNames[keys[i]]);
                string rel = tmp.Remove(0, mainPath.Length);
                //string rel = relativeUri.ToString().Replace("/",@"\"); //Uri works very strange
                sw.WriteLine("[{0}|{1}]", keys[i], rel);
            }
            sw.Close();
        }
        public void SaveAllChanges()
        {
            if (!ProjectChanged)
                return;
            SaveProjectFile();
            foreach (string key in Sources.Keys.ToList())
                if (FilesChanged[key])
                    SaveFile(key);
        }
        public void SaveAll()
        {
            SaveProjectFile();
            foreach (string key in Sources.Keys.ToList())
                SaveFile(key);
        }
        /// <summary>
        /// Changes source code of method with this name
        /// </summary>
        /// <param name="name">Name of the method</param>
        /// <param name="newSource">New source code of the method</param>
        public void ChangeSource(string name, char[,] newSource)
        {
            if (newSource.GetUpperBound(0) != 15 && newSource.GetUpperBound(1) != 15) throw new ArgumentException("Incorrect source size");
            if (!Sources.ContainsKey(name)) throw new ArgumentException("Unknown method name");
            FilesChanged[name] = true;
            ProjectChanged = true;
            Sources[name] = newSource;
        }
        public void ChangeFileName(string name, string newPath)
        {
            if (!Sources.ContainsKey(name)) throw new ArgumentException("Unknown method name");
            Directory.SetCurrentDirectory(ProjectPath);
            ProjectChanged = true;
            FilesChanged[name] = true;
            FileNames[name] = Path.GetFullPath(newPath);
        }
        public void RenameMethod(string name, string newName)
        {
            if (!Sources.ContainsKey(name)) throw new ArgumentException("Unknown method name");
            if (Sources.ContainsKey(newName)) throw new ArgumentException("Method "+newName+" already exists.");
            if (newName.Length != name.Length) throw new ArgumentException("New method name must have same number of symbols as old name");
            if (newName.Length != 1 && newName.Length != 4) throw new ArgumentException("Incorrect length of new method name");
            if (newName.Length == 1 && (!System.Text.RegularExpressions.Regex.IsMatch(newName,"[B-Za-z]") || newName == "A" || newName == "V")) throw new ArgumentException("Incorrect method name.\nMethod name must be 1 Latin character except 'A' or 'V'");
            ProjectChanged = true;
            Sources.Add(newName, Sources[name]);
            Sources.Remove(name);
            FileNames.Add(newName, FileNames[name]);
            FileNames.Remove(name);
            FilesChanged.Add(newName, FilesChanged[name]);
            FilesChanged.Remove(name);
            Comments.Add(newName, Comments[name]);
            Comments.Remove(name);
            if (name.Length == 1)
            {
                char old = name[0];
                char nw = newName[0];
                foreach (var a in Sources.Keys)
                {
                    for (int i = 0; i < 16; i++)
                        for (int j = 0; j < 16; j++)
                            if(Sources[a][i,j]==old)
                            {
                                Sources[a][i, j] = nw;
                            }
                    
                }
            }
        }
        /// <summary>
        /// Add new method with zero source code
        /// </summary>
        /// <param name="name">Name of new method</param>
        /// <param name="path">Path of file with source</param>
        public void AddMethod(string name, string path)
        {
            ProjectChanged = true;
            FileNames.Add(name, path);
            Comments.Add(name, "");
            char[,] source = new char[16, 16];
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    source[i, j] = '0';
            source[0, 0] = '#';// Empty method immediately ends running
            Sources.Add(name, source);
            FilesChanged.Add(name, true);
        }
        public void AddMethod(string name, string path, char[,] source)
        {
            AddMethod(name, path);
            Sources[name] = source;
        }

        public void RemoveMethod(string name)
        {
            ProjectChanged = true;
            FileNames.Remove(name);
            Sources.Remove(name);
            Comments.Remove(name);
            FilesChanged.Remove(name);
        }
        /// <summary>
        /// Create new project
        /// </summary>
        /// <param name="name">Name of project</param>
        /// <param name="folder">folder, where project will be saved</param>
        /// <returns></returns>
        public static ByterProject CreateProject(string name, string folder)
        {
            ByterProject res = new ByterProject();
            res.ProjectPath = folder + name;
            res.MainMethod = "Main";
            res.AddMethod(res.MainMethod, folder + res.MainMethod + ".byt");
            return res;
        }
    }
}
