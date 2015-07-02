using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace ByterIDE
{
    public static class InputBox
    {
        private static int Margin =3;
        private static Form CreateForm(string caption, string text, string defaultInput)
        {
            Form dialForm = new Form();
            dialForm.Size = new Size(400, 170);
            dialForm.AutoSize = true;
            dialForm.Text = caption;
            Label label = new Label();
            label.AutoSize = true;
            label.Text = text;
            label.Location = new Point(10, 10);
            dialForm.Controls.Add(label);
            TextBox tb = new TextBox();
            tb.Text = defaultInput;
            tb.Left = 10;
            tb.Top = label.Bounds.Bottom+Margin;
            tb.Name = "TextBox";
            dialForm.Controls.Add(tb);
            Button btnOK = new Button();
            Button btnCancel = new Button();
            btnOK.Size = btnCancel.Size = new Size(tb.Width / 2 - 10, 25);
            btnOK.Location = new Point(tb.Left, tb.Bounds.Bottom + 2 * Margin + 16);
            btnCancel.Location = new Point(tb.Bounds.Right - btnCancel.Width, btnOK.Top);
            btnOK.DialogResult = DialogResult.OK;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnOK.AutoSize = btnCancel.AutoSize = true;
            btnOK.Text = "OK";
            btnCancel.Text = "Cancel";
            dialForm.Controls.Add(btnOK);
            dialForm.Controls.Add(btnCancel);
            dialForm.Width= btnOK.Bounds.Right+Margin;
            return dialForm;
        }
        private static Form CreateFormWithCheckBox(string caption, string text, string defaultInput, string checkText, bool defaultVariant)
        {
            Form result = CreateForm(caption, text, defaultInput);
            CheckBox chBox = new CheckBox();
            chBox.Left = 10;
            chBox.AutoSize = true;
            chBox.Text = checkText;
            chBox.Checked = defaultVariant;
            chBox.Top = result.Controls.Find("TextBox", true)[0].Bounds.Bottom + Margin;
            chBox.Name = "CheckBox";
            result.Controls.Add(chBox);
            return result;
        }
        public static DialogResult ShowDialog(string caption, string text, ref string defaultInput)
        {
            Form form = CreateForm(caption, text, defaultInput);
            DialogResult res = form.ShowDialog();
            defaultInput = form.Controls.Find("TextBox", true)[0].Text;
            return res;
        }
        public static DialogResult ShowDialog(string caption, string text, string checkText, ref string defaultInput, ref bool defaultVariant)
        {
            Form form = CreateFormWithCheckBox(caption, text, defaultInput,checkText,defaultVariant);
            DialogResult res = form.ShowDialog();
            defaultInput = form.Controls.Find("TextBox", true)[0].Text;
            defaultVariant = (form.Controls.Find("CheckBox", true)[0] as CheckBox).Checked;
            return res;
        }
    }
}
