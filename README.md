# Service SSH avec Port Forwarding

Ce projet est un service Windows qui établit une connexion SSH avec du port forwarding. Il permet de passer les paramètres de connexion soit via des arguments en ligne de commande, soit depuis un fichier de configuration. Le service surveille continuellement la connexion SSH et tente de se reconnecter automatiquement en cas de perte de connexion.

## Fonctionnalités

- Supporte l'authentification SSH via mot de passe ou clé privée.
- Fonctionnalité de redirection de port local (tunnel SSH).
- Paramètres de connexion passés via ligne de commande ou fichier de configuration.
- Reconnexion automatique en cas de perte de connexion SSH.
- Journalisation des activités et des erreurs dans un fichier de log pour faciliter le débogage.

## Table des matières

1. [Installation](#installation)
2. [Utilisation](#utilisation)
3. [Configuration](#configuration)
4. [Journalisation](#journalisation)
5. [Contribuer](#contribuer)
6. [Licence](#licence)

## Installation

### Prérequis

- .NET Framework 4.8 ou plus récent
- Visual Studio 2019 ou plus récent
- Serveur SSH accessible avec les informations d'identification nécessaires

### Étape 1 : Cloner le dépôt

```bash
git clone https://github.com/neruaka/ssh-service-port-forwarding.git
cd ssh-service-port-forwarding
```

### Étape 2 : Compiler le projet

Ouvre le projet dans Visual Studio et compile-le en utilisant la configuration Release.

### Étape 3 : Installer le service

1. Ouvre l'invite de commande du développeur pour Visual Studio en mode Administrateur.
2. Accède au répertoire de sortie de la compilation (par exemple, bin/Release).
3. Exécute la commande suivante pour installer le service :

```bash
sc create SSHService binPath= "C:\chemin-vers-sortie\SSHService.exe"
```

4.Démarre le service :

```bash
net start SSHService
```

Le service sera désormais exécuté automatiquement à chaque démarrage de Windows.

## Utilisation

Passer des arguments en ligne de commande

Le service accepte les détails de la connexion via des arguments en ligne de commande. Voici un exemple pour passer les paramètres :

```bash
SSHService.exe -L 8080:destinationHost:80 -i chemin/vers/clé/privée -P 22 -p motDePasse utilisateur@hôte
```

-L [localPort:destinationHost:destinationPort] : Définit la configuration du port forwarding local.

-i [cheminVersCléPrivée] : Chemin vers la clé privée SSH.

-P [port] : Le port du serveur SSH (par défaut 22).

-p [motDePasse] : Mot de passe SSH.

[utilisateur@hôte] : Nom d'utilisateur et hôte SSH.


## Utiliser un fichier de configuration

Il est aussi possible de stocker les paramètres dans un fichier de configuration et de les passer au service.

Crée un fichier de configuration SSHServiceArgs.txt avec les détails de la connexion, par exemple :

```bash
-L 8080:destinationHost:80 -i chemin/vers/clé/privée -P 22 -p motDePasse utilisateur@hôte
```

Décommente la section du code dans Serv.cs qui lit le fichier de configuration :

```bash
//string configFilePath = "E:\\SSHServiceArgs.txt";
//string fileArgs = File.ReadAllText(configFilePath);
//args = fileArgs.Split(' ');
```

Redémarre le service pour que les modifications prennent effet.

## Configuration

Le service accepte les paramètres suivants en ligne de commande :

-L [localPort:destinationHost:destinationPort] : Spécifie la configuration du port forwarding local.

-i [cheminVersCléPrivée] : Chemin vers la clé privée utilisée pour l'authentification SSH.

-P [port] : Spécifie le port à utiliser pour SSH (par défaut 22).

-p [motDePasse] : Mot de passe pour la connexion SSH.

[utilisateur@hôte] : Utilisateur et hôte SSH.

Ces paramètres peuvent être modifiés via la ligne de commande ou en créant un fichier de configuration.

## Journalisation

Le service enregistre les événements importants, tels que le démarrage/arrêt du service, l'état de la connexion, et les erreurs, dans un fichier de log situé à :

```bash
E:\\SSHService.txt
```

Ce fichier permet de suivre l'exécution du service et d'identifier les problèmes potentiels.

## Contribuer

Si vous souhaitez contribuer à ce projet :

1. Forkez le dépôt.

2. Créez une nouvelle branche (git checkout -b feature-branch).

3. Validez vos modifications (git commit -am 'Ajout d'une fonctionnalité').

4. Poussez votre branche (git push origin feature-branch).

5. Créez une nouvelle Pull Request.

Licence
Ce projet est sous licence MIT - voir le fichier LICENSE pour plus de détails.
