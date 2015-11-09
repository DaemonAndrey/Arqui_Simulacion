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

        private int[] RAMInstrucciones;
        private int[] RAMDatos;


        private int[] registro_nucleo1;
        private int[] registro_nucleo2;

        private int[,] cache_datos_nucleo1;
        private int[,] cache_datos_nucleo2;


        private int[,] cache_instrucciones_nucleo1;
        private int[,] cache_instrucciones_nucleo2;


        private int[] hilo_a_ejecutar; //indica cual hilo va a ejecutar cada núcleo

        private bool[] fin_hilos; //indica cuales hilos ya terminaron

        private long reloj;

        private bool finPrograma;
        private bool nucleo1Activo;
        private bool nucleo2Activo;

        private int[,] PCB; //Estructura que guarda los contextos de los hilos

        private int PC1; // Prorgram counter del núcleo 1
        private int PC2; // Prorgram counter del núcleo 1


        private int quantum;  //Guarda el quantum que el usuario especifica en interfaz

        private int tiempoLecturaEscritura; 
        private int tiempoTransferencia;
        private int delay;
        private int numHilos;

        private int quantum1; //Guarda el quantum local del hilo 1
        private int quantum2; 

        private WaitHandle[] banderas_nucleos_controlador; // Array de semáforo de los núcleos al controlador

        private AutoResetEvent bandera_nucleo1_controlador; //Semáforo del núcleo 1 al controlador
        private AutoResetEvent bandera_nucleo2_controlador; //Semáforo del núcleo 2 al controlador

        private AutoResetEvent bandera_controlador_nucleo1; //Semáforo del controlador al núcleo 1
        private AutoResetEvent bandera_controlador_nucleo2; //Semáforo del controlador al núcleo 2



        private AutoResetEvent bandera_agregar_registros; //Semáforo para controlar la escritura de los núcleos

        private ArrayList bus; //Bus de instrucciones
        private ArrayList busDatos;

        private Queue<int> robin; //Cola de round robin

        private string textoInterfaz;

        private string textoFinal;

        public Form1()
        {
            InitializeComponent();

            button1.Enabled = false;

            textoInterfaz = "";

            textoFinal = "";

            robin = new Queue<int>();

            delay = 0;

            finPrograma = false; //bandera para finalizar el programa
            nucleo1Activo = true;
            nucleo2Activo = true;


            RAMInstrucciones = new int[640];
            RAMDatos = new int[352];

            //Inicializar RAM de Datos
            for (int i = 0; i < 352; i++)
            {
                RAMDatos[i] = 1;
            }

            registro_nucleo1 = new int[32];
            registro_nucleo2 = new int[32];

            cache_datos_nucleo1 = new int[8, 8];
            cache_datos_nucleo2 = new int[8, 8];

            //Inicializar cachés de Datos en 0, etiqueta en -1 y banderas apagadas
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    cache_datos_nucleo1[i,j] = 0;
                }
                cache_datos_nucleo1[i, 4] = -1;
                cache_datos_nucleo1[i, 5] = 0;
                cache_datos_nucleo1[i, 6] = 0;
                cache_datos_nucleo1[i, 7] = 0;
            }

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    cache_datos_nucleo2[i, j] = 0;
                }
                cache_datos_nucleo2[i, 4] = -1;
                cache_datos_nucleo2[i, 5] = 0;
                cache_datos_nucleo2[i, 6] = 0;
                cache_datos_nucleo2[i, 7] = 0;
            }

            cache_instrucciones_nucleo1 = new int[8, 16];
            cache_instrucciones_nucleo2 = new int[8, 16];



            hilo_a_ejecutar = new int[2];

            

            bus = new ArrayList(4);

            bus.Add(0);
            bus.Add(1);
            bus.Add(2);
            bus.Add(3);

            busDatos = new ArrayList(4);

            busDatos.Add(0);
            busDatos.Add(1);
            busDatos.Add(2);
            busDatos.Add(3);

            banderas_nucleos_controlador = new WaitHandle[2];

            //El true permite que la primera vez que hace wait, siga adelante
            bandera_nucleo1_controlador = new AutoResetEvent(true); 
            bandera_nucleo2_controlador = new AutoResetEvent(true);

            banderas_nucleos_controlador[0] = bandera_nucleo1_controlador;
            banderas_nucleos_controlador[1] = bandera_nucleo2_controlador;

            //El false permite que la primera vez que hace wait se detenga
            bandera_controlador_nucleo1 = new AutoResetEvent(false);
            bandera_controlador_nucleo2 = new AutoResetEvent(false);

            //El true permita que la primera vez que hace wait, siga adelante
            bandera_agregar_registros = new AutoResetEvent(true);

        }





        public void controlador()
        {
            //Agregamos los hilos al round robin
            for (int i = 0; i < numHilos; ++i )
            {
                robin.Enqueue(i);
            }
            





            //Creamos e inicializamos los hilos de los núcleos
            Thread nucleo1 = new Thread(new ThreadStart(nucleo_1)); 
            nucleo1.Name = "nucleo1";
            nucleo1.Start(); //Los ponemos a correr 
            Thread nucleo2 = new Thread(new ThreadStart(nucleo_2));
            nucleo2.Name = "nucleo2";
            nucleo2.Start();
            
            reloj = 0;

            quantum1 = 0;
            quantum2 = 0;

            

            while(!finPrograma)
            {
                 WaitHandle.WaitAll(banderas_nucleos_controlador);

                //Si se vencio el quantum, el hilo terminó y el núcleo continúa activo es hora de asignar otro hilo
                 if((quantum1 == 0 || fin_hilos[hilo_a_ejecutar[0]]) && nucleo1Activo)
                 {
                     
                     if (robin.Count() != 0) //Si es distinto de 0, aún quedan hilos por procesar
                     {

                         hilo_a_ejecutar[0] = robin.Dequeue();

                         quantum1 = quantum;

                     }
                     else 
                     {
                         nucleo1Activo = false;


                         if (!nucleo2Activo)
                         {
                             finPrograma = true; //Si ya los dos núcleos están inactivos, terminó la simulación
                         } 

                     }
                    
                     
                 }
                 
                 if((quantum2 == 0 || fin_hilos[hilo_a_ejecutar[1]]) && nucleo2Activo)
                 {

                     if (robin.Count() != 0) //Si es distinto de 0, aún quedan hilos por procesar
                     {

                         hilo_a_ejecutar[1] = robin.Dequeue();
                         quantum2 = quantum;

                     }
                     else
                     {
                         nucleo2Activo = false;

                         if (!nucleo1Activo)
                         {
                             finPrograma = true; //Si ya los dos núcleos están inactivos, terminó la simulación                             
                         } 
                     }



                 }




                textoInterfaz = "El reloj es: " + reloj + "\n" + "El núcleo 1 ejecuta el hilo: "
                    + hilo_a_ejecutar[0] + "\n El núcleo 2 ejecuta el hilo: " 
                    + hilo_a_ejecutar[1]+"\n";

                textoInterfaz += "Los registros del núcleo 1 contienen: \n";

                
                for (int i = 0; i < 32; ++i )
                {
                    textoInterfaz += registro_nucleo1[i] + ", ";
                }

                textoInterfaz += "\n Los registros del núcleo 2 contienen: \n";

                for (int i = 0; i < 32; ++i)
                {
                    textoInterfaz += registro_nucleo2[i] + ", ";
                }
                
                textoInterfaz += "\n" ;

                ++reloj;

                

                bandera_controlador_nucleo1.Set();
                bandera_controlador_nucleo2.Set();
             
            }
            
        }

        public void nucleo_1()
        {

            ArrayList bloques_cargados = new ArrayList(); //Lleva la cuenta de los bloques cargados

            for (int i = 0; i < 8; ++i )
            {
                bloques_cargados.Add(-1); //Lo inicializamos en -1 para asegurar que cuando empieza a ejecutarse no haya ningún bloque
            }

            int numInstruccion;
            int [] instruccion = new int[4]; //Guarda la instrucción cargada
            bool busOcupado = false; 
            int numHilo1; //Hilo actual
            int numBloque;// Número de bloque a cargar
            int offset = 0;


            while (nucleo1Activo)
            {

                Thread.Sleep(delay);
                bandera_controlador_nucleo1.WaitOne();
                numHilo1 = hilo_a_ejecutar[0]; //pedimos el hilo seleccionado por round robin
  
                    for (int i = 0; i < 32; ++i)    //recupera el contexto (falta PC)
                    {
                        
                        registro_nucleo1[i] = PCB[numHilo1, i];
                    }

                    PC1 = PCB[numHilo1, 32]; //Recuperamos el contexto

                    while ( (quantum1 != 0) && !fin_hilos[numHilo1])    //mientras tenga quantum y no haya terminado el hilo
                    {

                        numBloque = (int) Math.Floor((double)PC1 / 16);    //calcula el número de bloque en el que está la siguiente instrucción



                        if (!bloques_cargados.Contains(numBloque))
                        {
                            while (!busOcupado)
                            {
                                if (Monitor.TryEnter(bus)) //Esto funciona como un lock
                                {
                                    try
                                    {
                                        /** fallo de caché **/

                                        busOcupado = true;
                                        offset = 0;
                                        for (int n = 0; n < 4; ++n)
                                        { //Cargamos de la RAM
                                            for (int m = 0; m < bus.Count; ++m)
                                            {
                                                bus[m] = RAMInstrucciones[numBloque * 16 + m + offset];
                                            }

                                            //Cargamos a la caché de instrucciones
                                            for (int m = 0; m < 4; m++)
                                            {
                                                cache_instrucciones_nucleo1[numBloque % 8, m + offset] = (int)bus[m];
                                            }

                                            offset += 4;
                                        }

                                        //Grabamos el número de bloque en la estructura
                                        bloques_cargados[numBloque % 8] = numBloque;

                                        for (int t = 0; t < (8 * tiempoTransferencia + 4 * tiempoLecturaEscritura); t++)
                                        {
                                            //Simulamos el tiempo de transferencia y escritura
                                            bandera_nucleo1_controlador.Set();
                                            bandera_controlador_nucleo1.WaitOne();
                                        }



                                    }
                                    finally
                                    {
                                        Monitor.Exit(bus); //Habilitamos el bus

                                    }
                                }
                                else
                                {
                                    //Si el bus está ocupado, dejamos actualizar el reloj.
                                    bandera_nucleo1_controlador.Set();
                                    bandera_controlador_nucleo1.WaitOne();
                                }
                            }
                            busOcupado = false;
                        }


                        numInstruccion = PC1 % 16; //Calculamos el número de instrucción
                        for (int j = numInstruccion; j < numInstruccion + 4; ++j)
                        {
                            instruccion[j % 4] = cache_instrucciones_nucleo1[numBloque % 8, j];
                        }
                        
                        PC1 += 4; 
                        ejecutarInstruccion1(ref instruccion, numHilo1); // mandamos a ejecutar la instrucción

                        quantum1--;

                        if (quantum1 == 0 && !fin_hilos[numHilo1])
                        {
                            for (int k = 0; k < 32; ++k)    //guarda el contexto (falta PC)
                            {
                                PCB[numHilo1, k] = registro_nucleo1[k];
                            }
                            PCB[numHilo1, 32] = PC1;

                            robin.Enqueue(numHilo1); //Si el hilo aún no se ha terminado de ejecutar,
                                                     //lo volvemos a encolar para calendarizarlo

                        }


                        if (quantum1 != 0 && !fin_hilos[numHilo1])
                        {
                            bandera_nucleo1_controlador.Set();
                            bandera_controlador_nucleo1.WaitOne();
                        }
                    }

                    bandera_nucleo1_controlador.Set();
               
            }

            
            while (!finPrograma)
            {
                bandera_nucleo1_controlador.Set();
                bandera_controlador_nucleo1.WaitOne();
            }
             
        }

        /**
         * Véase los comentarios del núcleo 1, ya que ambos son iguales
        **/
        public void nucleo_2()
        {
            ArrayList bloques_cargados = new ArrayList();

            for (int i = 0; i < 8; ++i)
            {
                bloques_cargados.Add(-1);
            }

            int numHilo2;
            int numInstruccion;
            int[] instruccion = new int[4];
            int numBloque;
            int offset = 0;
            bool busOcupado = false;

            while (nucleo2Activo)
            {
                Thread.Sleep(delay);

                bandera_controlador_nucleo2.WaitOne();

                numHilo2 = hilo_a_ejecutar[1];


                for (int i = 0; i < 32; ++i)    //recupera el contexto (falta PC)
                {
                    registro_nucleo2[i] = PCB[numHilo2, i];
                }
                PC2 = PCB[numHilo2, 32];

                while ((quantum2 != 0) && !fin_hilos[numHilo2])    //mientras tenga quantum y no haya terminado el hilo
                {
                


                    numBloque = (int)Math.Floor((double)PC2 / 16);    //calcula el número de bloque en el que está la siguiente instrucción

                    if (!bloques_cargados.Contains(numBloque))
                    {
                        while (!busOcupado)
                        {
                            if (Monitor.TryEnter(bus))
                            {
                                try
                                {
                                    //Fallo de caché
                                    busOcupado = true;
                                    offset = 0;
                                    for (int n = 0; n < 4; ++n)
                                    {
                                        for (int m = 0; m < 4; ++m)
                                        {
                                            bus[m] = RAMInstrucciones[numBloque * 16 + m + offset];
                                        }

                                        for (int m = 0; m < 4; ++m)
                                        {
                                            cache_instrucciones_nucleo2[numBloque % 8, m + offset] = (int)bus[m];
                                        }

                                        offset += 4;
                                    }

                                    bloques_cargados[numBloque % 8] = numBloque;

                                    for (int t = 0; t < (8 * tiempoTransferencia + 4 * tiempoLecturaEscritura); t++)
                                    {
                                        bandera_nucleo2_controlador.Set();
                                        bandera_controlador_nucleo2.WaitOne();
                                    }



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
                        busOcupado = false;
                    }


                    numInstruccion = PC2 % 16;
                    for (int j = numInstruccion; j < (numInstruccion + 4); ++j)
                    {
                        instruccion[j % 4] = cache_instrucciones_nucleo2[numBloque % 8, j];
                    }
                    
                    PC2 += 4;
                    ejecutarInstruccion2(ref instruccion, numHilo2);
                    quantum2--;

                    if (quantum2 == 0 && !fin_hilos[numHilo2])
                    {
                        for (int k = 0; k < 32; ++k)    //guarda el contexto (falta PC)
                        {
                            PCB[numHilo2, k] = registro_nucleo2[k];

                        }
                        PCB[numHilo2, 32] = PC2;
                        robin.Enqueue(numHilo2); // Si aún no ha terminado,
                        //lo volvemos a calendarizar para volverlo a calendarizar


                    }


                    if (quantum2 != 0 && !fin_hilos[numHilo2])
                    {
                        bandera_nucleo2_controlador.Set();
                        bandera_controlador_nucleo2.WaitOne();
                    }
                }

                bandera_nucleo2_controlador.Set();
            }

            
            while (!finPrograma)
            {
                bandera_nucleo2_controlador.Set();
                bandera_controlador_nucleo2.WaitOne();
            }
            
        }



        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            delay = 15;
        }

        /**
         * Permite cargar los archivos
         **/
        private void button1_Click(object sender, EventArgs e)
        {
            
            OpenFileDialog archivador = new OpenFileDialog();
          
            

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
            int contador = -1; //Número de hilo lógico [0,1,2,3]
            String linea;
            int[] temporal;

            System.IO.StreamReader sr;

            foreach (String file in archivador.FileNames)
            {
                ++contador;

                PCB[contador, 32] = puntero; //Agregamos a la lista de Round Robin el id del hilo
                //En este caso usaremos como id del hilo, la direccion en memoria
                //donde inicia el hilo

                sr = new System.IO.StreamReader(file);

                linea = sr.ReadLine();
                while ((linea != null) && (linea != ""))
                {

                    temporal = linea.Split(' ').Select(int.Parse).ToArray();

                    for (int i = 0; i < 4; ++i, ++puntero)
                    {
                        RAMInstrucciones[puntero] = temporal[i];
                    }

                    linea = sr.ReadLine();

                }

                sr.Close();

            }


            Thread control = new Thread(new ThreadStart(controlador)); //Iniciamos el controlador (Scheduler)
            control.Start();

            actualizarInterfaz();
        }


        /**
         * Carga todos los datos de la interfaz
         **/
        private void button3_Click(object sender, EventArgs e)
        {
            if ((textBox1.Text != "") && (textBox2.Text != "") && (textBox3.Text != "") && (textBox4.Text != ""))
            {
                numHilos = int.Parse(textBox1.Text);
                PCB = new int[numHilos, 33]; //Inicializamos el PCB (Process Control Block)
                fin_hilos = new bool[numHilos];

                quantum = int.Parse(textBox4.Text);

                tiempoLecturaEscritura = int.Parse(textBox2.Text);// Este es el valor de b que menciona el enunciado

                tiempoTransferencia = int.Parse(textBox3.Text); //Este es el valor de m que menciona el enunciado.

                for (int i = 0; i < fin_hilos.Length; ++i)
                {
                    fin_hilos[i] = false;
                }



                button1.Enabled = true;

            }
            else
            {
                MessageBox.Show("¡Espera!...\nAún no has cargado suficientes datos.\n"+
                    "Revisa que los textbox estén llenos y los hilos ya hayan sido cargados a memoria.","Hilos",MessageBoxButtons.OK,MessageBoxIcon.Stop);    
            }

        }

        private void actualizarInterfaz()
        {

            while(!finPrograma)
            {
                Thread.Sleep(delay);

                richTextBox1.AppendText(textoInterfaz);

                Application.DoEvents();
                
            }

            textoFinal += "El contenido de la memoria RAM es: \n\n";
            for (int i = 0; i < 2048; ++i )
            {
                textoFinal += RAMInstrucciones[i]+ ", ";
            }

            richTextBox1.Clear();

            richTextBox1.AppendText(textoFinal);
            
        }

        /**
         * Cada vez que un hilo termina, agregamos sus regristros a un string que se imprimirá al final.
         **/
        private void agregarRegistros(ref int[] registros, int hilo, int nucleo)
        {

            bandera_agregar_registros.WaitOne();

            textoFinal += "Hilo: "+hilo+" Nucleo: "+nucleo+"\n";
            textoFinal += "Registros: \n";
            for (int i = 0; i < 32; ++i )
            {
                textoFinal += "R" + i + " = " + registros[i]+"\n";
            }

            textoFinal += "\n\n";

            bandera_agregar_registros.Set();
        }

        private bool buscarEnCacheDatos1(int numBloque)
        {
            bool encontrado = false;
            int mapeo = numBloque % 8;

            if ((numBloque == cache_datos_nucleo1[mapeo, 4]) && (cache_datos_nucleo1[mapeo, 5] != -1))
            {
                encontrado = true;
            }

            return encontrado;
        }

        private void resolverFalloCacheDatos1(int direccion)
        {
            //Calcular número de bloque
            int bloque = (int)(Math.Floor((float)direccion / 16));
            int numDato = direccion % 4;
            bool tengoBus = false;
            bool encontradoEnOtraCache = false;
            bool traerDeMemoria = true;
            while (!tengoBus)
            {
                //Reservar caché propia
                if (Monitor.TryEnter(cache_datos_nucleo1))
                {
                    try
                    {
                        //Reservar bus
                        if (Monitor.TryEnter(busDatos))
                        {
                            try
                            {
                                //Reservar otra caché
                                if (Monitor.TryEnter(cache_datos_nucleo2))
                                {
                                    try
                                    {
                                        //Buscar bloque en la otra caché
                                        int i = 0;
                                        while (i < 8 && !encontradoEnOtraCache)
                                        {
                                            if (cache_datos_nucleo2[i, 4] == bloque)
                                            {
                                                encontradoEnOtraCache = true;
                                            }
                                            i++;
                                        }
                                        i--;

                                        //Si se encontró, revisar si está modificado
                                        //Los bloques de caché de datos miden lo mismo tanto en MIPS como en nuestra simulación (128 bits)
                                        if (encontradoEnOtraCache)
                                        {
                                            if (cache_datos_nucleo2[i, 6] == 1)
                                            {
                                                traerDeMemoria = false;
                                            }
                                        }

                                        //Si está modificado, traer el bloque de la otra caché
                                        if (!traerDeMemoria)
                                        {
                                            WriteBackNucleo1(false, bloque, i);
                                        }

                                    }

                                    finally
                                    {
                                        //Liberar caché del otro núcleo
                                        Monitor.Exit(cache_datos_nucleo2);
                                    }
                                }

                                //Si el bloque nuevo no está en la otra caché o no está modificado
                                if (traerDeMemoria)
                                {
                                    WriteBackNucleo1(true, bloque, bloque % 8);
                                }
                            }

                            finally
                            {
                                //Liberar bus
                                Monitor.Exit(busDatos);
                            }
                        }
                    }

                    finally
                    {
                        //Liberar caché propia
                        Monitor.Exit(cache_datos_nucleo1);
                    }
                }
                else
                {
                    bandera_nucleo1_controlador.Set();
                    bandera_controlador_nucleo1.WaitOne();
                }
            }
        }


        /**
         * El booleano memoria indica si hay que traer el bloque de memoria (true) o de la otra caché (false)
         **/
        private void WriteBackNucleo1(bool memoria, int bloqueNuevo, int indice)
        {
            //Si el bloque viejo está modificado, devolverlo a memoria
            if (cache_datos_nucleo1[indice, 6] == 1)
            {
                int bloqueViejo = cache_datos_nucleo1[indice, 4];
                int direccion = bloqueViejo * 4;
                for (int i = 0; i < 4; i++)
                {
                    RAMDatos[direccion + i] = cache_datos_nucleo1[indice, i];
                }

                //Esperar 4(2b + m) ciclos
                for (int j = 0; j < (8 * tiempoTransferencia + 4 * tiempoLecturaEscritura); j++)
                {
                    bandera_nucleo1_controlador.Set();
                    bandera_controlador_nucleo1.WaitOne();
                }
            }

            //Traer bloque nuevo de memoria
            if (memoria)
            {
                int direccion = bloqueNuevo * 4;
                for (int i = 0; i < 4; i++)
                {
                    cache_datos_nucleo1[indice, i] = RAMDatos[direccion + i];
                }

                //Esperar 4(2b + m) ciclos
                for (int j = 0; j < (8 * tiempoTransferencia + 4 * tiempoLecturaEscritura); j++)
                {
                    bandera_nucleo1_controlador.Set();
                    bandera_controlador_nucleo1.WaitOne();
                }
            }
            //Traer bloque nuevo de la otra caché
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    cache_datos_nucleo1[indice, i] = cache_datos_nucleo2[indice, i];
                }
            }

            //Reiniciar banderas y actualizar etiqueta
            cache_datos_nucleo1[indice, 4] = bloqueNuevo;
            cache_datos_nucleo1[indice, 5] = 1;
            cache_datos_nucleo1[indice, 6] = 0;
            cache_datos_nucleo1[indice, 7] = 0;
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
                        PC1 += ins[3] * 4;

                    }
                    break;

                case 5: //BNEZ
                    if (registro_nucleo1[ins[1]] != 0)
                    {
                        PC1 += ins[3] * 4;

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

                    agregarRegistros(ref registro_nucleo1, numHilo, 1);
                    break;
                    
                case 35: //LW
                    bool enCache;
                    enCache = buscarEnCacheDatos1(registro_nucleo1[ins[1]] + ins[3]);
                    if (!enCache)
                    {
                        resolverFalloCacheDatos1(registro_nucleo1[ins[1]] + ins[3]);
                    }


                    break;

                case 43: //SW


                    break;

                case 50: //LL


                    break;

                case 51: //SC


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
                        PC2 += ins[3] * 4;

                    }
                    break;

                case 5: //BNEZ
                    if (registro_nucleo2[ins[1]] != 0)
                    {
                        PC2 += ins[3] * 4;

                    }
                    break;

                case 3: //JAL
                    registro_nucleo2[31] = PC2;
                    PC2 += ins[3];

                    break;

                case 2: //JR

                    PC2 = registro_nucleo2[ins[1]];

                    break;

                case 63: //FIN
                    fin_hilos[numHilo] = true;

                    agregarRegistros(ref registro_nucleo2, numHilo, 2);
                    break;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            delay = 0;
        }

        

    }
}
