using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.NetworkInformation;
using System.Reflection;

namespace Website.Helper
{
    public static class AppSettings
    {
        #region Public properties

        public static string ApiUsername { get; private set; }
        public static string ApiPassword { get; private set; }
        public static string RootFolder { get; private set; }
        public static bool IsConfigured { get; private set; }

        #endregion

        /// <summary>
        /// Configures all properties by using reflection to assign values from an IConfigurationSection
        /// </summary>
        /// <param name="section">IConfigurationSection</param>
        public static void Configure(IConfigurationSection section)
        {
            try
            {
                if (section == null)
                    throw new ArgumentNullException($"{nameof(section)} cannot be null.");

                foreach (PropertyInfo property in typeof(AppSettings).GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    string value = section.GetSection(property.Name).Value;

                    Type baseType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    property.SetValue(null, value == null ? null : Convert.ChangeType(value, baseType));
                }

                IsConfigured = true;
            }
            catch
            {
                IsConfigured = false;
                throw;
            }
        }

        /// <summary>
        /// Configures all properties by using reflection to assign values from an IConfigurationSection
        /// Overwrites the RootFolder property from IWebHostEnvironment WebRootPath
        /// </summary>
        /// <param name="section">IConfigurationSection</param>
        /// <param name="env">IWebHostEnvironment</param>
        public static void Configure(IConfigurationSection section, IWebHostEnvironment env)
        {
            try
            {
                Configure(section);
                RootFolder = env.WebRootPath;
                IsConfigured = true;
            }
            catch
            {
                IsConfigured = false;
                throw;
            }
        }
    }
}
