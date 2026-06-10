using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;
using System.Text.Json;

namespace BusinessManager.Application.Services;

public class SettingService : ISettingService
{
    private readonly ISettingRepository _settingRepository;
    private readonly ILogger<SettingService> _logger;

    public SettingService(ISettingRepository settingRepository, ILogger<SettingService> logger)
    {
        _settingRepository = settingRepository;
        _logger = logger;
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        try
        {
            return await _settingRepository.GetValueAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting with key {Key}", key);
            return null;
        }
    }

    public async Task<T?> GetSettingAsync<T>(string key)
    {
        try
        {
            var value = await GetSettingAsync(key);
            if (string.IsNullOrEmpty(value))
            {
                return default(T);
            }

            return JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting {Key} as type {Type}", key, typeof(T).Name);
            return default(T);
        }
    }

    public async Task SetSettingAsync(string key, string value)
    {
        try
        {
            var setting = await _settingRepository.GetByKeyAsync(key);
            if (setting != null)
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                await _settingRepository.UpdateAsync(setting);
            }
            else
            {
                setting = new Setting
                {
                    Key = key,
                    Value = value,
                    CreatedAt = DateTime.UtcNow
                };
                await _settingRepository.AddAsync(setting);
            }

            _logger.LogInformation("Setting {Key} updated successfully", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key {Key}", key);
            throw;
        }
    }

    public async Task SetSettingAsync<T>(string key, T value)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await SetSettingAsync(key, serializedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key {Key} as type {Type}", key, typeof(T).Name);
            throw;
        }
    }

    public async Task<IEnumerable<Setting>> GetAllSettingsAsync()
    {
        try
        {
            return await _settingRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all settings");
            return Enumerable.Empty<Setting>();
        }
    }

    public async Task<bool> DeleteSettingAsync(string key)
    {
        try
        {
            var setting = await _settingRepository.GetByKeyAsync(key);
            if (setting != null)
            {
                await _settingRepository.DeleteAsync(setting.Id);
                _logger.LogInformation("Setting {Key} deleted successfully", key);
                return true;
            }
            
            _logger.LogWarning("Setting {Key} not found", key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setting {Key}", key);
            return false;
        }
    }
}
