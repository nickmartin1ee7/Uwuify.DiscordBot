using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using Uwuify.DiscordBot.WorkerService.Models;
using Uwuify.DiscordBot.WorkerService.Modules;

namespace Uwuify.DiscordBot.WorkerService.Services
{
    public class EvaluationService
    {
        private readonly ILogger<EvaluationService> _logger;
        private readonly DiscordSettings _settings;

        public EvaluationService(ILogger<EvaluationService> logger, DiscordSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public async Task<string> EvaluateAsync(string script, SocketCommandContext context)
        {
            string result = string.Empty;

            try
            {
                var scriptOptions = ScriptOptions.Default;

                //Add reference to mscorlib
                var mscorlib = typeof(object).Assembly;
                var systemCore = typeof(Enumerable).Assembly;
                scriptOptions = scriptOptions.AddReferences(mscorlib, systemCore);
                //Add namespaces
                scriptOptions = scriptOptions.AddReferences("System");
                scriptOptions = scriptOptions.AddReferences("System.Linq");
                scriptOptions = scriptOptions.AddReferences("System.Text");
                scriptOptions = scriptOptions.AddReferences("System.Text.Json");
                scriptOptions = scriptOptions.AddReferences("System.Collections.Generic");

                result = await CSharpScript.EvaluateAsync<string>(script,
                    scriptOptions,
                    new Globals { Context = context });
            }
            catch (Exception e)
            {
                result = e.Message;
            }

            return result;
        }
    }
}