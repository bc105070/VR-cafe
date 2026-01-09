using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CSV writer for experiment results.
/// Writes ONE row per participant into a single CSV file.
/// Quest path: /sdcard/Documents
/// </summary>
public static class CsvWriter
{
    // ====== Public configuration ======
    public static string FileName = "experiment_data.csv";

    // Quest / SideQuest 可見路徑
    public static string QuestDocumentsPath = "/sdcard/Documents";

    // ====== Public state ======
    public static bool HeaderWritten = false;

    [System.Serializable]
    public class ParticipantData
    {
        public int participantID;
        public int condition; // 1..4
    }

    /// <summary>
    /// Load participant data from Participants.csv in StreamingAssets
    /// </summary>
    public static List<ParticipantData> LoadParticipants(string fileName = "Participants.csv")
    {
        List<ParticipantData> participants = new List<ParticipantData>();
        string csvPath = Path.Combine(Application.streamingAssetsPath, fileName);
        string csvContent = "";

        if (csvPath.Contains("://") || csvPath.Contains("jar:"))
        {
            // Android / Quest - Note: This is synchronous, for simplicity. In production, consider async.
            Debug.LogWarning("Loading CSV from web request synchronously. Consider making this async.");
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(csvPath))
            {
                www.SendWebRequest();
                while (!www.isDone) { } // Wait synchronously - not ideal for production
                if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load {fileName}: {www.error}");
                    return participants;
                }
                csvContent = www.downloadHandler.text;
            }
        }
        else
        {
            // PC / Standalone
            if (File.Exists(csvPath))
            {
                csvContent = File.ReadAllText(csvPath);
            }
            else
            {
                Debug.LogError($"{fileName} not found at {csvPath}");
                return participants;
            }
        }

        if (string.IsNullOrEmpty(csvContent))
        {
            Debug.LogError("CSV content is empty");
            return participants;
        }

        string[] lines = csvContent.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // skip header
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');
            if (cols.Length < 2) continue;

            ParticipantData data = new ParticipantData();

            if (int.TryParse(cols[0].Trim(), out data.participantID) &&
                int.TryParse(cols[1].Trim(), out data.condition))
            {
                participants.Add(data);
            }
        }

        Debug.Log($"Loaded {participants.Count} participant records from {fileName}");
        return participants;
    }

    /// <summary>
    /// Write ONE participant row into CSV
    /// </summary>
    public static void WriteParticipantRow(ExperimentSession session)
    {
        if (session == null)
        {
            Debug.LogError("[CsvWriter] ExperimentSession is null.");
            return;
        }

        string basePath;

#if UNITY_ANDROID && !UNITY_EDITOR
        basePath = QuestDocumentsPath;
#else
        // Editor / PC 測試用
        basePath = Application.persistentDataPath;
#endif

        // 確保資料夾存在
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        string fullPath = Path.Combine(basePath, FileName);

        // 如果檔案不存在 → 寫 header
        if (!File.Exists(fullPath))
        {
            WriteHeader(fullPath);
            HeaderWritten = true;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append(Escape(session.participantId)).Append(",");
        sb.Append(Escape(session.condition)).Append(",");
        sb.Append(Escape(session.orderChoice)).Append(",");
        sb.Append(Escape(session.q1Choice)).Append(",");
        sb.Append(Escape(session.q2Choice)).Append(",");
        sb.Append(Escape(session.q3Choice)).Append(",");
        sb.Append(Escape(session.q4Choice)).Append(",");
        sb.Append(Escape(session.q5Choice));

        File.AppendAllText(fullPath, sb.ToString() + "\n", Encoding.UTF8);

        Debug.Log($"[CsvWriter] CSV saved: {fullPath}");
    }

    /// <summary>
    /// Legacy method for compatibility - append session row
    /// </summary>
    public static void AppendSessionRow(ExperimentSession s, StateManagement stateManager, string fallbackFileName = "experiment_data.csv")
    {
        // For compatibility, just call WriteParticipantRow
        WriteParticipantRow(s);
    }

    /// <summary>
    /// Write CSV header
    /// </summary>
    public static void WriteHeader(string fullPath)
    {
        string header = "participantId,condition,Order,q1,q2,q3,q4,q5";
        File.WriteAllText(fullPath, header + "\n", Encoding.UTF8);
        Debug.Log("[CsvWriter] CSV header written.");
    }

    /// <summary>
    /// Escape CSV values safely
    /// </summary>
    public static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }
}
