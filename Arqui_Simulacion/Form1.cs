﻿using System;
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

        private int[] registro_nucleo1;
        private int[] registro_nucleo2;

        private int[,] cache_datos_nucleo1;
        private int[,] cache_datos_nucleo2;


        private int[,] cache_instrucciones_nucleo1;
        private int[,] cache_instrucciones_nucleo2;

        private long reloj;

        private bool finPrograma;

        private int[,] PCB;

        public Form1()
        {
            InitializeComponent();

            RAM = new int[512];
            registro_nucleo1 = new int[32];
            registro_nucleo2 = new int[32];

            cache_datos_nucleo1 = new int[8,4];
            cache_datos_nucleo2 = new int[8,4];

            cache_instrucciones_nucleo1 = new int[8,4];
            cache_instrucciones_nucleo2 = new int[8,4];

            PCB = new int[6,33];

            Thread control = new Thread(new ThreadStart(controlador)); //Iniciamos el controlador (Scheduler)
            control.Start();

            


        }

        public void controlador()
        {
            Thread nucleo1 = new Thread(new ThreadStart(nucleo));
            nucleo1.Start();
            Thread nucleo2 = new Thread(new ThreadStart(nucleo));
            nucleo2.Start();

        }

        public void nucleo()
        {
            while (!finPrograma)
            {
                if (Thread.CurrentThread.Name == "Nucleo1")
                {

                }
            }
        }



        private void edicionToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        /**Metodo para abrir archivos**/
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
