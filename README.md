# MCD2 Custom Skin (it's need a better name lmao)

Permet de convertir des skins Minecraft vers Minecraft Dungeons.

- Affiche un aperçu 3D avant d'exporter
- Génère les portraits de héros pour le menu du jeu (merci gemini car je pu la merde en 3d)
- Écrit les données de mod dans `Content\Paks\~mods` comme ça les fichiers originaux du jeu restent intacts en cas d'update ou autre
- Prend en charge les skins slim de bras

## Prérequis

- Windows 10 ou Windows 11 (64 bits).
- Minecraft Dungeons 2 (Steam, pas de support pour la version Xbox pour maintenant).
- Runtime .NET 8.0

## Utilisation

- Lancez le logiciel et si vous n'avez pas installé le jeu dans le dossier de base de Steam cliquez juste sur Paks Folder et sélectionnez le dossier de Minecraft Dungeons 2 ou son répertoire `Content\Paks`.
- Sélectionnez un héros
- Cliquez sur "Upload Skin" et choisissez n'importe quel fichier de skin Minecraft classique (.png)
- Cliquez sur "Apply Skin to Hero"

Et c'est tout

Pour restaurer le skin par défaut juste cliquez sur "Reset to Vanilla"

## Compilation

Prérequis :
- SDK .NET 8.0

Commandes :

```powershell
dotnet build -c Release

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```
