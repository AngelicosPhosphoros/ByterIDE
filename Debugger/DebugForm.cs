using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CompilerLib;

namespace Debugger
{
    public partial class DebugForm : Form
    {
        public DebugForm()
        {
            InitializeComponent();
            CurrentPosition = new Point(0, 0);
            IsMain = false;
            CurrentDirection = Direction.Right;
            CodeTable = new char[16, 16];
            dataGridView1.RowCount = 16;
            dataGridView1.ColumnCount = 16;
            for (int i = 0; i < 16; i++)
                dataGridView1.Columns[i].Width = dataGridView1.Rows[i].Height;
        }

        public DebugForm(ByterProject project, string methodName, OutputForm output)
        {
            if (project == null || methodName == null || output == null)
                throw new ArgumentNullException("Cannot create debug form for empty procedure.");
            InitializeComponent();
            Project = project;
            CodeTable = new char[16, 16];
            Array.Copy(Project.Sources[methodName], CodeTable, 256);
            Output = output;
            CurrentPosition = new Point(0, 0);
            IsMain = false;
            CurrentDirection = Direction.Right;
            Child = Parent = null;
            dataGridView1.RowCount = 16;
            dataGridView1.ColumnCount = 16;
            RefreshTable();
            for (int i = 0; i < 16; i++)
                dataGridView1.Columns[i].Width = dataGridView1.Rows[i].Height;
            Text += " — " + methodName;
        }

        static DebugForm()
        {
            CharTable = new char[16, 16];
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    CharTable[i, j] = System.Text.Encoding.ASCII.GetString(new byte[] { (byte)(16 * i + j) })[0];
        }

        public static char[,] CharTable;
        public bool IsMain;
        public char[,] CodeTable;
        public ByterProject Project;
        Point CurrentPosition;
        public OutputForm Output;
        Direction CurrentDirection;
        public DebugForm Child;
        public DebugForm Parent;
        /// <summary>
        /// This is for highlight text
        /// </summary>
        private static Tuple<List<char[]>, Color[]> SyntaxColor =
            new Tuple<List<char[]>, Color[]>(
            new List<char[]>
            (new char[][]{
                new char[]{ '>', '<', 'V', 'A' },
                new char[]{ '#' },
                new char[]{ '+', '-', '}','{'},
                new char[] {'$'}, new char[]{'0'}}
            ),
            new Color[] { Color.Blue, Color.Red, Color.Green, Color.DarkViolet, Color.Black });
        private void Print(Point position)
        {
            Output.Add(CharTable[position.Y, position.X].ToString());
        }
        private int MakeStep(bool goInto)
        {
            return MakeStep(CodeTable, ref CurrentPosition, ref CurrentDirection, goInto);
        }
        private int MakeStep(char[,] codeTable, ref Point position, ref Direction direction, bool goInto)
        {
            switch (codeTable[position.Y, position.X])
            {
                case '>': codeTable[position.Y, position.X] = '<'; position.X++; direction = Direction.Right; break;
                case '<': codeTable[position.Y, position.X] = '>'; position.X--; direction = Direction.Left; break;
                case 'V': codeTable[position.Y, position.X] = 'A'; position.Y++; direction = Direction.Down; break;
                case 'A': codeTable[position.Y, position.X] = 'V'; position.Y--; direction = Direction.Up; break;
                case '0':
                    switch (direction)
                    {
                        case Direction.Up: position.Y++; direction = Direction.Down; break;
                        case Direction.Right: position.X--; direction = Direction.Left; break;
                        case Direction.Down: position.Y--; direction = Direction.Up; break;
                        case Direction.Left: position.X++; direction = Direction.Right; break;
                    }
                    break;
                case '}': Print(position); position.X++; direction = Direction.Right; break;
                case '{': Print(position); position.X--; direction = Direction.Left; break;
                case '+': Print(position); position.Y--; direction = Direction.Up; break;
                case '-': Print(position); position.Y++; direction = Direction.Down; break;
                case '$': Print(position); position = new Point(0, 0); break;
                case '#': return 1;
                default:
                    if (Project.Sources.ContainsKey(codeTable[position.Y, position.X].ToString()))
                    {
                        if (goInto)
                        {
                            this.statusLabel.Text = "Waiting";
                            this.StepBtn.Enabled = this.StepIntoBtn.Enabled = this.StepOutBtn.Enabled = false;
                            Child = new DebugForm(Project, codeTable[position.Y, position.X].ToString(), Output);
                            Child.Parent = this;
                            Child.Show();
                        }
                        else
                        {
                            this.statusLabel.Text = "Waiting";
                            this.StepBtn.Enabled = this.StepIntoBtn.Enabled = this.StepOutBtn.Enabled = false;
                            RunToEnd(Project.Sources[codeTable[position.Y, position.X].ToString()], new Point(0, 0), Direction.Right);
                            this.StepBtn.Enabled = this.StepIntoBtn.Enabled = this.StepOutBtn.Enabled = true;
                            statusLabel.Text = "Step-by-step running";
                        }
                    }
                    else
                        MessageBox.Show("Unknown symbol");
                    switch (direction)
                    {
                        case Direction.Right: position.X++; break;
                        case Direction.Left: position.X--; break;
                        case Direction.Up: position.Y--; break;
                        case Direction.Down: position.Y++; break;
                    }
                    break;
            }
            if (position.X < 0) position.X += 16;
            if (position.X >= 16) position.X -= 16;
            if (position.Y < 0) position.Y += 16;
            if (position.Y >= 16) position.Y -= 16;
            return 0;
        }

        public void RunToEnd(char[,] codeTable, Point startPosition, Direction startD)
        {
            char[,] tmpTable = new char[16, 16]; // For safety changing of code table without changing source
            Array.Copy(codeTable, tmpTable, 256);
            while (MakeStep(tmpTable, ref startPosition, ref startD, false) == 0) ;
        }

        private void StepBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (MakeStep(false) != 0)
                {
                    RefreshTable();
                    if (!IsMain)
                    {
                        this.Close();
                        return;
                    }
                    else
                    {
                        StepBtn.Enabled = StepIntoBtn.Enabled = StepOutBtn.Enabled = false;
                    }
                }
                RefreshTable();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void StepIntoBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (MakeStep(true) != 0 && !IsMain)
                {
                    RefreshTable();
                    if (!IsMain)
                    {
                        this.Close();
                        return;
                    }
                    else
                        StepBtn.Enabled = StepIntoBtn.Enabled = StepOutBtn.Enabled = false;
                }
                RefreshTable();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void StepOutBtn_Click(object sender, EventArgs e)
        {
            try
            {
                this.StepBtn.Enabled = this.StepIntoBtn.Enabled = this.StepOutBtn.Enabled = false;
                this.statusLabel.Text = "Waiting";
                RunToEnd(CodeTable, CurrentPosition, CurrentDirection);
                this.statusLabel.Text = "Finish";
                if (this.IsMain)
                {
                    StepBtn.Enabled = StepIntoBtn.Enabled = StepOutBtn.Enabled = false;
                    RefreshTable();
                }
                else
                    this.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void RefreshTable()
        {
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                {
                    dataGridView1.Rows[i].Cells[j].Value = CodeTable[i, j];
                    dataGridView1.Rows[i].Cells[j].Selected = false;
                }
            dataGridView1.Rows[CurrentPosition.Y].Cells[CurrentPosition.X].Selected = true;
        }

        private void DebugForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (Child != null)
                    Child.Close();
                if (Parent != null && !IsMain)
                    Parent.StepBtn.Enabled = Parent.StepIntoBtn.Enabled = Parent.StepOutBtn.Enabled = true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void DebugForm_Shown(object sender, EventArgs e)
        {
            try
            {
                if (Output != null)
                    Output.Show();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // this is need to create syntax highlighting
                char c = (sender as DataGridView).Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString()[0];
                int index = SyntaxColor.Item1.FindIndex(n => { return n.Contains(c); });
                (sender as DataGridView).Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = index >= 0 ? SyntaxColor.Item2[index] : Color.Brown;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

    }

    public enum Direction { Up, Right, Left, Down };
}
