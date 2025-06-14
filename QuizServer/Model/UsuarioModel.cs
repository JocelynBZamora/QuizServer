using System;
using System.Net;

namespace QuizServer.Model
{
    public class UsuarioModel
    {
        public string Nombre { get; set; } = "";
        public IPEndPoint EndPoint { get; set; } = null!;
        public int Puntuacion { get; set; } = 0;
    }
} 