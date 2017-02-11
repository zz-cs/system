﻿using System;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using ThinkingHome.Core.Plugins;

namespace ThinkingHome.Core.Infrastructure
{
    public class HomeApplication
    {
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private IServiceContext context;

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="config"></param>
        public void Init(HomeConfiguration config)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddNLog();

            try
            {
                LoadPlugins(config);

                // инициализируем плагины
                foreach (var plugin in context.GetAllPlugins())
                {
                    var pluginType = plugin.GetType();
                    logger.Info($"init plugin: {pluginType.FullName}");

                    var pluginConfig = config.GetPluginSection(pluginType);
                    var pluginLogger = loggerFactory.CreateLogger(pluginType);

                    plugin.Logger = pluginLogger;
                    plugin.InitPlugin(pluginConfig);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger.Error(ex, "error on plugins initialization");
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    logger.Error(loaderException, loaderException.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "error on plugins initialization");
                throw;
            }
        }

        public void StartServices()
        {
            try
            {
                foreach (var plugin in context.GetAllPlugins())
                {
                    logger.Info($"start plugin {plugin.GetType().FullName}");
                    plugin.StartPlugin();
                }

                logger.Info("all plugins are started");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "error on start plugins");
                throw;
            }
        }

        public void StopServices()
        {
            foreach (var plugin in context.GetAllPlugins())
            {
                try
                {
                    logger.Info($"stop plugin {plugin.GetType().FullName}");
                    plugin.StopPlugin();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "error on stop plugins");
                }
            }

            logger.Info("all plugins are stopped");
        }

        #region private

        private void LoadPlugins(HomeConfiguration config)
        {
            var asms = config.GetDependencies().ToArray();

            var conventions = new ConventionBuilder();
            conventions
                .ForTypesDerivedFrom<PluginBase>()
                .Export<PluginBase>()
                .Shared();

            var container = new ContainerConfiguration()
                .WithAssemblies(asms, conventions)
                .WithAssembly(GetType().GetTypeInfo().Assembly, conventions)
                .CreateContainer();

            context = container.GetExport<IServiceContext>("DCCEE19A-2CEA-423F-BFE5-AE5E12679938");
        }

        #endregion
    }
}