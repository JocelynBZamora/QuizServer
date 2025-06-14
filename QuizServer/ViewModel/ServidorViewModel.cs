using QuizServer.Service;
using QuizServer.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using QuizServer.Controls;
using QuizServer;
using System.Text.Json;
using System.IO;
using System.Net;
using System.Linq;

namespace ServidorUDP.ViewModels
{
    public class ServidorViewModel : INotifyPropertyChanged
    {
        private  ServidorUdpService? servidor;
        private int indicePregunta = 0;
        private TimerControl? timerControl;
        private string respuestaCorrectaActual = "";
        private bool puedeResponder = false;
        private bool registrado = false;

        // Estado del servidor
        public ObservableCollection<string> MensajesRecibidos { get; set; } = new();
        public string Estado { get => estado; set { estado = value; OnPropertyChanged(); } }
        private string estado = "Servidor detenido";
        public string IP { get => ip; set { ip = value; OnPropertyChanged(); } }
        private string ip = "0.0.0.0";
        public ObservableCollection<UsuarioModel> UsuariosConectados { get; set; } = new();

        // Preguntas y respuestas para mostrar en UI
        public string PreguntaActual { get => preguntaActual; set { preguntaActual = value; OnPropertyChanged(); } }
        private string preguntaActual = "";
        public ObservableCollection<string> Opciones { get; set; } = new();
        public string ResultadosPreguntaActual { get => resultadosPreguntaActual; set { resultadosPreguntaActual = value; OnPropertyChanged(); } }
        private string resultadosPreguntaActual = "";

        // Comandos
        public ICommand IniciarCommand { get; }
        public ICommand DetenerCommand { get; }
        public ICommand IniciarCuestionarioCommand { get; }

        // Lista de preguntas (puedes cargar desde archivo o base de datos)
        private PreguntaModel[] preguntas = Array.Empty<PreguntaModel>();

        private void CargarPreguntasDesdeJson()
        {
            try
            {
                string ruta = @"..\..\..\Resources\preguntas.json";

                if (File.Exists(ruta))
                {
                    string json = File.ReadAllText(ruta);
                    preguntas = JsonSerializer.Deserialize<PreguntaModel[]>(json) ?? Array.Empty<PreguntaModel>();
                }
                else
                {
                    MensajesRecibidos.Add("[ERROR] No se encontró el archivo de preguntas.");
                }
            }
            catch (Exception ex)
            {
                MensajesRecibidos.Add($"[ERROR] Al leer JSON: {ex.Message}");
            }
        }

        public ServidorViewModel()
        {


            IniciarCommand = new RelayCommand(IniciarServidor);
            DetenerCommand = new RelayCommand(DetenerServidor);
            IniciarCuestionarioCommand = new RelayCommand(IniciarCuestionario, () => servidor != null);

            CargarPreguntasDesdeJson();
        }

        public void SetTimerControl(TimerControl control)
        {
            timerControl = control;
            timerControl.TimerCompleted += TimerControl_TimerCompleted;
        }

        private void TimerControl_TimerCompleted(object? sender, EventArgs e)
        {
            IniciarCuestionario();
        }

        private void IniciarServidor()
        {
            if (servidor != null)
                return;

            servidor = new ServidorUdpService();
            IP = servidor.IP;
            servidor.AlNuevoCliente += (cliente) =>
            {
                // Mostrar información detallada del cliente
                string mensaje = $"[INFO] Nuevo cliente conectado:\n" +
                               $"  IP: {cliente.Address}\n" +
                               $"  Puerto: {cliente.Port}\n" +
                               $"  Dirección completa: {cliente}";
                MensajesRecibidos.Add(mensaje);
            };

            servidor.AlIniciar += () => Estado = "Servidor en ejecución";
            servidor.AlTenerDetenerse += () => Estado = "Servidor detenido";
            servidor.AlRecibirMensaje += ProcesarMensajeCliente;

            servidor.Iniciar();
            OnCanExecuteChanged();
        }

        private void ProcesarMensajeCliente(string mensaje)
        {
            try
            {
                var paquete = JsonSerializer.Deserialize<dynamic>(mensaje);
                string tipo = paquete.GetProperty("Tipo").GetString();

                switch (tipo)
                {
                    case "Registro":
                        ProcesarRegistro(paquete);
                        break;
                    case "Respuesta":
                        ProcesarRespuesta(paquete);
                        break;
                }
            }
            catch (Exception ex)
            {
                MensajesRecibidos.Add($"[ERROR] Error al procesar mensaje: {ex.Message}");
            }
        }

        private void ProcesarRegistro(JsonElement paquete)
        {
            string nombre = paquete.GetProperty("Nombre").GetString() ?? "";
            string ip = paquete.GetProperty("IP").GetString() ?? "";
            int puerto = paquete.GetProperty("Puerto").GetInt32();

            var endPoint = new IPEndPoint(IPAddress.Parse(ip), puerto);
            var usuario = new UsuarioModel
            {
                Nombre = nombre,
                EndPoint = endPoint
            };

            App.Current.Dispatcher.Invoke(() =>
            {
                if (!UsuariosConectados.Any(u => u.Nombre == nombre))
                {
                    UsuariosConectados.Add(usuario);
                    string mensaje = $"[INFO] Usuario registrado:\n" +
                                   $"  Nombre: {nombre}\n" +
                                   $"  IP: {ip}\n" +
                                   $"  Puerto: {puerto}";
                    MensajesRecibidos.Add(mensaje);
                }
            });
        }

        private void ProcesarRespuesta(JsonElement paquete)
        {
            string nombre = paquete.GetProperty("Nombre").GetString() ?? "";
            string respuesta = paquete.GetProperty("Respuesta").GetString() ?? "";

            var usuario = UsuariosConectados.FirstOrDefault(u => u.Nombre == nombre);
            if (usuario != null)
            {
                bool esCorrecta = respuesta == respuestaCorrectaActual;
                if (esCorrecta)
                {
                    usuario.Puntuacion += 10;
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    ResultadosPreguntaActual += $"{nombre} ({usuario.EndPoint.Address}): {(esCorrecta ? "Correcto" : "Incorrecto")} - Puntuación: {usuario.Puntuacion}\n";
                });
            }
        }

        private void DetenerServidor()
        {
            if (servidor == null) return;

            servidor.Detener();
            servidor = null;
            Estado = "Servidor detenido";
            PreguntaActual = "";
            Opciones.Clear();
            UsuariosConectados.Clear();
            ResultadosPreguntaActual = "";
            timerControl?.StopTimer();
            OnCanExecuteChanged();
        }

        private void IniciarCuestionario()
        {
            // Si es la primera vez que se inicia el cuestionario
            if (PreguntaActual == "")
            {
                indicePregunta = 0;
            }
            else
            {
                // Avanzar a la siguiente pregunta
                indicePregunta++;
                if (indicePregunta >= preguntas.Length)
                {
                    // Si llegamos al final, detener el timer
                    timerControl?.StopTimer();
                    return;
                }
            }
            MostrarPregunta(indicePregunta);
        }

        private void MostrarPregunta(int indice)
        {
            if (indice < 0 || indice >= preguntas.Length) return;

            var p = preguntas[indice];
            PreguntaActual = p.Texto;
            ResultadosPreguntaActual = "";

            Opciones.Clear();
            foreach (var op in p.Opciones)
                Opciones.Add(op);
            respuestaCorrectaActual = p.RespuestaCorrecta;

            // Enviar JSON con pregunta, opciones y respuesta correcta
            if (servidor != null)
            {
                var paquete = new
                {
                    Tipo = "Pregunta",
                    Texto = p.Texto,
                    Opciones = p.Opciones,
                    RespuestaCorrecta = p.RespuestaCorrecta
                };
                ServidorViewModelAccessor.RespuestaCorrectaActual = p.RespuestaCorrecta;

                string mensajeJson = JsonSerializer.Serialize(paquete);
                servidor.EnviarBroadcast(mensajeJson);
            }
        }

        // Para actualizar CanExecute de los comandos
        private void OnCanExecuteChanged()
        {
            (IniciarCuestionarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public static class ServidorViewModelAccessor
    {
        public static string RespuestaCorrectaActual { get; set; } = "";
    }

    public class RelayCommand : ICommand
    {
        private readonly Action ejecutar;
        private readonly Func<bool>? puedeEjecutar;

        public RelayCommand(Action ejecutar, Func<bool>? puedeEjecutar = null)
        {
            this.ejecutar = ejecutar;
            this.puedeEjecutar = puedeEjecutar;
        }

        public bool CanExecute(object? parameter) => puedeEjecutar == null || puedeEjecutar();
        public void Execute(object? parameter) => ejecutar();

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

