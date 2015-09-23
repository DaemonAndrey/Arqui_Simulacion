using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace Arqui_Simulacion
{
    public partial class Form1 : Form
    {
        private int[] RAM;

        public Form1()
        {
            InitializeComponent();

            RAM = new int[512];

        }

        public void controlador()
        {


        }

        public void primerNucleo()
        {
           
        }

        public void segundoNucleo()
        {
           
        }

        private void edicionToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e) //Este método es el del menú Abrir
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.IO.StreamReader sr = new
                   System.IO.StreamReader(open.FileName);
                MessageBox.Show(sr.ReadToEnd());
                sr.Close();
            }
        }

    }
}
