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
        private bool archivosCargados;

        private int[] registro_nucleo1;
        private int[] registro_nucleo2;

        private int[,] cache_datos_nucleo1;
        private int[,] cache_datos_nucleo2;


        private int[,] cache_instrucciones_nucleo1;
        private int[,] cache_instrucciones_nucleo2;

        private int[] bloques_cache_instrucciones_nucleo1; //indica el número de bloques que están en caché
        private int[] bloques_cache_instrucciones_nucleo2;

        private int[] hilo_a_ejecutar; //indica cual hilo va a ejecutar cada núcleo

        private bool[] fin_hilos; //indica cuales hilos ya terminaron

        private long reloj;

        private bool finPrograma;

        private int[,] PCB;

        private int PC1;
        private int PC2;

        private List<int> colaRR; //Simula la cola para el round Robin
        private Dictionary<int, int> mapaContexto; //Asocia el id del hilo con el contexto

        private int quatum;
        private int quatumTempo;
        private int tiempoLecturaEscritura;
        private int tiempoTransferencia;
        private bool modoLento;
        private int numHilos;

        private WaitHandle[] banderas_nucleos_controlador;

        private AutoResetEvent bandera_nucleo1_controlador;
        private AutoResetEvent bandera_nucleo2_controlador;

        private AutoResetEvent bandera_controlador_nucleo1;
        private AutoResetEvent bandera_controlador_nucleo2;

        private ArrayList bus;

        private string textoInterfaz;

        public Form1()
        {
            InitializeComponent();


            default_values();

            archivosCargados = false; //Se enciende cuando se cargan los archivos.
            textoInterfaz = "";

            colaRR = new List<int>(); //Creamos el calendarizador
            mapaContexto = new Dictionary<int, int>(); //<id del proceso, puntero al contexto>

            modoLento = false;


            RAM = new int[2048];
            registro_nucleo1 = new int[32];
            registro_nucleo2 = new int[32];

            cache_datos_nucleo1 = new int[8, 16];
            cache_datos_nucleo2 = new int[8, 16];

            cache_instrucciones_nucleo1 = new int[8, 16];
            cache_instrucciones_nucleo2 = new int[8, 16];

            bloques_cache_instrucciones_nucleo1 = new int[8];
            bloques_cache_instrucciones_nucleo2 = new int[8];

            hilo_a_ejecutar = new int[2];

            

            bus = new ArrayList(4);

            banderas_nucleos_controlador = new WaitHandle[2];

            bandera_nucleo1_controlador = new AutoResetEvent(true);
            bandera_nucleo2_controlador = new AutoResetEvent(true);

            banderas_nucleos_controlador[0] = bandera_nucleo1_controlador;
            banderas_nucleos_controlador[1] = bandera_nucleo2_controlador;

            bandera_controlador_nucleo1 = new AutoResetEvent(false);
            bandera_controlador_nucleo2 = new AutoResetEvent(false);
        }

        private void default_values()
        {
            button1.Enabled = false;
        }

        public void controlador()
        {
            int contador = 0;


            Thread nucleo1 = new Thread(new ThreadStart(nucleo_1));
            nucleo1.Name = "nucleo1";
            nucleo1.Start();
            Thread nucleo2 = new Thread(new ThreadStart(nucleo_2));
            nucleo2.Name = "nucleo2";
            nucleo2.Start();
            
            reloj = -1;

            hilo_a_ejecutar[0] = contador;
            hilo_a_ejecutar[1] = ++contador;

            while(colaRR.Count != 0)
            {
                /**
                  if(fin_hilos[(contador - 1)] )
                  {
                    colaRR.Remove((contador-1));
                    finQuantum1 = true;
                  }
               
                  if(fin_hilos[contador])
                  {
                    colaRR.Remove(contador);
                    finQuatum2 = true;
                  }
                  
                 if(finQuantum1)
                 {
                    
                   contador = (contador == (colaRR.Count - 1)) ? 0 : ++contador; // Si ya llegamos al final
                    //Devuelvase al inicio de la cola, si no, sume 1
                 * 
                    hilo_a_ejecutar[0] = contador;
                 
                 }
                 
                  if(finQuantum2)
                 {
                    
                    contador = (contador == (colaRR.Count - 1)) ? 0 : ++contador; // Si ya llegamos al final
                    //Devuelvase al inicio de la cola, si no, sume 1
                  
                    hilo_a_ejecutar[1] = contador;
                 
                 }
                 
               

                **/



                WaitHandle.WaitAll(banderas_nucleos_controlador);

                


                ++reloj;

                textoInterfaz = "El reloj es: " + reloj + "\n" + "El núcleo 1 ejecuta el hilo: "
                    + hilo_a_ejecutar[0] + "\n El núcleo 2 ejecuta el hilo: " 
                    + hilo_a_ejecutar[1];



                bandera_controlador_nucleo1.Set();
                bandera_controlador_nucleo2.Set();
                
            }
            


            

            
        }

        public void nucleo_1()
        {
            int quantum = 0 ;
            int [] instruccion = new int[4];
            bool aciertoCache = false;
            bool busOcupado = true;
            while (!finPrograma)
            {
                bandera_controlador_nucleo1.WaitOne();
                
                    int dirHilo = hilo_a_ejecutar[0];
                    int numHilo1 = mapaContexto[hilo_a_ejecutar[0]];  //se guarda en numHilo1 el número de hilo que le toca ejecutar
                    for (int i = 0; i < 32; i++)    //recupera el contexto (falta PC)
                    {
                        registro_nucleo1[i] = PCB[numHilo1, i];
                    }
                    PC1 = PCB[numHilo1, 32];
                    while (quantum != 0 && !fin_hilos[numHilo1])    //mientras tenga quantum y no haya terminado el hilo
                    {
                        bandera_controlador_nucleo1.WaitOne();
                        int numBloque = PC1 / 4;    //calcula el número de bloque en el que está la siguiente instrucción
                        int i = 0;
                        while (i < numHilos && !aciertoCache)    //busca el bloque en caché
                        {
                            if (numBloque == bloques_cache_instrucciones_nucleo1[i])
                            {
                                aciertoCache = true;
                            }
                        }

                        
                        if (!aciertoCache)
                        {
                            while (busOcupado)
                            {
                                if (Monitor.TryEnter(bus))
                                {
                                    try
                                    {
                                        /** TODO: Aquí va el fallo de caché **/
                                        int offset = 0;
                                        for (int n = 0; n < 4; n++)
                                        {
                                            for (int m = 0; m < 4; m++)
                                            {
                                                bus[m] = RAM[numBloque * 16 + m + offset];
                                            }

                                            for (int m = 0; m < 4; m++)
                                            {
                                                cache_instrucciones_nucleo1[numBloque % 8, m + offset] = (int) bus[m];
                                            }

                                            offset += 4;
                                        }

                                        bloques_cache_instrucciones_nucleo1[numBloque % 8] = numBloque;

                                        for (int t = 0; t < (8 * tiempoTransferencia + 4 * tiempoLecturaEscritura); t++)
                                        {
                                            bandera_nucleo1_controlador.Set();
                                            bandera_controlador_nucleo1.WaitOne();
                                        }

                                        
                                        busOcupado = false;
                                    }
                                    finally
                                    {
                                        Monitor.Exit(bus);
                                    }
                                }
                                else
                                {
                                    bandera_nucleo1_controlador.Set();
                                    bandera_controlador_nucleo1.WaitOne();
                                }
                            }
                        }

                        int numInstruccion = PC1 % 4;
                        for (int j = numInstruccion; j < numInstruccion + 4; j++)
                        {
                            instruccion[j] = cache_instrucciones_nucleo1[i, j];
                        }
                        PC1 += 4;
                        ejecutarInstruccion1(ref instruccion, numHilo1);
                        quantum--;

                        if (quantum == 0 || !fin_hilos[numHilo1])
                        {
                            for (int k = 0; k < 32; k++)    //guarda el contexto (falta PC)
                            {
                                PCB[numHilo1, k] = registro_nucleo1[k];
                            }
                            PCB[numHilo1, 32] = PC1;
                        }

                        bandera_nucleo1_controlador.Set();
                    }
                    
                

                
            }
        }


        public void nucleo_2()
        {
            int quantum = 0;
            int[] instruccion = new int[4];
            bool aciertoCache = false;
            bool busOcupado = true;
            while (!finPrograma)
            {
                bandera_controlador_nucleo2.WaitOne();

                int dirHilo = hilo_a_ejecutar[1];
                int numHilo2 = mapaContexto[hilo_a_ejecutar[1]];  //se guarda en numHilo1 el número de hilo que le toca ejecutar
                for (int i = 0; i < 32; i++)    //recupera el contexto (falta PC)
                {
                    registro_nucleo2[i] = PCB[numHilo2, i];
                }
                PC2 = PCB[numHilo2, 32];
                while (quantum != 0 && !fin_hilos[numHilo2])    //mientras tenga quantum y no haya terminado el hilo
                {
                    bandera_controlador_nucleo2.WaitOne();
                    int numBloque = PC2 / 4;    //calcula el número de bloque en el que está la siguiente instrucción
                    int i = 0;
                    while (i < numHilos && !aciertoCache)    //busca el bloque en caché
                    {
                        if (numBloque == bloques_cache_instrucciones_nucleo2[i])
                        {
                            aciertoCache = true;
                        }
                    }


                    if (!aciertoCache)
                    {
                        while (busOcupado)
                        {
                            if (Monitor.TryEnter(bus))
                            {
                                try
                                {
                                    /** TODO: Aquí va el fallo de caché **/
                                    int offset = 0;
                                    for (int n = 0; n < 4; n++)
                                    {
                                        for (int m = 0; m < 4; m++)
                                        {
                                            bus[m] = RAM[numBloque * 16 + m + offset];
                                        }

                                        for (int m = 0; m < 4; m++)
                                        {
                                            cache_instrucciones_nucleo2[numBloque % 8, m + offset] = (int)bus[m];
                                        }

                                        offset += 4;
                                    }

                                    bloques_cache_instrucciones_nucleo2[numBloque % 8] = numBloque;

                                    for (int t = 0; t < (8 * tiempoTransferencia + 4 * tiempoLecturaEscritura); t++)
                                    {
                                        bandera_nucleo2_controlador.Set();
                                        bandera_controlador_nucleo2.WaitOne();
                                    }


                                    busOcupado = false;
                                }
                                finally
                                {
                                    Monitor.Exit(bus);
                                }
                            }
                            else
                            {
                                bandera_nucleo2_controlador.Set();
                                bandera_controlador_nucleo2.WaitOne();
                            }
                        }
                    }

                    int numInstruccion = PC2 % 4;
                    for (int j = numInstruccion; j < numInstruccion + 4; j++)
                    {
                        instruccion[j] = cache_instrucciones_nucleo2[i, j];
                    }
                    PC2 += 4;
                    ejecutarInstruccion2(ref instruccion, numHilo2);
                    quantum--;
                    if (quantum == 0 || !fin_hilos[numHilo2])
                    {
                        for (int k = 0; k < 32; k++)    //guarda el contexto (falta PC)
                        {
                            PCB[numHilo2, k] = registro_nucleo2[k];
                        }
                        PCB[numHilo2, 32] = PC2;
                    }
                    bandera_nucleo2_controlador.Set();
                }




            }
        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            button2.Visible = true;
            modoLento = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            button2.Visible = false;
            modoLento = false;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            
            OpenFileDialog archivador = new OpenFileDialog();
          
            System.IO.StreamReader sr;

            archivador.Multiselect = true; //permite seleccionar mas de un archivo
            archivador.Title = "Seleccione los hilos";
            archivador.Filter = "All files(*.txt)|*.txt";

            if (archivador.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                cargarRAM(ref archivador);
            }

        }

        private void cargarRAM(ref OpenFileDialog archivador )
        {
            int puntero = 0; //puntero que indica la posicion en la que debe guardarse el siguiente entero en la RAM
            int contador = 0; //Número de hilo lógico [0,1,2,3]
            String linea;
            int[] temporal;

            System.IO.StreamReader sr;

            foreach (String file in archivador.FileNames)
            {
                ++contador;

                colaRR.Add(puntero); //Agregamos a la lista de Round Robin el id del hilo
                //En este caso usaremos como id del hilo, la direccion en memoria
                //donde inicia el hilo

                mapaContexto.Add(puntero, contador); //<ID del hilo, # lógico de hilo>

                sr = new System.IO.StreamReader(file);

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

                sr.Close();

            }


            if ((int.Parse(textBox1.Text) != contador) && (contador > 0))
            {
                textBox1.Text = "" + contador;

                MessageBox.Show("El parámetro que usted especificó para la cantidad de hilos " +
                "no coincide con la cantidad de archivos que escogió, pero lo hemos cambiado, no se preocupe.",
                "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }

            archivosCargados = true; //Indicamos que ya los archivos se cargaron
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = false;

            if(textBox1.Text != "")
            {
                button1.Enabled = true;
            }
        }

        /**
         * Carga todos los datos de la interfaz
         **/
        private void button3_Click(object sender, EventArgs e)
        {
            if (archivosCargados && (textBox1.Text != "") && (textBox2.Text != "") && (textBox3.Text != "") && (textBox4.Text != ""))
            {
                 numHilos = int.Parse(textBox1.Text);
                PCB = new int[numHilos, 33]; //Inicializamos el PCB (Process Control Block)
                fin_hilos = new bool[numHilos];

                quatum = int.Parse(textBox4.Text);

                tiempoLecturaEscritura = int.Parse(textBox2.Text);// Este es el valor de b que menciona el enunciado

                tiempoTransferencia = int.Parse(textBox3.Text); //Este es el valor de m que menciona el enunciado.

                Thread control = new Thread(new ThreadStart(controlador)); //Iniciamos el controlador (Scheduler)
                control.Start();

                actualizarInterfaz();

            }
            else
            {
                MessageBox.Show("¡Espera!...\nAún no has cargado suficientes datos.\n"+
                    "Revisa que los textbox estén llenos y los hilos ya hayan sido cargados a memoria.","Hilos",MessageBoxButtons.OK,MessageBoxIcon.Stop);    
            }

        }

        private void actualizarInterfaz()
        {

            while(true)
            {
                richTextBox1.AppendText(textoInterfaz);

                Application.DoEvents();
              
            } 
            
        }

        private void ejecutarInstruccion1(ref int[] ins, int numHilo)
        {
            switch (ins[0])
            {
                case 8: //ADDI
                    registro_nucleo1[ins[2]] = registro_nucleo1[ins[1]] + ins[3];
                    break;

                case 32: //ADD
                    registro_nucleo1[ins[3]] = registro_nucleo1[ins[1]] + registro_nucleo1[ins[2]];
                    break;

                case 34: //SUB
                    registro_nucleo1[ins[3]] = registro_nucleo1[ins[1]] - registro_nucleo1[ins[2]];
                    break;

                case 12: //MUL
                    registro_nucleo1[ins[3]] = registro_nucleo1[ins[1]] * registro_nucleo1[ins[2]];
                    break;

                case 14: //DIV
                    registro_nucleo1[ins[3]] = registro_nucleo1[ins[1]] / registro_nucleo1[ins[2]];
                    break;

                case 4: //BEQZ
                    if (registro_nucleo1[ins[1]] == 0)
                    {
                        PC1 = ins[3];
                    }
                    break;

                case 5: //BNEZ
                    if (registro_nucleo1[ins[1]] != 0)
                    {
                        PC1 = ins[3];
                    }
                    break;

                case 3: //JAL
                    registro_nucleo1[31] = PC1;
                    PC1 += ins[3];
                    break;

                case 2: //JR
                    PC1 = registro_nucleo1[ins[1]];
                    break;

                case 63: //FIN
                    fin_hilos[numHilo] = true;
                    break;
            }
        }

        private void ejecutarInstruccion2(ref int[] ins, int numHilo)
        {
            switch (ins[0])
            {
                case 8: //ADDI
                    registro_nucleo2[ins[2]] = registro_nucleo2[ins[1]] + ins[3];
                    break;

                case 32: //ADD
                    registro_nucleo2[ins[3]] = registro_nucleo2[ins[1]] + registro_nucleo2[ins[2]];
                    break;

                case 34: //SUB
                    registro_nucleo2[ins[3]] = registro_nucleo2[ins[1]] - registro_nucleo2[ins[2]];
                    break;

                case 12: //MUL
                    registro_nucleo2[ins[3]] = registro_nucleo2[ins[1]] * registro_nucleo2[ins[2]];
                    break;

                case 14: //DIV
                    registro_nucleo2[ins[3]] = registro_nucleo2[ins[1]] / registro_nucleo2[ins[2]];
                    break;

                case 4: //BEQZ
                    if (registro_nucleo2[ins[1]] == 0)
                    {
                        PC2 = ins[3];
                    }
                    break;

                case 5: //BNEZ
                    if (registro_nucleo2[ins[1]] != 0)
                    {
                        PC2 = ins[3];
                    }
                    break;

                case 3: //JAL
                    registro_nucleo2[31] = PC2;
                    PC2 += ins[3];
                    break;

                case 2: //JR
                    PC2 = registro_nucleo1[ins[1]];
                    break;

                case 63: //FIN
                    fin_hilos[numHilo] = true;
                    break;
            }
        }

        

    }
}
