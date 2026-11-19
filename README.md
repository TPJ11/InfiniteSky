NOTE: This readme is not too helpful in setting up the server anymore.
Someone needs to update it.

[Documentation](https://rawgit.com/LiamKarlMitchell/InfiniteSky/InfiniteSky_FruitSalad/FruitSalad/Documentation/index.html)

InfiniteSky
=========

## .NET 10 foundation

The repository now includes the approved .NET 10 solution foundation alongside the unchanged Node.js implementation.

The current C# host provides:

- the approved modular project and dependency structure;
- a Generic Host composition root;
- validated, strongly typed configuration;
- structured startup/shutdown logging and foundation readiness;
- login authentication and an opt-in MongoDB account repository;
- a single-world lifecycle, readiness boundary, and optional MongoDB guild bootstrap;
- test projects and developer build/test/format scripts;
- GitHub Actions build, test, and formatting verification.

The world lifecycle is not full world readiness: no zone provider or world packet/session-entry path is registered, so world health remains degraded and zone/gameplay simulation does not run. The server does **not** yet implement complete legacy game protocol behavior, character selection/creation, game data loading, zone management, or gameplay. Those remain governed by the feature parity matrix and migration plan.

Prerequisites: .NET SDK 10.0.401 or a later compatible 10.0.x feature band.

From the repository root:

```powershell
dotnet restore InfiniteSky.sln
dotnet build InfiniteSky.sln --no-restore
dotnet test InfiniteSky.sln --no-restore
dotnet format InfiniteSky.sln --verify-no-changes --no-restore
dotnet run --project src\InfiniteSky.Server\InfiniteSky.Server.csproj --no-build
```

PowerShell helpers are available under `scripts/`. See [FOUNDATION_IMPLEMENTATION.md](docs/architecture/FOUNDATION_IMPLEMENTATION.md) and [MIGRATION_STATUS.md](MIGRATION_STATUS.md) for scope and progress.

Lifecycle, DI, configuration, diagnostics, background-service, and health conventions are documented in [`docs/architecture/`](docs/architecture/).

The server exits with code `0` after normal shutdown and code `1` on fatal startup/runtime failure. Networking and MongoDB remain disabled by default. When MongoDB is enabled, the server verifies its connection and account index before starting listeners.

GitHub Actions validates the .NET foundation on both Ubuntu and Windows. No Linux-only service wrapper or Windows Service is installed at this stage; the portable Generic Host is the deployment foundation.

To enable loopback TCP transport for development:

```powershell
$env:INFINITESKY_InfiniteSky__Networking__Enabled = "true"
$env:INFINITESKY_InfiniteSky__Networking__BindAddress = "127.0.0.1"
$env:INFINITESKY_InfiniteSky__Networking__LoginPort = "11091"
$env:INFINITESKY_InfiniteSky__Networking__WorldPort = "11092"
dotnet run --project src\InfiniteSky.Server\InfiniteSky.Server.csproj
```

The listeners accept raw TCP connections and the login endpoint has an authentication packet path. Character selection, world entry, most packet handlers, and gameplay remain incomplete. See [NETWORKING_IMPLEMENTATION.md](docs/migration/NETWORKING_IMPLEMENTATION.md) and [AUTHENTICATION_IMPLEMENTATION.md](docs/migration/AUTHENTICATION_IMPLEMENTATION.md).

### World lifecycle and guild bootstrap

The server initializes a single process-local world before starting network listeners. `InfiniteSky:World:TickMilliseconds` defaults to the legacy 100 ms cadence. With MongoDB enabled, after MongoDB's host startup checks pass, world startup loads the configured guild collection; a guild-query failure or the not-yet-implemented zone runtime is reported as degraded world readiness. A MongoDB connection/startup failure prevents host startup. This is lifecycle/bootstrap infrastructure only: it does not load zones, create players, process world packets, or run gameplay. See [WORLD_MANAGEMENT_IMPLEMENTATION.md](docs/migration/WORLD_MANAGEMENT_IMPLEMENTATION.md), [WORLD_LIFECYCLE.md](docs/migration/WORLD_LIFECYCLE.md), [WORLD_CONFIGURATION.md](docs/migration/WORLD_CONFIGURATION.md), and [WORLD_INTEGRATION_BOUNDARIES.md](docs/migration/WORLD_INTEGRATION_BOUNDARIES.md).

### Configure MongoDB account persistence

MongoDB is disabled unless explicitly enabled. For a local development database with no authentication:

```powershell
$env:INFINITESKY_InfiniteSky__Database__Enabled = "true"
$env:INFINITESKY_InfiniteSky__Database__ConnectionString = "mongodb://127.0.0.1:27017/ts1"
$env:INFINITESKY_InfiniteSky__Database__DatabaseName = "ts1"
$env:INFINITESKY_InfiniteSky__Database__AccountCollectionName = "account_mongooses"
dotnet run --project src\InfiniteSky.Server\InfiniteSky.Server.csproj
```

The collection name is inferred from the legacy Mongoose model and must be checked against the target database before use. The original server resets `active` across all accounts at startup; the C# option `ResetActiveAccountsOnStartup` defaults to true to preserve that behavior. Review it carefully for multi-instance deployments. Never commit authenticated connection strings; use protected environment configuration or a secret manager.

See [MONGODB_PERSISTENCE_IMPLEMENTATION.md](docs/migration/MONGODB_PERSISTENCE_IMPLEMENTATION.md), [MONGODB_SCHEMA_MAPPING.md](docs/migration/MONGODB_SCHEMA_MAPPING.md), and [DATABASE_CONFIGURATION.md](docs/migration/DATABASE_CONFIGURATION.md) for scope, BSON decisions, configuration, and the opt-in isolated integration test.

The legacy Node.js code and its installation instructions remain below for reference.

[![Join the chat at https://gitter.im/LiamKarlMitchell/InfiniteSky](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/LiamKarlMitchell/InfiniteSky?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

InfiniteSky is an open-source project, based on public functionality of Twelve Sky. It is written in Node.JS

  - We have reloadable code. This means less need to reset the server when developing.
  - Currently the world server part is being re-intergrated into the code base.
  - Zone features are being implemented.
  - Code will under go cleaning and documentation..

### Donate with PayPal today.
[
![Donate with PayPal Credit or any major credit card.](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)
](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=AV9L6EV4S95GW)

Donations will be used to further improve the project or for server expenses.

Version
----

1.1.00 - Fruit Salad

Tech
-----------

InfiniteSky uses a number of open source projects to work properly:

* [hexy] - hexdump, binary pretty-printing
* [async] - Higher-order functions and common patterns for asynchronous code
* [jshint] - Static analysis tool for JavaScript
* [socket] - Socket is a connect clone for simple socket based applicationsad
* [netmask] - Parse and lookup IP network blocks
* [node.js] - evented I/O for the backend
* [mongodb] - A NoSQL DB that is awesome
* [mongoose] - Mongoose MongoDB ODM
* [vmscript] - Scripts that reload when changed and run in a node vm [@LiamKarlMitchell]
* [socket.io] - a super fast port of Markdown to JavaScript apps
* [restruct.js] A JavaScript binary data library. [@rfw]

If you need a runtime debuger, I would suggest node-inspector
Example usage: npm install node-inspector -g
node-debug main

Installation
--------------

Please Note: These Instructions are very out of date.
Contact us for more information on how to develop on this project.

[Video Guide](http://youtu.be/WH32P1i5DrQ)
I will make another video when I can as this one has poor quality.

To setup with git check see [Git Setup](https://github.com/LiamKarlMitchell/InfiniteSky/blob/master/docs/gitsetup.md)

## Install Node.js & MongoDB
*If you have not already*
Currently using Node.js v0.10.26

Don't use the one from apt-get on linux, I have had experiences with it being quite out of date.

See the following links:
* [Node.JS](http://nodejs.org)
* [MongoDB](http://www.mongodb.org)

Some assumptions are that you know how to do basic file management and use the command prompt or terminal. As well as download and install files.
We suggest setting up your code somewhere like c:\node\projects and check out Infinite Sky using a GIT client.

Server is currently untested on linux. I have been running it on Windows 7
```
sh
git clone [git-repo-url] InfiniteSky
cd InfiniteSky
npm install
mkdir data
mkdir data/infos
mkdir data/packets
mkdir data/spawninfo
mkdir data/world
```

or maybe ```mkdir -p data/world data/infos data/packets data/spawninfo data/world```

To setup mongodb what I did was create a data directory in the mongo db directory. Then I make a bat script like this
```
title Mongodb
cls
bin\mongod.exe --dbpath "data"
```

or you could use sh script like this on linux
```
./bin/mongod --dbpath "./data"
```

By running that script you can easily start the database server for InfiniteSky.

The legacy Node.js and MongoDB setup instructions below are historical and are not the C# deployment procedure. Do not reuse the sample database credentials from old configuration files. For the C# server, create/authorize a database user through your deployment's normal secure process and supply its URI via protected configuration; see [DATABASE_CONFIGURATION.md](docs/migration/DATABASE_CONFIGURATION.md).

## Get files to data directorys you made

for more information on these see [Directory Documentation](https://github.com/LiamKarlMitchell/InfiniteSky/blob/master/docs/directorys.md)

## Configure. Instructions in
copy the config.json-dist file and rename it to config.json and edit it accordingly

```cp config.json-dist config.json```

For more info see [Config Documentation](https://github.com/LiamKarlMitchell/InfiniteSky/blob/master/docs/config.md)

Replace the following in the config
"PUT YOUR INTERNAL IP HERE" with your ip address you can find it by doing ipconfig and looking at your interfaces ip.
"PUT YOUR NETWORK MASK HERE" Your nemask in cider notation. For example "10.1.1.0/24" is 255.255.255.0
"PUT YOUR EXTERNAL IP HERE" Get your external ip by googling what is my ip and putting the result in here. This is required for other people to connect to your server. Otherwise use your internal IP if running on lan.

## Configure Plugins. Instructions in following README.md files

* plugins/dropbox/README.md
* plugins/github/README.md
* plugins/googledrive/README.md

```sh
node app
```

## Documentation
Main Documentation in docs/index.html
[Restruct Documentation](https://github.com/LiamKarlMitchell/InfiniteSky/blob/master/docs/gitsetup.md)
[Scripting Documentation](https://github.com/LiamKarlMitchell/InfiniteSky/blob/master/docs/scripting.md)

## Create account
You will want to create some game accounts too use.
To make a GM account use
````
/createaccount Name Password 100
````

The 100 us the level.

Try also /help for other commands.
If you want information on game info try infos.Item[1].Name or other attributes.

## Notes

The server will take a while to start, be patient.

## Style Guide
We should style our code like so [Idiomatic.js Style Guide](https://github.com/rwldrn/idiomatic.js),
there are places where we may not have written code this way in existing code. I recommend you fix these in existing code or in the case of a pull request tell the author to fix it up.

##Recommended sublime plug-ins:
Coming Soon


License
----

GNU GPL see LICENSE file

**Enjoy!**
