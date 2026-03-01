using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace LevelUpChoices {
    internal static class Log {
        private static ManualLogSource s_logSource;

        internal static void Init(ManualLogSource logSource) {
            s_logSource = logSource;
        }

        internal static void Debug(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => s_logSource.LogDebug(FormatLog(data, filePath, memberName));
        internal static void Error(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => s_logSource.LogError(FormatLog(data, filePath, memberName));
        internal static void Fatal(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => s_logSource.LogFatal(FormatLog(data, filePath, memberName));
        internal static void Info(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => s_logSource.LogInfo(FormatLog(data, filePath, memberName));
        internal static void Message(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => s_logSource.LogMessage(FormatLog(data, filePath, memberName));
        internal static void Warning(object data, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "") => s_logSource.LogWarning(FormatLog(data, filePath, memberName));

        private static string FormatLog(object data, string filePath, string memberName) {
            string className = Path.GetFileNameWithoutExtension(filePath);
            string dataStr = data?.ToString() ?? "null";

            // Clean up potentially duplicate prefixes manually added before like "Integrations.Init():" or "Integrations.Init() - "
            string prefixToRemove = $"{className}.{memberName}()";

            if (dataStr.StartsWith(prefixToRemove)) {
                dataStr = dataStr[prefixToRemove.Length..].TrimStart(':', '-', ' ');
            }
            else if (dataStr.StartsWith($"{className}.{memberName}")) {
                dataStr = dataStr[$"{className}.{memberName}".Length..].TrimStart('(', ')', ':', '-', ' ');
            }

            return $"{className}.{memberName}() - {dataStr}";
        }
    }
}
