using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizClient;
using QuizzCliente.Services;
using System;

namespace QuizzCliente.ViewModels
{
    public partial class ClienteViewModel : ObservableObject
    {
        private QuizzClient? cliente;

        [ObservableProperty]
        private string ipServidor = "127.0.0.1";

        [ObservableProperty]
        private string nombreUsuario = "";

        [ObservableProperty]
        private string mensajeEstado = "Ingrese su nombre y conecte.";

        private bool registrado = false;

        [RelayCommand]
        private void Conectar()
        {
            if (string.IsNullOrWhiteSpace(IpServidor))
            {
                MensajeEstado = "⚠️ Ingresa una IP válida.";
                return;
            }

            cliente = new QuizzClient(IpServidor);
            cliente.AlRecibirMensaje += ProcesarMensaje;
            cliente.AlTenerError += (err) => MensajeEstado = err;
            cliente.IniciarCliente();

            MensajeEstado = $"🔌 Conectado a {IpServidor}. Ahora regístrate.";
        }

        [RelayCommand]
        private void Registrar()
        {
            if (cliente == null)
            {
                MensajeEstado = "⚠️ Conéctate primero.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NombreUsuario))
            {
                MensajeEstado = "⚠️ Escribe un nombre válido.";
                return;
            }

            cliente.EnviarRegistro(NombreUsuario);
            MensajeEstado = $"⌛ Registrando '{NombreUsuario}'...";
        }

        private void ProcesarMensaje(string mensaje)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (mensaje == "REGISTRO_OK")
                {
                    registrado = true;
                    MensajeEstado = $"✅ Registro exitoso. ¡Bienvenido {NombreUsuario}!";
                }
                else if (mensaje == "REGISTRO_DUPLICADO")
                {
                    registrado = false;
                    MensajeEstado = $"❌ El nombre '{NombreUsuario}' ya está en uso.";
                }
                else
                {
                    MensajeEstado = $"📩 {mensaje}";
                }
            });
        }
    }
}
