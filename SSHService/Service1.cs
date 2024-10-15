using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

[RunInstaller(true)]
public class Service1 : Installer
{
    private ServiceProcessInstaller serviceProcessInstaller;
    private ServiceInstaller serviceInstaller;

    public Service1()
    {
        // Créer un ServiceProcessInstaller et un ServiceInstaller
        serviceProcessInstaller = new ServiceProcessInstaller();
        serviceInstaller = new ServiceInstaller();

        // Définir le type de compte sous lequel le service fonctionnera
        serviceProcessInstaller.Account = ServiceAccount.LocalSystem;

        // Définir le nom du service, qui apparaîtra dans services.msc
        serviceInstaller.ServiceName = "SSHService";
        serviceInstaller.DisplayName = "Service de Connexion SSH";
        serviceInstaller.StartType = ServiceStartMode.Automatic;

        // Ajouter les installateurs au processus
        Installers.Add(serviceProcessInstaller);
        Installers.Add(serviceInstaller);
    }
}
