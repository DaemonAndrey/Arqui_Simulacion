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

        private int[] bloques_cache_instrucciones_nucleo1; //indica el número de bloques que están en caché

        private int[] hilo_a_ejecutar; //indica cual hilo va a ejecutar cada núcleo

        private bool[] fin_hilos; //indica cuales hilos ya terminaron

        private long reloj;

        private bool finPrograma;

        private int[,] PCB;

        private int PC1;
        private int PC2;

        private List<int> colaRR; //Simula la cola para el round Robin
        private Dictionary<int, int> mapaContexto; //Asocia el id del hilo con el contexto

        private int hilo; //Variable que indica si hay un hilo para ejecución

        private int numHilos;  //indica cuántos hilos de MIPS (archivos) tiene el programa

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

            hilo_a_ejecutar = new int[2];

            fin_hilos = new bool[6];


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
            int quantum;
            int [] instruccion = new int[4];
            bool aciertoCache = false;
            while (!finPrograma)
            {
                if (Thread.CurrentThread.Name == "Nucleo1")
                {
                    int numHilo1 = hilo_a_ejecutar[0];  //se guarda en numHilo1 el número de hilo que le toca ejecutar
                    for (int i = 0; i < 32; i++)    //recupera el contexto (falta PC)
                    {
                        registro_nucleo1[i] = PCB[numHilo1, i];
                    }
                    while (quantum != 0 && !fin_hilos[numHilo1])    //mientras tenga quantum y no haya terminado el hilo
                    {
                        int numBloque = PC1 / 4;    //calcula el número de bloque en el que está la siguiente instrucción
                        int i = 0;
                        while (i < numHilos && !aciertoCache)    //busca el bloque en caché
                        {
                            if (numBloque == bloques_cache_instrucciones_nucleo1[i])
                            {
                                aciertoCache = true;
                            }
                        }

                        /** Aquí va el fallo de caché **/
                        if (!aciertoCache)
                        {

                        }

                        int numInstruccion = PC1 % 4;
                        for (int j = numInstruccion; j < numInstruccion + 4; j++)
                        {
                            instruccion[j] = cache_instrucciones_nucleo1[i, j];
                        }
                        PC1 += 4;
                        ejecutarInstruccion(ref instruccion);
                        quantum--;
                    }
                    if (quantum == 0 && !fin_hilos[numHilo1])
                    {
                        for (int i = 0; i < 32; i++)    //guarda el contexto (falta PC)
                        {
                            PCB[numHilo1, i] = registro_nucleo1[i];
                        }
                    }
                }

                else if (Thread.CurrentThread.Name == "Nucleo2")
                {
                    int numHilo2 = hilo_a_ejecutar[1];
                    for (int i = 0; i < 32; i++)
                    {
                        registro_nucleo2[i] = PCB[numHilo2, i];
                    }
                }
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

        private void ejecutarInstruccion(ref int[] ins)
        {
            switch (ins[0])
            {
                case 8: //ADDI
                    break;

                case 32: //ADD
                    break;

                case 34: //SUB
                    break;

                case 12: //MUL
                    break;

                case 14: //DIV
                    break;

                case 4: //BEQZ
                    break;

                case 5: //BNEZ
                    break;

                case 3: //JAL
                    break;

                case 2: //JR
                    break;

                case 63: //FIN
                    break;
            }
        }

        

    }
}
