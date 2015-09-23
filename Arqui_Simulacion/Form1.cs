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

        private int numero;
        public Form1()
        {
            InitializeComponent();

             numero = 5;

            Thread thread = new Thread( new ThreadStart(controlador));
            thread.Start();


            primerNucleo();

            thread.Abort();
        }

        public void controlador()
        {
            numero = 10;

        }

        public void primerNucleo()
        {
            segundoNucleo();
        }

        public void segundoNucleo()
        {
            MessageBox.Show(""+numero);
        }

    }
}
