using SDBManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMGUI
{
    public partial class Form1 : Form
    {
        SQLMem SQL;
        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            SQL = new SQLMem(File.ReadAllBytes(openFileDialog1.FileName));

            listBox1.Items.Clear();
            listBox1.Items.AddRange(SQL.Import());
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
                    listBox1.SelectedIndex++;
                }
            }
            catch { }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var Str = (string)listBox1.Items[listBox1.SelectedIndex];
                
                if (Str == null)
                    listBox1.SelectedIndex++;

                textBox1.Text = Str;
            }
            catch { }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            var Lines = listBox1.Items.Cast<string>().ToArray();
            File.WriteAllBytes(saveFileDialog1.FileName, SQL.Export(Lines));
        }
    }
}
