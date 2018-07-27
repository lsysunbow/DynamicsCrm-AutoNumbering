﻿// this file was generated by the xRM Test Framework VS Extension

#region Imports

using System;
using System.Linq;
using LinkDev.Libraries.Common;
using Microsoft.Xrm.Sdk;
using static LinkDev.Libraries.Common.CrmHelpers;

#endregion

namespace LinkDev.AutoNumbering.Plugins.AutoNumber.Target.Preval
{
	/// <summary>
	/// DEPRECATED
	/// </summary>
	public class CreateTargetUseBacklog : IPlugin
	{
		private readonly string config;

		public CreateTargetUseBacklog(string unsecureConfig, string secureConfig)
		{
			if (string.IsNullOrEmpty(unsecureConfig))
			{
				throw new InvalidPluginExecutionException(
					"Plugin config is empty. Please enter the config record ID or inline config first.");
			}

			config = unsecureConfig;
		}

		public void Execute(IServiceProvider serviceProvider)
		{
			////var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
			new CreateTargetLogic(config).Execute(this, serviceProvider, PluginUser.System);
		}
	}

	internal class CreateTargetLogic : PluginLogic<CreateTargetUseBacklog>
	{
		private readonly string config;

		public CreateTargetLogic(string unsecureConfig) : base("Create", PluginStage.PreValidation)
		{
			config = unsecureConfig;
		}

		protected override bool IsContextValid()
		{
			if (context.MessageName != "Create")
			{
				throw new InvalidPluginExecutionException(
					$"Step registered on wrong message: {context.MessageName},"
						+ $"expected: Create.");
			}

			if (!context.InputParameters.Contains("Target"))
			{
				throw new InvalidPluginExecutionException($"Context is missing input parameter: Target.");
			}

			return true;
		}

		protected override void ExecuteLogic()
		{
			// get the triggering record
			var target = (Entity)context.InputParameters["Target"];

			if (log.MaxLogLevel >= LogLevel.Debug)
			{
				LogAttributeValues(target.Attributes, target, log, "Target Attributes");
			}

			var autoNumberConfig =
				(from autoNumberQ in new XrmServiceContext(service).AutoNumberingSet
				 where autoNumberQ.UniqueID == config
					 && autoNumberQ.Status == AutoNumbering.StatusEnum.Active
				 select new AutoNumbering
						{
							Id = autoNumberQ.Id,
							Name = autoNumberQ.Name,
							FormatString = autoNumberQ.FormatString,
							UseBacklog = autoNumberQ.UseBacklog
						}).FirstOrDefault();

			if (autoNumberConfig == null)
			{
				throw new InvalidPluginExecutionException($"Couldn't find an active auto-numbering configuration"
					+ $" with ID '{config}'.");
			}

			if (autoNumberConfig.FormatString.Contains("{index}") && autoNumberConfig.UseBacklog == true)
			{
				var triggerId = Guid.NewGuid().ToString();

				log.Log($"Updating config with trigger ID '{triggerId}' ...");
				service.Update(
					new AutoNumbering
					{
						Id = autoNumberConfig.Id,
						TriggerID = triggerId
					});
				log.Log($"Finished updating config with trigger ID.");

				context.SharedVariables["AutoNumberingTriggerId"] = triggerId;
				log.Log($"Added trigger ID to shared variables.");
			}
		}
	}
}
