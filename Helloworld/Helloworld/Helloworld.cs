using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using System;
using System.IO;

namespace Helloworld;

public sealed class Helloworld : IModSharpModule, IEventListener
{
    public string DisplayName => "Noob Hello world";
    public string DisplayAuthor => "YourName";

    private readonly ILogger<Helloworld> _logger;
    private readonly ISharpModuleManager _sharpModuleManager;
    private readonly IEntityManager _entityManager;
    private readonly ISharedSystem _sharedSystem;

    // Keep references so we can unregister on Shutdown
    private bool _serverCommandRegistered;
    private bool _clientCommandInstalled;
    private bool _eventListenerInstalled;

    public delegate void GameClientHandler(IGameClient client);

    public Helloworld(
        ISharedSystem sharedSystem,
        string? dllPath,
        string? sharpPath,
        Version? version,
        IConfiguration? coreConfiguration,
        bool hotReload)
    {
        _sharedSystem = sharedSystem ?? throw new ArgumentNullException(nameof(sharedSystem));
        _logger = _sharedSystem.GetLoggerFactory().CreateLogger<Helloworld>();
        _sharpModuleManager = _sharedSystem.GetSharpModuleManager();
        _entityManager = _sharedSystem.GetEntityManager();

        try
        {
            if (!string.IsNullOrEmpty(dllPath))
            {
                var cfgPath = Path.Combine(dllPath, "appsettings.json");
                if (File.Exists(cfgPath))
                {
                    var localCfg = new ConfigurationBuilder()
                        .AddJsonFile(cfgPath, optional: true, reloadOnChange: false)
                        .Build();
                }
                else
                {
                    _logger.LogDebug("No local appsettings.json at {path}", dllPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read local config");
        }
    }

    public bool Init()
    {
        _logger.LogInformation("HelloWorld 已連線！");
        _logger.LogInformation("HelloWorld Type Assembly: {asm}", this.GetType().Assembly.FullName);
        _logger.LogInformation("IModSharpModule Type Assembly: {asm}", typeof(IModSharpModule).Assembly.FullName);
        return true;
    }

    public void PostInit()
    {
        _logger.LogInformation("HELLOWORLD : HELLO MY FRIEND!");

        // on server console only
        try
        {
            _sharedSystem.GetConVarManager()
                         .CreateServerCommand("ms_echo",
                                              OnServerCommand,
                                              "Echo command from HelloWorld",
                                              ConVarFlags.Release);
            _serverCommandRegistered = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register server command ms_echo");
            _serverCommandRegistered = false;
        }

        // client chat/console
        try
        {
            _sharedSystem.GetClientManager().InstallCommandCallback("hello", OnClientCommand);
            _clientCommandInstalled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install client command callback hello");
            _clientCommandInstalled = false;
        }

        // install event listener (this implements IEventListener)
        try
        {
            _sharedSystem.GetEventManager().InstallEventListener(this);
            _eventListenerInstalled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install event listener");
            _eventListenerInstalled = false;
        }
    }

    public void Shutdown()
    {
        _logger.LogInformation("Helloworld 模組關閉！");

        if (_serverCommandRegistered)
        {
            try
            {
                _sharedSystem.GetConVarManager().ReleaseCommand("ms_echo");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release server command ms_echo");
            }
            _serverCommandRegistered = false;
        }

        if (_clientCommandInstalled)
        {
            try
            {
                _sharedSystem.GetClientManager().RemoveCommandCallback("hello", OnClientCommand);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove client command callback hello");
            }
            _clientCommandInstalled = false;
        }

        if (_eventListenerInstalled)
        {
            try
            {
                _sharedSystem.GetEventManager().RemoveEventListener(this);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove event listener");
            }
            _eventListenerInstalled = false;
        }
    }

    // Server console handler
    private ECommandAction OnServerCommand(StringCommand command)
    {
        _logger.LogInformation("Server command ms_echo triggered: {cmd}", command.GetCommandString());
        Console.WriteLine("ms_echo: " + command.GetCommandString());
        return ECommandAction.Stopped;
    }

    // Client chat/console handler
    private ECommandAction OnClientCommand(IGameClient client, StringCommand command)
    {
        var name = client.Name ?? "error";

        var who = command.ArgCount >= 1 ? command.GetArg(1) ?? name : name;
        client.ConsolePrint($"Hello, {who}");
        _sharedSystem.GetModSharp().PrintChannelFilter(HudPrintChannel.Chat,
                                                       $"Hello, {who}",
                                                       new RecipientFilter(client.Slot));
        _logger.LogInformation("OnClientCommand -> {client}: {command}", client.Name, command.GetCommandString());
        return ECommandAction.Stopped;
    }

    // IEventListener implementation
    public void FireGameEvent(IGameEvent e)
    {
        if (e is IEventPlayerDeath death)
        {
            _logger.LogInformation("{v} was killed by {k}",
                                   death.VictimController?.PlayerName,
                                   death.KillerController?.PlayerName ?? "World");
        }
        else if (e.Name.Equals("player_spawn", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Player slot[{s}] spawned", e.GetInt("userid"));
        }
        else
        {
            _logger.LogDebug("GameEvent {e} fired", e.Name);
        }
    }

    public bool HookFireEvent(IGameEvent e, ref bool serverOnly)
    {
        if (e.Name.Equals("player_say", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Blocked GameEvent fire: {s}", e.Name);
            return false;
        }

        return true;
    }

    int IEventListener.ListenerVersion => IEventListener.ApiVersion;
    int IEventListener.ListenerPriority => 0;

    public void OnAllModulesLoaded()
    {
        _logger.LogInformation("所有模組已載入！");
    }

    public void OnLibraryConnected(string name)
    {
        _logger.LogInformation("Library {name} 已連線！", name);
    }

    public void OnLibraryDisconnect(string name)
    {
        _logger.LogInformation("Library {name} 已斷線！", name);
    }
}