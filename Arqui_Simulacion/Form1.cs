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

        private List<int> colaRR; //Simula la cola para el round Robin
        private Dictionary<int, int> mapaContexto; //Asocia el id del hilo con el contexto

        private int hilo; //Variable que indica si hay un hilo para ejecución

        public Form1()
        {
            colaRR = new List<int>(); //Creamos el calendarizador
            mapaContexto = new Dictionary<int, int>(); //<id del proceso, puntero al contexto>


            InitializeComponent();

            default_values();

            hilo = -1;

            RAM = new int[2048];
            registro_nucleo1 = new int[32];
            registro_nucleo2 = new int[32];

            cache_datos_nucleo1 = new int[8, 16];
            cache_datos_nucleo2 = new int[8, 16];

            cache_instrucciones_nucleo1 = new int[8, 16];
            cache_instrucciones_nucleo2 = new int[8, 16];


          //  Thread control = new Thread(new ThreadStart(controlador)); //Iniciamos el controlador (Scheduler)
           // control.Start();




        }

        private void default_values()
        {
            button1.Enabled = false;
        }

        public void controlador()
        {

            int contador = 0;

            Thread nucleo1 = new Thread(new ThreadStart(nucleo));
            nucleo1.Name = "nucleo1";
            nucleo1.Start();
            Thread nucleo2 = new Thread(new ThreadStart(nucleo));
            nucleo2.Name = "nucleo2";
            nucleo2.Start();

            reloj = 0;

            while(colaRR.Count != 0)
            {
                contador = (contador == (colaRR.Count - 1)) ? 0 : contador; // Si ya llegamos al final
                //Devuelvase al inicio de la cola, si no, siga en donde está

                hilo = contador;

                
                ++contador;
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
            int puntero = 0; //puntero que indica la posicion en la que debe guardarse el siguiente entero en la RAM
            OpenFileDialog archivador = new OpenFileDialog();
            
            int[] temporal; //guarda el un array de int's temporal
            String linea;
            int contador = 0;
            System.IO.StreamReader sr;

            archivador.Multiselect = true; //permite seleccionar mas de un archivo
            archivador.Title = "Seleccione los hilos";
            archivador.Filter = "All files(*.txt)|*.txt";

            if (archivador.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach(String file in archivador.FileNames)
                {
                    ++contador;


                    colaRR.Add(puntero); //Agregamos a la lista de Round Robin el id del hilo
                    //En este caso usaremos como id del hilo, la direccion en memoria
                    //donde inicia el hilo

                   sr = new System.IO.StreamReader(file);
                   cargarRAM(ref sr, ref puntero);
                   sr.Close();

                }       
                
            }


            if ((int.Parse(textBox1.Text) != contador) && (contador > 0))
            {
                textBox1.Text = "" + contador;

                MessageBox.Show("El parámetro que usted especificó para la cantidad de hilos " +
                "no coincide con la cantidad de archivos que escogió, pero lo hemos cambiado, no se preocupe.",
                "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }

            for (int i = 0; i < colaRR.Count; ++i )
            {
                MessageBox.Show(""+colaRR[i]);
            }
        }

        private void cargarRAM(ref System.IO.StreamReader sr, ref int puntero)
        {
            String linea;
            int[] temporal;


            linea = sr.ReadLine();
            while ((linea != null) && (linea != ""))
            {

                temporal = linea.Split(' ').Select(int.Parse).ToArray();

                for (int i = 0; i < 4; ++i, ++puntero)
                {
                    RAM[puntero] = temporal[i];
                }

                linea = sr.ReadLine();
                
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

        private void button3_Click(object sender, EventArgs e)
        {
            PCB = new int[int.Parse(textBox1.Text), 33]; //Iinicializamos el PCB (Process Control Block)

        }



        

    }
}
