using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace QuizzCliente.Services
{
    public class QuizzClient
    {
        private readonly string ipServidor;
        private readonly int puerto = 65000;

        private UdpClient? udpClient;
        private Thread? hiloEscucha;
        private IPEndPoint? servidorDestino;

        public event Action<string>? AlRecibirMensaje;
        public event Action<string>? AlTenerError;
        public event Action<string, string[]>? AlRecibirPregunta;


        public QuizzClient(string ipServidor)
        {
            this.ipServidor = ipServidor;
        }

        public void IniciarCliente()
        {
            try
            {
                udpClient = new UdpClient(0);
                servidorDestino = new IPEndPoint(IPAddress.Parse(ipServidor), puerto);

                hiloEscucha = new Thread(EscucharMensajes)
                {
                    IsBackground = true
                };
                hiloEscucha.Start();
            }
            catch (Exception ex)
            {
                AlTenerError?.Invoke($"❌ Error al iniciar cliente: {ex.Message}");
            }
        }

        public void EnviarRegistro(string nombre)
        {
            try
            {
                if (udpClient == null || servidorDestino == null) return;

                var localEP = (IPEndPoint)udpClient.Client.LocalEndPoint!;
                var paquete = new
                {
                    Tipo = "Registro",
                    Nombre = nombre,
                    IP = localEP.Address.ToString(),
                    Puerto = localEP.Port
                };

                string mensajeJson = JsonSerializer.Serialize(paquete);
                byte[] datos = Encoding.UTF8.GetBytes(mensajeJson);

                udpClient.Send(datos, datos.Length, servidorDestino);
            }
            catch (Exception ex)
            {
                AlTenerError?.Invoke($"❌ Error al registrar usuario: {ex.Message}");
            }
        }

        private void EscucharMensajes()
        {
            try
            {
                IPEndPoint remoto = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    byte[] datos = udpClient!.Receive(ref remoto);
                    string mensaje = Encoding.UTF8.GetString(datos);
                    AlRecibirMensaje?.Invoke(mensaje);
                }
            }
            catch (Exception ex)
            {
                AlTenerError?.Invoke($"❌ Error al escuchar: {ex.Message}");
            }
        }

        public void DetenerCliente()
        {
            udpClient?.Close();
            hiloEscucha?.Join(500);
        }
    }
}
