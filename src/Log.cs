using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace LevelUpChoices
{
    internal static class Log
    {
        private static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void Debug(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => _logSource.LogDebug(FormatLog(data, filePath, memberName));
        internal static void Error(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => _logSource.LogError(FormatLog(data, filePath, memberName));
        internal static void Fatal(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => _logSource.LogFatal(FormatLog(data, filePath, memberName));
        internal static void Info(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => _logSource.LogInfo(FormatLog(data, filePath, memberName));
        internal static void Message(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => _logSource.LogMessage(FormatLog(data, filePath, memberName));
        internal static void Warning(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => _logSource.LogWarning(FormatLog(data, filePath, memberName));

        private static string FormatLog(object data, string filePath, string memberName)
        {
            var className = Path.GetFileNameWithoutExtension(filePath);
            var dataStr = data?.ToString() ?? "null";

            // Clean up potentially duplicate prefixes manually added before like "Integrations.Init():" or "Integrations.Init() - "
            var prefixToRemove = $"{className}.{memberName}()";

            if (dataStr.StartsWith(prefixToRemove))

            {
                dataStr = dataStr[prefixToRemove.Length..].TrimStart(':', '-', ' ');
            }
            else if (dataStr.StartsWith($"{className}.{memberName}"))
            {
                dataStr = dataStr[$"{className}.{memberName}".Length..].TrimStart('(', ')', ':', '-', ' ');
            }

            return $"{className}.{memberName}() - {dataStr}";
        }
    }
}
