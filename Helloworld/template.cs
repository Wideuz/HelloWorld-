using Microsoft.Extensions.Logging;
using Sharp.Shared;
using System;

namespace NewModule
{
    public sealed class NewModule : IModSharpModule
    {
        public string DisplayName => "NewModule";
        public string DisplayAuthor => "YourName";

        private readonly ILogger<NewModule> _logger;
        private readonly ISharedSystem _sharedSystem;

        public NewModule(ISharedSystem sharedSystem,
                      string? dllPath,
                      string? sharpPath,
                      Version? version,
                      Microsoft.Extensions.Configuration.IConfiguration? coreConfiguration,
                      bool hotReload = false)
        {
            ArgumentNullException.ThrowIfNull(dllPath);
            ArgumentNullException.ThrowIfNull(sharpPath);
            ArgumentNullException.ThrowIfNull(version);
            ArgumentNullException.ThrowIfNull(coreConfiguration);
            _sharedSystem = sharedSystem ?? throw new ArgumentNullException(nameof(sharedSystem));
            _logger = _sharedSystem.GetLoggerFactory().CreateLogger<NewModule>();
        }

        public bool Init()
        {
            _logger.LogInformation("NewModule initializing");
            return true;
        }

        public void PostInit()
        {
            _logger.LogInformation("NewModule post-initialized");
        }

        public void Shutdown()
        {
            _logger.LogInformation("NewModule shutting down");
        }
        public void OnAllModulesLoaded()
        {
            _logger.LogInformation("All Modules Loaded!!!");
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
}