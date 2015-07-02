using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing.Text;
using System.IO;
using CompilerLib;

namespace ByterIDE
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Creates tab with greetings
        /// </summary>
        private void InitGreetingTab()
        {
            RichTextBox greetingText = new RichTextBox();
            greetingText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            greetingText.Location = new System.Drawing.Point(6, 6);
            greetingText.Name = "greetingText";
            greetingText.Size = new System.Drawing.Size(841, 311);
            greetingText.TabIndex = 2;
            greetingText.Text = "Welcome to Byter!\nThis is a developer tool to create programs on Byter.\nIt uses " +
    "extended specification of language.";
            greetingText.Font = new Font(greetingText.Font.Name, 25);
            greetingText.ReadOnly = true;
            TabPage greetingTab = new TabPage();
            greetingTab.Controls.Add(greetingText);
            greetingTab.Location = new System.Drawing.Point(4, 22);
            greetingTab.Name = "greetingTab";
            greetingTab.Padding = new System.Windows.Forms.Padding(3);
            greetingTab.Size = new System.Drawing.Size(853, 323);
            greetingTab.TabIndex = 0;
            greetingTab.Text = "Welcome";
            greetingTab.UseVisualStyleBackColor = true;
            while (codeTabControl.Controls.Count != 0)
                codeTabControl.Controls.Remove(codeTabControl.Controls[0]);
            codeTabControl.Controls.Add(greetingTab);
        }
        /// <summary>
        /// Close tab when user press on close button in tabs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void codeTabControl_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                System.Drawing.Font font = codeTabControl.Font;
                int margin = 3;
                for (int i = 0; i < codeTabControl.TabCount; i++)
                {
                    Rectangle tabRect = codeTabControl.GetTabRect(i);
                    RectangleF btnRect = new RectangleF(tabRect.Right - margin - tabCloseButtonSize.Width, tabRect.Top + margin, tabCloseButtonSize.Width, tabCloseButtonSize.Height);
                    if (btnRect.Contains(e.Location))
                    {
                        if (OpenedProject != null && OpenedProject.FilesChanged.ContainsKey(codeTabControl.TabPages[i].Name) && OpenedProject.FilesChanged[codeTabControl.TabPages[i].Name])
                            switch (MessageBox.Show(this, "Save changes in module " + codeTabControl.TabPages[i].Name + '?', "Confirm", MessageBoxButtons.YesNoCancel))
                            {
                                case DialogResult.Yes: OpenedProject.SaveFile(codeTabControl.TabPages[i].Name); break;
                                case DialogResult.Cancel: return;
                            }
                        codeTabControl.TabPages.RemoveAt(i);
                        return;
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        protected SizeF tabCloseButtonSize = new Size(0, 0);
        /// <summary>
        /// Draws close cross on tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void codeTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                System.Drawing.Font font = codeTabControl.Font;
                int margin = 1;
                Graphics g = e.Graphics;
                int top = e.Bounds.Top + margin;
                int left = e.Bounds.Left + margin;
                g.DrawString(codeTabControl.TabPages[e.Index].Text, font, Brushes.Black, left, top);
                tabCloseButtonSize = g.MeasureString("x", font);
                left = e.Bounds.Right - (int)(tabCloseButtonSize.Width) - margin;
                g.DrawString("x", font, Brushes.Red, left, top);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                this.InitGreetingTab();
                this.WindowState = FormWindowState.Maximized;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        /// <summary>
        /// This is for highlight text
        /// </summary>
        Tuple<List<char[]>, Color[]> SyntaxColor =
            new Tuple<List<char[]>, Color[]>(
            new List<char[]>
            (new char[][]{
                new char[]{ '>', '<', 'V', 'A' },
                new char[]{ '#' },
                new char[]{ '+', '-', '}','{'},
                new char[] {'$'}, new char[]{'0'}}
            ),
            new Color[] { Color.Blue, Color.Red, Color.Green, Color.DarkViolet, Color.Black });
        char[] StandartOperators = { '>', '<', 'V', 'A', '#', '+', '-', '}', '{', '$', '0' };
        ByterProject _openedProject;
        ByterProject OpenedProject
        {
            get { return _openedProject; }
            set
            {
                contextMenuStrip.Enabled= closeProjectToolStripMenuItem.Enabled = closeToolStripMenuItem.Enabled = projectToolStripMenuItem.Enabled = debugToolStripMenuItem.Enabled =
                    buildToolStripMenuItem.Enabled = saveAllToolStripMenuItem.Enabled = saveAsToolStripMenuItem.Enabled = saveToolStripMenuItem.Enabled = value != null;
                projectExplorerBox.Items.Clear();
                _openedProject = value;
            }
        }
        // Error's, which occures during compilation
        CompileErrors CompilationErrors;
        /// <summary>
        /// Debugging process
        /// </summary>
        private Process _dbgProcess;
        Process DbgProcess
        {
            get { return _dbgProcess; }
            set
            {
                debugToolStripMenuItem.Text = value == null ? "Debug" : "Stop debug";
                _dbgProcess = value;
            }
        }
        /// <summary>
        /// Creates new tab in codeTable with code
        /// </summary>
        /// <param name="methodName">name of method</param>
        /// <returns></returns>
        private TabPage InitNewTab(string methodName)
        {
            try
            {
                SplitContainer horizontalSplit = new SplitContainer();
                SplitContainer verticalSplit = new SplitContainer();
                DataGridView dataGridView = new DataGridView();
                GroupBox commentGB = new GroupBox();
                TextBox textBox = new TextBox();
                RichTextBox commentBox = new RichTextBox();
                TabPage newTP = new TabPage();
                horizontalSplit.Dock = System.Windows.Forms.DockStyle.Fill;
                horizontalSplit.Name = "horizontalSplit" + methodName;
                horizontalSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
                horizontalSplit.Panel1.Controls.Add(verticalSplit);
                horizontalSplit.Panel2.Controls.Add(commentGB);
                horizontalSplit.SplitterDistance = 220;
                horizontalSplit.TabIndex = 0;
                commentBox.Dock = System.Windows.Forms.DockStyle.Fill;
                commentBox.Name = "CommentBox" + methodName;
                commentBox.LostFocus += commentBox_LostFocus;
                commentGB.Dock = System.Windows.Forms.DockStyle.Fill;
                commentGB.Name = "comment" + methodName;
                commentGB.TabIndex = 0;
                commentGB.TabStop = false;
                commentGB.Text = "Comment";
                commentGB.Controls.Add(commentBox);
                verticalSplit.Name = "verticalSplit" + methodName;
                verticalSplit.Panel1.Controls.Add(dataGridView);
                verticalSplit.Panel2.Controls.Add(textBox);
                verticalSplit.TabIndex = 0;
                verticalSplit.Dock = System.Windows.Forms.DockStyle.Fill;
                dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
                dataGridView.Name = "codeGridView" + methodName;
                dataGridView.TabIndex = 0;
                dataGridView.AllowUserToAddRows = false;
                dataGridView.EditMode = DataGridViewEditMode.EditOnEnter;
                dataGridView.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
                dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
                dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.DarkBlue;
                dataGridView.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
                dataGridView.RowHeadersDefaultCellStyle = dataGridView.ColumnHeadersDefaultCellStyle;
                dataGridView.RowCount = 16;
                dataGridView.ColumnCount = 16;
                dataGridView.RowHeadersWidth = 50;
                for (int i = 0; i < 16; i++)
                {
                    dataGridView.Columns[i].HeaderCell.Value = i.ToString("x");
                    dataGridView.Rows[i].HeaderCell.Value = i.ToString("x");
                    dataGridView.Columns[i].Width = dataGridView.Rows[0].Height;
                }
                dataGridView.CellEndEdit += dataGridView_CellEndEdit;
                textBox.Dock = System.Windows.Forms.DockStyle.Fill;
                textBox.Multiline = true;
                textBox.Name = "codeTextBox" + methodName;
                textBox.TabIndex = 0;
                textBox.Font = new System.Drawing.Font("Lucida Console", 12, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
                textBox.LostFocus += codeTextBox_Leave;
                newTP.Controls.Add(horizontalSplit);
                newTP.Name = methodName;
                newTP.Padding = new System.Windows.Forms.Padding(3);
                newTP.TabIndex = codeTabControl.TabPages.Count;
                newTP.Text = "Module " + methodName + "  ";
                newTP.UseVisualStyleBackColor = true;
                codeTabControl.TabPages.Add(newTP);
                verticalSplit.SplitterDistance = codeTabControl.Width / 2;
                horizontalSplit.SplitterDistance = codeTabControl.Height * 2 / 3;
                return newTP;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
                return null;
            }
        }
        /// <summary>
        /// Saves changes in comments
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void commentBox_LostFocus(object sender, EventArgs e)
        {
            try
            {
                if (sender is RichTextBox && OpenedProject != null)
                {
                    string methodName = (sender as Control).Name.Substring("CommentBox".Length);
                    OpenedProject.Comments[methodName] = (sender as Control).Text;
                    OpenedProject.FilesChanged[methodName] = true;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        private void AddTab(string methodName)
        {
            TabPage tp = InitNewTab(methodName);
            LoadCodeToTab(tp);
            LoadComment(tp);
        }

        private void LoadCodeToTab(TabPage tab)
        {
            string methodName = tab.Name;
            var controls = tab.Controls;
            DataGridView dg = (controls.Find("codeGridView" + methodName, true)[0] as DataGridView);
            string[] str = new string[16];
            for (int i = 0; i < 16; i++)
            {
                str[i] = string.Empty;
                for (int j = 0; j < 16; j++)
                {
                    char val = OpenedProject.Sources[methodName][i, j];
                    dg.Rows[i].Cells[j].Value = val;
                    // this is need to create syntax highlighting
                    int index = SyntaxColor.Item1.FindIndex(n => { return n.Contains(val); });
                    dg.Rows[i].Cells[j].Style.ForeColor = index >= 0 ? SyntaxColor.Item2[index] : Color.Brown;
                    if (val != '0' && StandartOperators.Contains(val))
                        dg.Rows[i].Cells[j].Style.Font = new Font(dg.DefaultCellStyle.Font, FontStyle.Bold);
                    else dg.Rows[i].Cells[j].Style.Font = dg.DefaultCellStyle.Font;
                    str[i] += val;
                }
            }
            (controls.Find("codeTextBox" + methodName, true)[0] as TextBox).Lines = str;
        }
        private void LoadComment(TabPage tab)
        {
            (tab.Controls.Find("CommentBox" + tab.Name, true)[0]).Text = OpenedProject.Comments[tab.Name];
        }
        private void OpenTab(string name)
        {
            if (name == null || name == "")
                return;
            if (name.Length > 1)
                name = OpenedProject.MainMethod;
            if (!codeTabControl.TabPages.ContainsKey(name))
                AddTab(name);
            codeTabControl.SelectedIndex = codeTabControl.TabPages.IndexOfKey(name);
        }
        private void projectExplorerBox_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (projectExplorerBox.SelectedItem == null || OpenedProject == null) return;
                string methodName = projectExplorerBox.SelectedItem.ToString().Remove(0, "Module ".Length);
                OpenTab(methodName);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void openProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog(this) == DialogResult.OK && Path.GetExtension(openFileDialog1.FileName) == ".bytpr")
                {
                    Directory.SetCurrentDirectory(Path.GetDirectoryName(openFileDialog1.FileName));
                    ByterProject newProject = new ByterProject();
                    newProject.LoadFiles(openFileDialog1.FileName);
                    OpenedProject = newProject;
                    projectExplorerBox.Items.Clear();
                    codeTabControl.TabPages.Clear();
                    foreach (var k in OpenedProject.FileNames)
                        projectExplorerBox.Items.Add("Module " + k.Key);

                }
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }
        }
        private void closeProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (OpenedProject.ProjectChanged || OpenedProject.FilesChanged.ContainsValue(true))
                    switch (MessageBox.Show(this, "Save changes in project?", "Confirm", MessageBoxButtons.YesNoCancel))
                    {
                        case DialogResult.Yes: OpenedProject.SaveAll(); break;
                        case DialogResult.Cancel: return;
                    }
                codeTabControl.TabPages.Clear();
                OpenedProject = null;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (!(sender is DataGridView) || OpenedProject == null) return;
                DataGridView dg = sender as DataGridView;
                char[,] source = new char[16, 16];
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 16; j++)
                        source[i, j] = (dg.Rows[i].Cells[j].Value!=null)?dg.Rows[i].Cells[j].Value.ToString()[0]:'0';
                string methodName = dg.Name.Remove(0, "codeGridView".Length);
                OpenedProject.ChangeSource(methodName, source);
                this.LoadCodeToTab(codeTabControl.TabPages[codeTabControl.TabPages.IndexOfKey(methodName)]);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        private void codeTextBox_Leave(object sender, EventArgs e)
        {
            try
            {
                if (!(sender is TextBox) || OpenedProject == null)
                    return;
                TextBox tb = sender as TextBox;
                bool IsValid = tb.Lines.Length == 16;
                if (IsValid)
                    for (int i = 0; i < 16; i++)
                        if (tb.Lines[i].Length != 16)
                        {
                            IsValid = false;
                            break;
                        }
                if (!IsValid)
                {
                    MessageBox.Show("Table of code must be 16x16");
                    tb.Focus();
                    return;
                }
                char[,] source = new char[16, 16];
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 16; j++)
                        source[i, j] = tb.Lines[i][j];
                string methodName = tb.Name.Remove(0, "codeTextBox".Length);
                OpenedProject.ChangeSource(methodName, source);
                this.LoadCodeToTab(codeTabControl.TabPages[codeTabControl.TabPages.IndexOfKey(methodName)]);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void newProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string newProjectName = "";
                bool CreateNewFolder = true;
                if (InputBox.ShowDialog("Введите название проекта", "Название проекта", "Создать новую папку", ref newProjectName, ref CreateNewFolder) == DialogResult.OK &&
                    folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(newProjectName, @"\w+"))
                    {
                        MessageBox.Show("Incorrect project name!");
                        return;
                    }
                    string s = folderBrowserDialog1.SelectedPath;
                    s += @"\";
                    if (CreateNewFolder)
                        s += newProjectName + @"\";
                    if (OpenedProject != null)
                        this.closeProjectToolStripMenuItem_Click(sender, e);
                    OpenedProject = ByterProject.CreateProject(newProjectName + ".bytpr", s);
                    projectExplorerBox.Items.Clear();
                    codeTabControl.TabPages.Clear();
                    foreach (var k in OpenedProject.FileNames)
                        projectExplorerBox.Items.Add("Module " + k.Key);
                    this.AddTab(OpenedProject.MainMethod);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (codeTabControl.TabCount == 0)
                    return;
                TabPage currentTab = codeTabControl.SelectedTab;
                string methodName = currentTab.Name;
                DataGridView dg = (DataGridView)(currentTab.Controls.Find("codeGridView" + methodName, true)[0]);
                char[,] table = new char[16, 16];
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 16; j++)
                        table[i, j] = dg.Rows[i].Cells[j].Value.ToString()[0];
                OpenedProject.ChangeSource(methodName, table);
                OpenedProject.SaveFile(methodName);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenedProject.SaveAll();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        public void ShowErrors()
        {
            errorBox.Items.Clear();
            if (CompilationErrors != null)
            {
                if (CompilationErrors.Criticals != null)
                    for (int i = 0; i < CompilationErrors.Criticals.Count; i++)
                    {
                        string s = string.Empty;
                        Error current = CompilationErrors.Criticals[i];
                        switch (current.Type)
                        {
                            case ErrorType.MissReturn: s = string.Format("Miss return symbol('#') in {0}", current.ModuleName); break;
                            case ErrorType.UnknownSymbol: s = string.Format("Unknown symbol in {0}:{1} in module {2}", current.Row, current.Column, current.ModuleName); break;
                            case ErrorType.MissDirectionalSymbolInFirstCell: s = string.Format("Left directional operator in {0}", current.ModuleName); break;
                        }
                        errorBox.Items.Add(s);
                    }
                if (CompilationErrors.NonCriticals != null)
                    for (int i = 0; i < CompilationErrors.NonCriticals.Count; i++)
                    {
                        string s = string.Empty;
                        Error current = CompilationErrors.NonCriticals[i];
                        switch (current.Type)
                        {
                            case ErrorType.Recursion: s = string.Format("Methods {0} compose recursion cycle", current.Resource); break;
                            case ErrorType.UnUsedProcedure: s = string.Format("Procedure {0} never was called", current.ModuleName); break;
                        }
                        errorBox.Items.Add(s);
                    }
            }
        }
        public bool Compile()
        {
            char[,] main = OpenedProject.Sources[OpenedProject.MainMethod];
            var keys = OpenedProject.Sources.Keys.ToList();
            Dictionary<char, char[,]> otherMethods = new Dictionary<char, char[,]>();
            foreach (string key in keys)
                if (key.Length == 1)
                    otherMethods.Add(key[0], OpenedProject.Sources[key]);
            Compiler comp = new Compiler(main, otherMethods);
            Tuple<int, CompileErrors> res = comp.GenerateCode(Path.GetDirectoryName(OpenedProject.ProjectPath) + @"\" + Path.GetFileNameWithoutExtension(OpenedProject.ProjectPath) + ".exe");
            CompilationErrors = res.Item2;
            ShowErrors();
            return res.Item1 == 0;
        }

        private void compileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Compile())
                {
                    MessageBox.Show("Compilation was failed");
                    return;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void compileAndRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Compile())
                {
                    MessageBox.Show("Compilation was failed");
                    return;
                }
                string fileName = Path.GetDirectoryName(OpenedProject.ProjectPath) + @"\" + Path.GetFileNameWithoutExtension(OpenedProject.ProjectPath) + ".exe";
                if (File.Exists(fileName))
                    System.Diagnostics.Process.Start(fileName);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void debugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (OpenedProject == null)
                    return;
                if (DbgProcess != null)
                {
                    DbgProcess.Kill();
                    debugToolStripMenuItem.Text = "Debug";
                    return;
                }
                if (MessageBox.Show("Project will be saved to debug.\nDo you sure to start?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    OpenedProject.SaveAll();
                    DbgProcess = new System.Diagnostics.Process();
                    Directory.SetCurrentDirectory(Path.GetDirectoryName(Application.ExecutablePath));
                    DbgProcess.StartInfo = new System.Diagnostics.ProcessStartInfo("Debugger.exe", OpenedProject.ProjectPath);
                    DbgProcess.EnableRaisingEvents = true;
                    DbgProcess.Exited += DbgProcess_Exited;
                    DbgProcess.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void DbgProcess_Exited(object sender, EventArgs e)
        {
            DbgProcess = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultName">contains name of module</param>
        /// <param name="succesful">false, if user has made mistakes</param>
        /// <returns>path to file</returns>
        public string ChooseMethodNameAndPath(ref string name, out bool succesful)
        {
            try
            {
                succesful = false;
                bool useDefaultDir = true;
                string old = name;
                string path = Path.GetDirectoryName(OpenedProject.ProjectPath);
                if (!(InputBox.ShowDialog("Choose name", "Choose new name of module(it must have only 1 letter).", "Use default directory(You can use only subdirectories of project directory)", ref name, ref useDefaultDir) == DialogResult.OK))
                    return path;
                if (name.Length != 1 || name == "V" || name == "A" || (OpenedProject.FileNames.ContainsKey(name)&& name!=old))
                {
                    MessageBox.Show("Module name must be 1 unused letter.\nAlso it must not be used in project and must not be letters 'A' or 'V'.");
                    return path;
                }
                path += @"\" + name + ".byt";
                if (!useDefaultDir)
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.DefaultExt = ".byt";
                    sfd.Filter = "Byter module file|*.byt";
                    sfd.AddExtension = true;
                    sfd.FileName = name + ".byt";
                    Directory.SetCurrentDirectory(Path.GetDirectoryName(OpenedProject.ProjectPath));
                    if (sfd.ShowDialog(this) != DialogResult.OK)
                        return path;
                    if (!sfd.FileName.Contains(Path.GetDirectoryName(OpenedProject.ProjectPath)))
                    {
                        MessageBox.Show("Modules must be contained only in folder of the project or in it's subfolders.");
                        return path;
                    }
                    path = sfd.FileName;
                }
                succesful = true;
                return path;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
                succesful = false;
                return Path.GetDirectoryName(OpenedProject.ProjectPath);
            }
        }
        private void addModuleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string name = "";
                bool success;
                string path = ChooseMethodNameAndPath(ref name, out success);
                if (success)
                {
                    OpenedProject.AddMethod(name, path);
                    projectExplorerBox.Items.Clear();
                    foreach (var k in OpenedProject.FileNames)
                        projectExplorerBox.Items.Add("Module " + k.Key);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (codeTabControl.TabCount == 0)
                    return;
                string name = codeTabControl.SelectedTab.Name;
                if (name.Length != 1 || !OpenedProject.Sources.ContainsKey(name))
                    return;
                bool success;
                string oldName = name;
                string path = ChooseMethodNameAndPath(ref name, out success);
                if (success)
                {
                    OpenedProject.RenameMethod(oldName, name);
                    OpenedProject.FileNames[name] = path;
                    
                    projectExplorerBox.Items.Clear();
                    foreach (var k in OpenedProject.FileNames)
                        projectExplorerBox.Items.Add("Module " + k.Key);
                    codeTabControl.TabPages.RemoveAt(codeTabControl.SelectedIndex);
                    
                    foreach (TabPage a in codeTabControl.TabPages)
                        this.LoadCodeToTab(a);
                    this.OpenTab(name);
                    OpenedProject.SaveFile(name);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if(DbgProcess!= null)
                    switch (MessageBox.Show(this, "Stop debugging?", "Confirm", MessageBoxButtons.OKCancel))
                    {
                        case DialogResult.OK: DbgProcess.Kill(); break;
                        case DialogResult.Cancel: e.Cancel = true; return;
                    }
                if (OpenedProject != null && OpenedProject.ProjectChanged)
                    switch (MessageBox.Show(this, "Save changes in project?", "Confirm", MessageBoxButtons.YesNoCancel))
                    {
                        case DialogResult.Yes: OpenedProject.SaveAll(); break;
                        case DialogResult.Cancel: e.Cancel = true; return;
                    }
                OpenedProject = null;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (codeTabControl.TabCount == 0)
                    return;
                var currentTab = codeTabControl.SelectedTab;
                if (OpenedProject != null && OpenedProject.FilesChanged.ContainsKey(currentTab.Name) && OpenedProject.FilesChanged[currentTab.Name])
                    switch (MessageBox.Show(this, "Save changes in module" + currentTab.Name + '?', "Confirm", MessageBoxButtons.YesNoCancel))
                    {
                        case DialogResult.Yes: OpenedProject.SaveFile(currentTab.Name); break;
                        case DialogResult.Cancel: return;
                    }
                codeTabControl.TabPages.Remove(currentTab);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void errorBox_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (errorBox.SelectedItem == null || OpenedProject == null || CompilationErrors == null)
                    return;
                int index = errorBox.SelectedIndex;
                Error error = (index < CompilationErrors.Criticals.Count) ? CompilationErrors.Criticals[index] : CompilationErrors.NonCriticals[index - CompilationErrors.Criticals.Count];
                switch (error.Type)
                {
                    case ErrorType.MissDirectionalSymbolInFirstCell:
                        OpenTab(error.ModuleName);
                        (codeTabControl.SelectedTab.Controls.Find("codeGridView" + error.ModuleName, true)[0] as DataGridView).ClearSelection();
                        (codeTabControl.SelectedTab.Controls.Find("codeGridView" + error.ModuleName, true)[0] as DataGridView).Rows[0].Cells[0].Selected = true;
                        break;
                    case ErrorType.MissReturn: OpenTab(error.ModuleName); break;
                    case ErrorType.Recursion: OpenTab(error.Resource.ToString()[0].ToString()); break;
                    case ErrorType.UnknownSymbol:
                        OpenTab(error.ModuleName);
                        (codeTabControl.SelectedTab.Controls.Find("codeGridView" + error.ModuleName, true)[0] as DataGridView).ClearSelection();
                        (codeTabControl.SelectedTab.Controls.Find("codeGridView" + error.ModuleName, true)[0] as DataGridView).Rows[error.Row].Cells[error.Column].Selected = true;
                        break;
                    case ErrorType.UnUsedProcedure: OpenTab(error.ModuleName); break;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void toolStripMenuItemDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (projectExplorerBox.SelectedItem == null || OpenedProject == null) return;
                string methodName = projectExplorerBox.SelectedItem.ToString().Remove(0, "Module ".Length);
                if (codeTabControl.TabPages.ContainsKey(methodName))
                    codeTabControl.TabPages.RemoveByKey(methodName);
                if (methodName.Length > 1)
                {
                    MessageBox.Show("You cannot delete main method");
                    return;
                }
                OpenedProject.RemoveMethod(methodName);
                projectExplorerBox.Items.Clear();
                foreach (var k in OpenedProject.FileNames)
                    projectExplorerBox.Items.Add("Module " + k.Key);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (projectExplorerBox.SelectedItem == null || OpenedProject == null) return;
                string methodName = projectExplorerBox.SelectedItem.ToString().Remove(0, "Module ".Length);
                string newname = methodName;
                if (codeTabControl.TabPages.ContainsKey(methodName))
                {
                    TabPage pg = codeTabControl.TabPages[codeTabControl.TabPages.IndexOfKey(methodName)];
                    foreach (Control a in pg.Controls)
                        a.LostFocus -= codeTextBox_Leave;
                    pg.Focus();
                }
                if (InputBox.ShowDialog("Changing method name", "Input new name", ref newname) == DialogResult.OK)
                {
                    if (methodName.ToUpper() == "MAIN")
                        if (newname.ToUpper() != "MAIN")
                        {
                            MessageBox.Show("Main method must be called as 'Main'");
                            return;
                        }
                        else
                            OpenedProject.RenameMethod(methodName, newname);
                    else
                        OpenedProject.RenameMethod(methodName, newname);
                    projectExplorerBox.Items.Clear();
                    foreach (var k in OpenedProject.FileNames)
                        projectExplorerBox.Items.Add("Module " + k.Key);
                    if (codeTabControl.TabPages.ContainsKey(methodName))
                    {
                        codeTabControl.TabPages.RemoveByKey(methodName);
                        OpenTab(newname);
                    }
                    foreach (TabPage a in codeTabControl.TabPages)
                        LoadCodeToTab(a);
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }




    }
}
