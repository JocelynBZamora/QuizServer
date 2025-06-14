using ClienteQuiz.Model;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ClienteQuiz.Services
{
    public class ClienteUdpService
    {
        private readonly string servidorIp;
        private readonly int puerto;

        public ClienteUdpService(string ip, int puerto = 65000)
        {
            this.servidorIp = ip;
            this.puerto = puerto;
        }

        public string? EnviarRespuesta(RespuestaClienteModel respuesta)
        {
            try
            {
                using UdpClient cliente = new();
                var datos = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(respuesta));
                var endpoint = new IPEndPoint(IPAddress.Parse(servidorIp), puerto);

                cliente.Send(datos, datos.Length, endpoint);

                // Intentar recibir respuesta del servidor (opcional)
                cliente.Client.ReceiveTimeout = 3000;
                var remoto = new IPEndPoint(IPAddress.Any, 0);
                byte[] respuestaBytes = cliente.Receive(ref remoto);
                return Encoding.UTF8.GetString(respuestaBytes);
            }
            catch (SocketException)
            {
                return "⏱️ El servidor no respondió.";
            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }
    }
}
