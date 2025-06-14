using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizServer.Model
{
    public class PreguntaModel
    {
        public string Texto { get; set; } = null!;
        public string[] Opciones { get; set; } = null!;
        public string RespuestaCorrecta { get; set; } = null!;
    }
}

