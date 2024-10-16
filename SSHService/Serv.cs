using System;
using System.ServiceProcess;
using Renci.SshNet;
using System.IO;
using System.Timers;
using System.Threading.Tasks;

public class Serv : ServiceBase
{
    static void Main(string[] args)
    {
        // Tableau qui contient les services à lancer
        ServiceBase[] ServicesToRun;

        ServicesToRun = new ServiceBase[]
        {
            new Serv() // Crée un nouveau service que l'on ajoute au tableau
        };
        // Lance les services du tableau
        ServiceBase.Run(ServicesToRun);
    }

    // Ajout des variables globales pour stocker les paramètres de connexion SSH
    private string host = "";
    private string localPort = "";
    private string keyFileName = "";
    private string destinationHost = "";
    private string destinationPort = "";
    private string username = "";
    private string pwd = "";
    private int portSSH = 22;
    private SshClient client;
    private ForwardedPortLocal portFwd = null; // Nullable

    private Timer connectionTimer;

    // Méthode pour traiter les arguments passés au service
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
        // Parcours des arguments pour récupérer les options et les assigner aux bonnes variables
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-L":
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
                    if (i + 1 < args.Length)
                    {
                        i++;
                        keyFileName = args[i];
                    }
                    break;

                case "-P":
                    if (i + 1 < args.Length)
                    {
                        i++;
                        portSSH = int.Parse(args[i]);
                    }
                    break;

                case "-p":
                    if (i + 1 < args.Length)
                    {
                        i++;
                        pwd = args[i];
                    }
                    break;

                default:
                    if (!args[i].StartsWith("-"))
                    {
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

    // Méthode OnStart appelée lors du démarrage du service
    protected override async void OnStart(string[] args)
    {
        // Démarrage du service - Log de l'événement
        File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Service démarré.\n");

        try
        {
            //Si je souhaite passer les args via le cmd /*admin*/
            string[] SvcArgs = Environment.GetCommandLineArgs();

            // Log des arguments pris en compte
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Arguments pris en compte :\n");
            foreach (var arg in SvcArgs)
            {
                File.AppendAllText("E:\\SSHService.txt", arg + "\n");
            }

            // Si le nombre d'arguments est insuffisant, arrêt du service
            if (SvcArgs.Length < 6)
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Arguments insuffisants pour démarrer le service.\n");
                this.Stop();
                return;
            }

            // Récupération et traitement des arguments
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Récupération des arguments...\n");
            ProcessArguments(SvcArgs, ref host, ref keyFileName, ref localPort, ref destinationHost, ref destinationPort, ref username, ref portSSH, ref pwd);

            AuthenticationMethod authenticationMethod;

            // Vérification de l'authentification par mot de passe ou clé privée
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

            // Tentative de connexion SSH
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Tentative de connexion SSH...\n");
            await StartSSHConnectionAsync(authenticationMethod);

            // Démarrage du Timer pour vérifier la connexion toutes les 10 secondes
            connectionTimer = new Timer(10000); // Vérifie la connexion toutes les 10 secondes
            connectionTimer.Elapsed += CheckConnectionStatus;
            connectionTimer.Start();
        }
        catch (Exception ex)
        {
            // Log de l'erreur en cas d'échec lors du démarrage
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Erreur lors du démarrage : {ex.Message}\n");
            this.Stop();
        }
    }

    // Méthode pour démarrer la connexion SSH et le port forwarding
    private async Task StartSSHConnectionAsync(AuthenticationMethod authenticationMethod)
    {
        try
        {
            // Configuration des informations de connexion SSH
            var connectionInfo = new ConnectionInfo(
                host,
                portSSH,
                username,
                authenticationMethod
            );

            // Création du client SSH
            client = new SshClient(connectionInfo);
            client.Connect();

            // Si le portFwd est déjà instancié, on le vide
            if (portFwd != null && portFwd.IsStarted)
            {
                portFwd.Stop();
                portFwd.Dispose();
                portFwd = null; // On vide le port forwarding
            }

            // Configuration et démarrage du port forwarding
            portFwd = new ForwardedPortLocal(
                "127.0.0.1",
                uint.Parse(localPort),
                destinationHost,
                uint.Parse(destinationPort)
            );
            client.AddForwardedPort(portFwd);
            portFwd.Start();

            // Log de la connexion réussie et du port forwarding actif
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: SSH connecté et port forwarding activé : {localPort} -> {destinationHost}:{destinationPort}.\n");

            // Boucle pour garder la connexion active et vérifier l'état toutes les 10 secondes
            while (client.IsConnected)
            {
                await Task.Delay(10000);
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: SSH et port forwarding sont toujours actifs.\n");
            }
        }
        catch (Exception ex)
        {
            // Log de l'erreur en cas d'échec de la connexion SSH
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Erreur lors de la connexion SSH : {ex.Message}\n");
            this.Stop();
        }
    }

    // Méthode appelée par le Timer pour vérifier l'état de la connexion
    private void CheckConnectionStatus(object sender, ElapsedEventArgs e)
    {
        try
        {
            // Vérification de l'état du client SSH et du port forwarding
            if (client != null && client.IsConnected && portFwd != null && portFwd.IsStarted)
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: La connexion SSH et le port forwarding sont toujours actifs.\n");
            }
            else
            {
                // Si la connexion est interrompue, tentative de reconnexion
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: La connexion SSH ou le port forwarding a été interrompu. Tentative de reconnexion...\n");
                ReconnectSSH();
            }
        }
        catch (Exception ex)
        {
            // Log en cas d'erreur lors de la vérification de la connexion
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Erreur lors de la vérification de la connexion : {ex.Message}\n");
        }
    }

    // Méthode pour tenter une reconnexion si la connexion est interrompue
    private void ReconnectSSH()
    {
        try
        {
            // Tentative de reconnexion au client SSH
            if (client != null && !client.IsConnected)
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Reconnexion SSH en cours...\n");
                client.Connect();
            }

            // Tentative de redémarrage du port forwarding si nécessaire
            if (portFwd == null || !portFwd.IsStarted)
            {
                File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Redémarrage du port forwarding...\n");

                // Réinstanciation du port forwarding
                portFwd = new ForwardedPortLocal(
                    "127.0.0.1",
                    uint.Parse(localPort),
                    destinationHost,
                    uint.Parse(destinationPort)
                );
                client.AddForwardedPort(portFwd);
                portFwd.Start();
            }

            // Log de la reconnexion réussie
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Reconnexion réussie.\n");
        }
        catch (Exception ex)
        {
            // Log en cas d'échec de la reconnexion
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Erreur lors de la tentative de reconnexion : {ex.Message}\n");
        }
    }

    // Méthode OnStop appelée lors de l'arrêt du service
    protected override void OnStop()
    {
        // Arrêt du port forwarding si actif
        if (portFwd != null && portFwd.IsStarted)
        {
            portFwd.Stop();
            portFwd.Dispose();
            portFwd = null; // Vider le port forwarding
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Port forwarding arrêté.\n");
        }

        // Déconnexion du client SSH si connecté
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
            File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Connexion SSH fermée.\n");
        }

        // Arrêt et libération du Timer
        if (connectionTimer != null)
        {
            connectionTimer.Stop();
            connectionTimer.Dispose();
        }

        // Log de l'arrêt du service
        File.AppendAllText("E:\\SSHService.txt", $"{DateTime.Now}: Service arrêté.\n");
    }
}
