using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
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

        private int[,] PCB;

        private bool fin_programa;

        public Form1()
        {
            InitializeComponent();

            default_values();

            RAM = new int[2048];
            registro_nucleo1 = new int[32];
            registro_nucleo2 = new int[32];

            cache_datos_nucleo1 = new int[8,4];
            cache_datos_nucleo2 = new int[8,4];

            cache_instrucciones_nucleo1 = new int[8,16];
            cache_instrucciones_nucleo2 = new int[8,16];

            PCB = new int[6,33]; //De momento se define con un tamaño fijo, luego por entrada de interfaz

          //  Thread control = new Thread(new ThreadStart(controlador)); //Iniciamos el controlador (Scheduler)
           // control.Start();




        }

        private void default_values()
        {
            button1.Enabled = false;
        }

        public void controlador()
        {
            Queue colaRR = new Queue(); //Creamos el calendarizador
            Queue colaContexto = new Queue(); 

            Thread nucleo1 = new Thread(new ThreadStart(nucleo));
            nucleo1.Start();
            Thread nucleo2 = new Thread(new ThreadStart(nucleo));
            nucleo2.Start();

            reloj = 0;

            while(colaRR.Count != 0)
            {
                
            }


        }

        public void nucleo()
        {
           while(!fin_programa)
           {
            
           }
        }



        private void edicionToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            button2.Visible = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            button2.Visible = false;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            int pointer = 0; //puntero que indica la posicion en la que debe guardarse el siguiente entero en la RAM
            OpenFileDialog open = new OpenFileDialog();
            System.IO.StreamReader sr;
            int[] temporal; //guarda el un array de int's temporal
            String line;
            int counter = 0;

            open.Multiselect = true; //permite seleccionar mas de un archivo
            open.Title = "Seleccione los hilos";
            open.Filter = "All files(*.txt)|*.txt";

            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach(String file in open.FileNames)
                {
                    ++counter;

                   sr = new System.IO.StreamReader(file);                   

                    line = sr.ReadLine();
                    while((line != null) && (line != ""))
                    {

                       temporal = line.Split(' ').Select(int.Parse).ToArray();
                       
                        for (int i = 0; i < 4; ++i, ++pointer )
                        {
                            RAM[pointer] = temporal[i];
                        }

                        line = sr.ReadLine();
                    }

                    sr.Close();
                }       
                
            }

            if((int.Parse(textBox1.Text) != counter) && (counter > 0))
            {
                textBox1.Text = "" + counter;

                MessageBox.Show("El parámetro que usted especificó para la cantidad de hilos "+
                "no coincide con la cantidad de archivos que escogió, pero lo hemos cambiado, no se preocupe.",
                "Información",MessageBoxButtons.OK,MessageBoxIcon.Information);

            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = false;

            if(textBox1.Text != "")
            {
                button1.Enabled = true;
            }
        }



        

    }
}
