using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using System.Drawing;

namespace RealtimeChatTest
{
    internal class Program
    {
        static FirebaseClient firebase = new FirebaseClient("https://realtimechattest-ff218-default-rtdb.firebaseio.com/");
        static string loggedInUserId;
        static string loggedInUserName;

        static async Task Main(string[] args)
        {
            // Manejar el cierre normal de la aplicación
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true; // Evita que la aplicación se cierre inmediatamente
                await CerrarSesion(); // Cierra la sesión
                Environment.Exit(0); // Cierra la aplicación
            };

            // Manejar el cierre abrupto de la aplicación
            AppDomain.CurrentDomain.ProcessExit += async (sender, e) =>
            {
                await CerrarSesion(); // Cierra la sesión
            };

            WriteColorWithText(ConsoleColor.Cyan, "===== Bienvenido a ELONET =====");

            while (true)
            {
                MostrarMenuPrincipal();
                string opcion = Console.ReadLine();

                switch (opcion)
                {
                    case "1":
                        await CrearUsuario();
                        break;
                    case "2":
                        await IniciarSesion();
                        break;
                    case "3":
                        await CerrarSesion(); // Cierra la sesión al salir
                        WriteColorWithText(ConsoleColor.Yellow, "Saliendo del sistema... ¡Hasta luego!");
                        return;
                    default:
                        WriteColorWithText(ConsoleColor.Red, "Opción no válida, por favor intenta de nuevo.");
                        break;
                }

                if (!string.IsNullOrEmpty(loggedInUserId))
                {
                    await MostrarMenuUsuario();
                }
            }
        }

        static void WriteColorWithText(ConsoleColor color, string msg)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        static void MostrarMenuPrincipal()
        {
            Console.Clear();
            WriteColorWithText(ConsoleColor.Cyan, "===== Bienvenido a ELONET =====");
            WriteColorWithText(ConsoleColor.White, "1. Crear nuevo usuario");
            WriteColorWithText(ConsoleColor.White, "2. Iniciar sesión");
            WriteColorWithText(ConsoleColor.White, "3. Salir");
            WriteColorWithText(ConsoleColor.White, "Elige una opción: ");
        }

        static async Task CrearUsuario()
        {
            Console.Clear();
            WriteColorWithText(ConsoleColor.White, "Ingresa tu nombre de usuario: ");
            string username = Console.ReadLine();

            WriteColorWithText(ConsoleColor.White, "Ingresa una contraseña: ");
            string password = Console.ReadLine();

            string userId = Guid.NewGuid().ToString();
            var nuevoUsuario = new ChatUser { Id = userId, Name = username, Password = password, IsOnline = false }; // Establecer IsOnline en false al crear

            WriteColorWithText(ConsoleColor.Yellow, "Creando usuario...");
            await firebase
                .Child("users")
                .Child(userId)
                .PutAsync(nuevoUsuario);

            WriteColorWithText(ConsoleColor.Green, "¡Usuario creado exitosamente!");
            WriteColorWithText(ConsoleColor.White, $"Tu ID único es: {userId}");
            WriteColorWithText(ConsoleColor.White, "Presiona cualquier tecla para volver al menú...");
            Console.ReadKey();
        }

        static async Task IniciarSesion()
        {
            Console.Clear();
            WriteColorWithText(ConsoleColor.White, "Ingresa tu nombre de usuario: ");
            string username = Console.ReadLine();

            WriteColorWithText(ConsoleColor.White, "Ingresa tu contraseña: ");
            string password = Console.ReadLine();

            WriteColorWithText(ConsoleColor.Yellow, "Verificando credenciales...");
            var usuarios = await firebase
                .Child("users")
                .OnceAsync<ChatUser>();

            var usuario = usuarios.FirstOrDefault(u => u.Object.Name == username && u.Object.Password == password);

            if (usuario != null)
            {
                loggedInUserId = usuario.Object.Id;
                loggedInUserName = usuario.Object.Name;

                // Actualiza el estado a conectado
                await firebase.Child("users").Child(loggedInUserId).Child("IsOnline").PutAsync(true);

                WriteColorWithText(ConsoleColor.Green, "Inicio de sesión exitoso!");
                WriteColorWithText(ConsoleColor.Cyan, $"¡Bienvenido, {loggedInUserName}!");
            }
            else
            {
                WriteColorWithText(ConsoleColor.Red, "Nombre de usuario o contraseña incorrectos.");
            }

            WriteColorWithText(ConsoleColor.White, "Presiona cualquier tecla para continuar...");
            Console.ReadKey();
        }

        static async Task MostrarMenuUsuario()
        {
            while (true)
            {
                Console.Clear();
                WriteColorWithText(ConsoleColor.Cyan, "===== ELONET - Usuarios Disponibles =====");
                await VerUsuariosDisponibles();
                WriteColorWithText(ConsoleColor.White, "1. Conectar con un usuario");
                WriteColorWithText(ConsoleColor.White, "2. Actualizar usuarios disponibles");
                WriteColorWithText(ConsoleColor.White, "3. Volver al menú principal");
                WriteColorWithText(ConsoleColor.White, "Elige una opción: ");

                string opcion = Console.ReadLine();

                switch (opcion)
                {
                    case "1":
                        await ConectarConUsuario();
                        break;
                    case "2":
                        WriteColorWithText(ConsoleColor.Yellow, "Actualizando lista de usuarios...");
                        await VerUsuariosDisponibles();
                        break;
                    case "3":
                        await CerrarSesion(); // Cerrar sesión al volver al menú principal
                        return;
                    default:
                        WriteColorWithText(ConsoleColor.Red, "Opción no válida, por favor intenta de nuevo.");
                        break;
                }
            }
        }

        static async Task VerUsuariosDisponibles()
        {
            var usuarios = await firebase
                .Child("users")
                .OnceAsync<ChatUser>();

            WriteColorWithText(ConsoleColor.White, "Usuarios disponibles conectados:");
            foreach (var user in usuarios)
            {
                if (user.Object.Id != loggedInUserId && user.Object.IsOnline) // Filtrar usuarios no conectados
                {
                    WriteColorWithText(ConsoleColor.Cyan, $"ID: {user.Object.Id} - Nombre: {user.Object.Name}");
                }
            }
        }

        static async Task ConectarConUsuario()
        {
            WriteColorWithText(ConsoleColor.White, "Ingresa el ID del usuario con el que deseas chatear: ");
            string targetUserId = Console.ReadLine();

            WriteColorWithText(ConsoleColor.Yellow, "Buscando usuario...");
            var targetUser = await firebase
                .Child("users")
                .Child(targetUserId)
                .OnceSingleAsync<ChatUser>();

            if (targetUser != null)
            {
                WriteColorWithText(ConsoleColor.Green, $"Conectado con {targetUser.Name}. ¡Puedes empezar a chatear!");
                await ChatConUsuario(targetUserId, targetUser.Name);
            }
            else
            {
                WriteColorWithText(ConsoleColor.Red, "ID de usuario no válido. Intenta de nuevo.");
            }

            WriteColorWithText(ConsoleColor.White, "Presiona cualquier tecla para continuar...");
            Console.ReadKey();
        }

        static async Task ChatConUsuario(string targetUserId, string targetUserName)
        {
            Console.Clear();
            WriteColorWithText(ConsoleColor.Cyan, $"Chat con {targetUserName}. Escribe tu mensaje:");

            firebase
                .Child("messages")
                .AsObservable<ChatMessage>()
                .Where(d => d.Object != null && d.Object.SenderId == targetUserId && d.Object.ReceiverId == loggedInUserId)
                .Subscribe(d =>
                {
                    WriteColorWithText(ConsoleColor.Yellow, $"{d.Object.SenderName}: {d.Object.Message}");
                });

            while (true)
            {
                string message = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(message)) break;

                var chatMessage = new ChatMessage
                {
                    SenderId = loggedInUserId,
                    SenderName = loggedInUserName,
                    ReceiverId = targetUserId,
                    Message = message
                };

                WriteColorWithText(ConsoleColor.Yellow, "Enviando mensaje...");
                await firebase
                    .Child("messages")
                    .PostAsync(chatMessage);

                WriteColorWithText(ConsoleColor.Green, $"Tú: {message}");
            }
        }

        static async Task CerrarSesion()
        {
            if (!string.IsNullOrEmpty(loggedInUserId))
            {
                await firebase.Child("users").Child(loggedInUserId).Child("IsOnline").PutAsync(false);
                loggedInUserId = null;
                loggedInUserName = null;
            }
        }
    }

    // Clases para representar un usuario y mensajes de chat
    public class ChatUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public bool IsOnline { get; set; } // Agregado para indicar el estado de conexión
    }

    public class ChatMessage
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string ReceiverId { get; set; }
        public string Message { get; set; }
    }
}