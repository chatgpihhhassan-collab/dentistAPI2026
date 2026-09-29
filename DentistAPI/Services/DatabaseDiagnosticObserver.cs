using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace DentistAPI.Services
{
    public class DatabaseDiagnosticObserver : IObserver<DiagnosticListener>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "SqlClientDiagnosticListener")
            {
                listener.Subscribe(new SqlClientCommandObserver());
            }
        }
    }

    public class SqlClientCommandObserver : IObserver<KeyValuePair<string, object?>>
    {
        private class CommandContext
        {
            public DateTime StartTime { get; set; } = DateTime.UtcNow;
            public string DatabaseName { get; set; } = "Unknown";
            public string ServerName { get; set; } = "Unknown";
            public string CommandText { get; set; } = string.Empty;
            public string Parameters { get; set; } = string.Empty;
        }

        private static readonly ConcurrentDictionary<Guid, CommandContext> _activeCommands = new();

        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> pair)
        {
            try
            {
                string eventName = pair.Key;
                object? payload = pair.Value;
                if (payload == null) return;

                if (eventName.EndsWith("WriteCommandBefore", StringComparison.OrdinalIgnoreCase))
                {
                    HandleCommandBefore(payload);
                }
                else if (eventName.EndsWith("WriteCommandAfter", StringComparison.OrdinalIgnoreCase))
                {
                    HandleCommandAfter(payload);
                }
                else if (eventName.EndsWith("WriteCommandError", StringComparison.OrdinalIgnoreCase))
                {
                    HandleCommandError(payload);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SqlClientCommandObserver Error]: {ex.Message}");
            }
        }

        private void HandleCommandBefore(object payload)
        {
            var opId = GetPropertyValue<Guid>(payload, "OperationId");
            var cmd = GetPropertyValue<DbCommand>(payload, "Command");

            if (opId == Guid.Empty || cmd == null) return;

            string dbName = cmd.Connection?.Database ?? "DentistDB";
            string server = cmd.Connection?.DataSource ?? "Local";
            string cmdText = cmd.CommandText ?? string.Empty;

            var paramSb = new StringBuilder();
            if (cmd.Parameters != null && cmd.Parameters.Count > 0)
            {
                foreach (DbParameter p in cmd.Parameters)
                {
                    paramSb.AppendLine($"{p.ParameterName} = {(p.Value == null || p.Value == DBNull.Value ? "NULL" : $"'{p.Value}'")}");
                }
            }

            _activeCommands[opId] = new CommandContext
            {
                StartTime = DateTime.UtcNow,
                DatabaseName = dbName,
                ServerName = server,
                CommandText = cmdText,
                Parameters = paramSb.ToString()
            };
        }

        private void HandleCommandAfter(object payload)
        {
            var opId = GetPropertyValue<Guid>(payload, "OperationId");
            var cmd = GetPropertyValue<DbCommand>(payload, "Command");

            _activeCommands.TryRemove(opId, out var context);

            DateTime endTime = DateTime.UtcNow;
            DateTime startTime = context?.StartTime ?? endTime;
            string dbName = context?.DatabaseName ?? cmd?.Connection?.Database ?? "DentistDB";
            string server = context?.ServerName ?? cmd?.Connection?.DataSource ?? "Local";
            string cmdText = context?.CommandText ?? cmd?.CommandText ?? string.Empty;
            string parameters = context?.Parameters ?? string.Empty;

            DbCallLogger.LogDbCall(
                databaseName: dbName,
                serverName: server,
                sqlQuery: cmdText,
                parameters: parameters,
                startTime: startTime,
                endTime: endTime,
                isSuccess: true,
                resultSummary: "Command executed successfully and returned results."
            );
        }

        private void HandleCommandError(object payload)
        {
            var opId = GetPropertyValue<Guid>(payload, "OperationId");
            var cmd = GetPropertyValue<DbCommand>(payload, "Command");
            var ex = GetPropertyValue<Exception>(payload, "Exception");

            _activeCommands.TryRemove(opId, out var context);

            DateTime endTime = DateTime.UtcNow;
            DateTime startTime = context?.StartTime ?? endTime;
            string dbName = context?.DatabaseName ?? cmd?.Connection?.Database ?? "DentistDB";
            string server = context?.ServerName ?? cmd?.Connection?.DataSource ?? "Local";
            string cmdText = context?.CommandText ?? cmd?.CommandText ?? string.Empty;
            string parameters = context?.Parameters ?? string.Empty;

            DbCallLogger.LogDbCall(
                databaseName: dbName,
                serverName: server,
                sqlQuery: cmdText,
                parameters: parameters,
                startTime: startTime,
                endTime: endTime,
                isSuccess: false,
                errorMessage: ex?.Message ?? "Unknown SQL execution error"
            );
        }

        private static T? GetPropertyValue<T>(object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null) return default;
            var val = prop.GetValue(obj);
            if (val is T typedVal) return typedVal;
            return default;
        }
    }
}
