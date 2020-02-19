<h1 align="center">Jellyfin Playback Reporting Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.org/">Jellyfin Project</a></h3>

## About

The Jellyfin Playback Reporting plugin enables the collection and display of user / media activity on your server.
This information can be viewed as a multitude of different graphs, and can also be queried straight from the Jellyfin database.

## Build & Installation Process

1. Clone this repository
2. Ensure you have .NET Core SDK setup and installed
3. Build the plugin with following command:

```
dotnet publish --configuration Release --output bin
```

4. Place the resulting `Jellyfin.Plugin.PlaybackReporting.dll` file in a folder called `plugins/` inside your Jellyfin installation / data directory.

### Screenshot

<img src=screenshot.png>
