using System;
using System.ServiceProcess;
using Renci.SshNet;
using System.IO;
using System.Timers; // Ajout du Timer

public class Serv : ServiceBase
{
    // Point d'entrée Main
    static void Main(string[] args)
    {
        ServiceBase[] ServicesToRun;    // Tableau qui contient les services à lancer
        ServicesToRun = new ServiceBase[]
        {
            new Serv()              // Crée un nouveau service que l'on ajoute au tableau
        };
        ServiceBase.Run(ServicesToRun); // Lance les services du tableau
    }

    // Ajout des variables globales pour stocker les paramètres de connexion SSH
    private string host = "";
    private string localPort = "";
    private string keyFileName = "";
    private string destinationHost = "";
    private string destinationPort = "";
    private string username = "";
    string pwd = "";
    private int portSSH = 22;
    private SshClient client;
    private ForwardedPortLocal portFwd;
    private Timer connectionTimer;

    static void ProcessArguments(
        string[] args,
        ref string host,
        ref string keyFileName,
        ref string localPort,
        ref string destinationHost,
        ref string destinationPort,
        ref string username,
        ref int portSSH,
        ref string pwd)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-L":
                    // Gestion du paramètre de port forwarding local
                    if (i + 1 < args.Length)
                    {
                        i++;
                        var portArgs = args[i].Split(':');
                        if (portArgs.Length == 3)
                        {
                            localPort = portArgs[0];
                            destinationHost = portArgs[1];
                            destinationPort = portArgs[2];
                        }
                    }
                    break;

                case "-i":
                    // Gestion du paramètre du fichier de clé privée
                    if (i + 1 < args.Length)
                    {
                        i++;
                        keyFileName = args[i];
                    }
                    break;

                case "-P":
                    // Gestion du paramètre du port SSH
                    if (i + 1 < args.Length)
                    {
                        i++;
                        portSSH = int.Parse(args[i]);
                    }
                    break;

                case "-p":
                    // Gestion du paramètre du mot de passe
                    if (i + 1 < args.Length)
                    {
                        i++;
                        pwd = args[i];
                    }
                    break;

                default:
                    if (!args[i].StartsWith("-"))
                    {
                        // Gestion du paramètre [user@]host
                        var userHost = args[i].Split('@');
                        if (userHost.Length == 2)
                        {
                            username = userHost[0];
                            host = userHost[1];
                        }
                        else
                        {
                            host = userHost[0];
                        }
                    }
                    break;
            }
        }
    }

    // OnStart
    protected override void OnStart(string[] args)
    {
        // Demmarage du service 
        File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Service démarré.\n");

        try
        {
            //Si je souhaite passer les args via le cmd /*admin*/
            string[] SvcArgs = Environment.GetCommandLineArgs();

            //Si je souhaite lire les arguments depuis un fichier de configuration // remplacer les SvcArgs par args
            //string configFilePath = "E:\\SSHServiceArgs.txt";
            //if (!File.Exists(configFilePath))
            //{
            //    File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Le fichier de configuration est introuvable.\n");
            //    this.Stop();
            //    return;
            //}

            //// Lire les arguments depuis le fichier texte
            //string fileArgs = File.ReadAllText(configFilePath);
            //args = fileArgs.Split(' ');

            // Loguer les arguments pris en compte
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Arguments pris en compte :\n");
            foreach (var arg in SvcArgs)
            {
                File.AppendAllText("E:\\SSHService.txt", arg + "\n");
            }

            if (SvcArgs.Length < 6)
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Arguments insuffisants pour démarrer le service.\n");
                this.Stop();
                return;
            }

            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Récupération des arguments...\n");

            ProcessArguments(SvcArgs, ref host, ref keyFileName, ref localPort, ref destinationHost, ref destinationPort, ref username, ref portSSH, ref pwd);

            AuthenticationMethod authenticationMethod;

            if (!string.IsNullOrEmpty(pwd))
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Authentification par mot de passe.\n");
                authenticationMethod = new PasswordAuthenticationMethod(username, pwd);
            }
            else if (!string.IsNullOrEmpty(keyFileName))
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Authentification par clé privée.\n");
                var file = new PrivateKeyFile(keyFileName);
                authenticationMethod = new PrivateKeyAuthenticationMethod(username, file);
            }
            else
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Erreur : ni mot de passe ni clé privée fournis.\n");
                this.Stop();
                return;
            }

            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Tentative de connexion SSH...\n");
            StartSSHConnection(authenticationMethod);

            // Démarrer le Timer pour vérifier la connexion toutes les 10 secondes
            connectionTimer = new Timer(10000); // 10000 ms = 10 secondes
            connectionTimer.Elapsed += CheckConnectionStatus;
            connectionTimer.Start();
        }
        catch (Exception ex)
        {
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Erreur lors du démarrage : {ex.Message}\n");
            this.Stop();
        }
    }

    private void StartSSHConnection(AuthenticationMethod authenticationMethod)
    {
        try
        {
            var connectionInfo = new ConnectionInfo(
                host,
                portSSH,
                username,
                authenticationMethod
            );

            client = new SshClient(connectionInfo);
            client.Connect();

            portFwd = new ForwardedPortLocal(
                "127.0.0.1",
                uint.Parse(localPort),
                destinationHost,
                uint.Parse(destinationPort)
            );
            client.AddForwardedPort(portFwd);
            portFwd.Start();

            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: SSH connecté et port forwarding activé : {localPort} -> {destinationHost}:{destinationPort}.\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Erreur lors de la connexion SSH : {ex.Message}\n");
            this.Stop();
        }
    }

    // Méthode appelée par le Timer pour vérifier l'état de la connexion
    private void CheckConnectionStatus(object sender, ElapsedEventArgs e)
    {
        try
        {
            if (client != null && client.IsConnected && portFwd != null && portFwd.IsStarted)
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: La connexion SSH et le port forwarding sont toujours actifs.\n");
            }
            else
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: La connexion SSH ou le port forwarding a été interrompu. Tentative de reconnexion...\n");
                ReconnectSSH();
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Erreur lors de la vérification de la connexion : {ex.Message}\n");
        }
    }

    // Méthode pour tenter une reconnexion si la connexion est interrompue
    private void ReconnectSSH()
    {
        try
        {
            if (client != null && !client.IsConnected)
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Reconnexion SSH en cours...\n");
                client.Connect();
            }

            if (portFwd != null && !portFwd.IsStarted)
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Redémarrage du port forwarding...\n");
                portFwd.Start();
            }

            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Reconnexion réussie.\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Erreur lors de la tentative de reconnexion : {ex.Message}\n");
        }
    }

    protected override void OnStop()
    {
        if (portFwd != null && portFwd.IsStarted)
        {
            portFwd.Stop();
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Port forwarding arrêté.\n");
        }

        if (client != null && client.IsConnected)
        {
            client.Disconnect();
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Connexion SSH fermée.\n");
        }

        if (connectionTimer != null)
        {
            connectionTimer.Stop();
            connectionTimer.Dispose();
        }

        File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Service arrêté.\n");
    }
}
