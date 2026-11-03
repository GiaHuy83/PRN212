using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClothingShop.DAL.Models;

namespace ClothingShop.DAL
{
    public static class ConfigDAO
    {
        private static List<string> GetCandidateConfigPaths()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var paths = new List<string>
            {
                // 1. Thư mục thực thi hiện tại (bin\Debug\net9.0-windows\appsettings.json)
                Path.Combine(baseDir, "appsettings.json"),
                // 2. Thư mục mã nguồn của project (ProjectDir\appsettings.json)
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "appsettings.json")),
                // 3. Thư mục gốc solution
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "appsettings.json"))
            };

            return paths.Distinct().ToList();
        }

        public static AppConfig LoadConfig()
        {
            var candidatePaths = GetCandidateConfigPaths();

            // Tìm file có thời gian chỉnh sửa mới nhất
            string? newestFile = null;
            DateTime newestTime = DateTime.MinValue;

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    var writeTime = File.GetLastWriteTimeUtc(path);
                    if (writeTime > newestTime)
                    {
                        newestTime = writeTime;
                        newestFile = path;
                    }
                }
            }

            if (!string.IsNullOrEmpty(newestFile))
            {
                try
                {
                    var json = File.ReadAllText(newestFile);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (config != null)
                    {
                        // Đồng bộ file này sang thư mục runtime nếu cần
                        var runtimePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                        if (!string.Equals(newestFile, runtimePath, StringComparison.OrdinalIgnoreCase))
                        {
                            try { File.WriteAllText(runtimePath, json); } catch { }
                        }
                        return config;
                    }
                }
                catch
                {
                    // Lỗi đọc json -> dùng mặc định
                }
            }

            var defaultConfig = new AppConfig();
            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        public static bool SaveConfig(AppConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                var candidatePaths = GetCandidateConfigPaths();

                bool atLeastOneSaved = false;

                foreach (var path in candidatePaths)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            File.WriteAllText(path, json);
                            atLeastOneSaved = true;
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi ghi ở từng path
                    }
                }

                return atLeastOneSaved;
            }
            catch
            {
                return false;
            }
        }
    }
}
