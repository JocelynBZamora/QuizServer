using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using QuizServer.Model;
using System.Text.Json;

namespace QuizServer.Service
{
    public class ServidorUdpService
    {
        public event Action<string>? AlTenerError;
        public event Action? AlTenerDetenerse;
        public event Action? AlIniciar;
        public event Action<string>? AlRecibirMensaje;
        public event Action<IPEndPoint>? AlNuevoCliente;
        public List<IPEndPoint> ClientesRegistrados { get; } = new();
        public string IP { get; private set; }

        private UdpClient? servidorUdp;
        private Thread? hiloEscucha;
        private bool enEjecucion;

        private readonly HashSet<string> nombresRegistrados = new();
        private bool respuestasHabilitadas = false;
        private readonly int puerto = 65000;

        public ServidorUdpService()
        {
            IPAddress[] ipAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            IP = ipAddresses
                .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)?
                .ToString() ?? "0.0.0.0";
        }

        /// <summary>
        /// Inicia el servidor y comienza a escuchar clientes.
        /// </summary>
        public void Iniciar()
        {
            if (enEjecucion) return;

            enEjecucion = true;

            hiloEscucha = new Thread(Escuchar)
            {
                IsBackground = true
            };
            hiloEscucha.Start();
        }

        /// <summary>
        /// Detiene el servidor de forma segura.
        /// </summary>
        public void Detener()
        {
            try
            {
                enEjecucion = false;
                servidorUdp?.Close();
                hiloEscucha?.Join(500);
                AlTenerDetenerse?.Invoke();
            }
            catch (Exception ex)
            {
                AlTenerError?.Invoke($"Error al detener servidor: {ex.Message}");
            }
        }

        private void Escuchar()
        {
            try
            {
                servidorUdp = new UdpClient(puerto)
                {
                    EnableBroadcast = true
                };

                AlIniciar?.Invoke();

                while (enEjecucion)
                {
                    try
                    {
                        IPEndPoint cliente = new IPEndPoint(IPAddress.Any, 0);
                        byte[] datos = servidorUdp.Receive(ref cliente);
                        string mensaje = Encoding.UTF8.GetString(datos);

                        // Notificar al ViewModel que llegó un nuevo cliente
                        AlNuevoCliente?.Invoke(cliente);

                        ProcesarMensaje(mensaje, cliente);
                        AlRecibirMensaje?.Invoke(mensaje);

                    }
                    catch (SocketException ex)
                    {
                        if (enEjecucion)
                            AlTenerError?.Invoke($"Socket error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        AlTenerError?.Invoke($"Error al recibir mensaje: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                AlTenerError?.Invoke($"Error crítico al iniciar servidor: {ex.Message}");
                AlTenerDetenerse?.Invoke();
            }
        }

        /// <summary>
        /// Puedes extender esta lógica para manejar comandos o respuestas.
        /// </summary>
        private void ProcesarMensaje(string mensaje, IPEndPoint cliente)
        {
            try
            {
                // Intentamos deserializar como JSON
                using JsonDocument doc = JsonDocument.Parse(mensaje);
                JsonElement root = doc.RootElement;

                string tipo = root.GetProperty("Tipo").GetString() ?? "";

                if (tipo == "Registro")
                {
                    string nombre = root.GetProperty("Nombre").GetString() ?? "";

                    if (nombresRegistrados.Contains(nombre))
                    {
                        EnviarMensaje("REGISTRO_DUPLICADO", cliente);
                    }
                    else
                    {
                        nombresRegistrados.Add(nombre);
                        EnviarMensaje("REGISTRO_OK", cliente);
                        AlNuevoCliente?.Invoke(cliente);
                    }
                }
                else if (tipo == "Respuesta")
                {
                    if (!respuestasHabilitadas)
                    {
                        EnviarMensaje("RESPUESTA_RECHAZADA|Debes esperar antes de responder.", cliente);
                        return;
                    }

                    string nombre = root.GetProperty("Nombre").GetString() ?? "";
                    string respuesta = root.GetProperty("Respuesta").GetString() ?? "";

                    if (!nombresRegistrados.Contains(nombre))
                    {
                        EnviarMensaje("RESPUESTA_RECHAZADA|No estás registrado.", cliente);
                        return;
                    }

                    AlRecibirMensaje?.Invoke($"[RESPUESTA] {nombre}: {respuesta}");
                }
            }
            catch (JsonException)
            {
                AlTenerError?.Invoke("❌ Mensaje recibido no es JSON válido.");
            }
            catch (Exception ex)
            {
                AlTenerError?.Invoke($"Error al procesar mensaje: {ex.Message}");
            }
        }


        public void EnviarPregunta( PreguntaModel pregunta)
        {
            Task.Run(async () =>
            {
                respuestasHabilitadas = false;

                string texto =  string.Join("|",pregunta.Opciones);
                var datos = Encoding.UTF8.GetBytes(texto);
                foreach (var c in ClientesRegistrados)
                    servidorUdp.Send(datos, datos.Length, c);
                string mensaje = string.Join("|", pregunta.Opciones);
                EnviarBroadcast(mensaje);

                AlRecibirMensaje?.Invoke("[SERVIDOR] Pregunta enviada. Esperando...");

                await Task.Delay(5000); // Espera 5 segundos antes de permitir respuestas

                respuestasHabilitadas = true;
                EnviarBroadcast("RESPUESTAS_HABILITADAS");

                AlRecibirMensaje?.Invoke("[SERVIDOR] Ya se puede responder.");
            });
        }



        /// <summary>
        /// Enviar mensaje UDP al cliente especificado.
        /// </summary>
        public void EnviarMensaje(string mensaje, IPEndPoint destino)
        {
            if (servidorUdp == null) return;

            byte[] datos = Encoding.UTF8.GetBytes(mensaje);
            servidorUdp.Send(datos, datos.Length, destino);
        }

        /// <summary>
        /// Enviar mensaje a todos los clientes por broadcast.
        /// </summary>
        public void EnviarBroadcast(string mensaje)
        {
            if (servidorUdp == null) return;

            byte[] datos = Encoding.UTF8.GetBytes(mensaje);
            servidorUdp.Send(datos, datos.Length, new IPEndPoint(IPAddress.Broadcast, puerto));
        }
    }
}