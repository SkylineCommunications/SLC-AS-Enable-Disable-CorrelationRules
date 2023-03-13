/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

dd/mm/2023	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

namespace AutomationTest_1
{
	using System;
	using System.IO;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Correlation;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	///     DataMiner Script Class.
	/// </summary>
	public class Script
	{
		/// <summary>
		///     The Script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(Engine engine)
		{
			// Retrieve script input parameters
			Input input = GetInput(engine);

			// Retrieve correlation rule
			CorrelationRuleMeta correlationRule = GetCorrelationRule(engine, input);

			if (correlationRule.IsEnabled == input.Enable)
			{
				var msg = $"Correlation rule {input.Name} already has the desired status: IsEnabled = {input.Enable}";
				engine.GenerateInformation(msg);
				engine.ExitSuccess(msg);
			}

			// Send update
			UpdateCorrelationRule(engine, input, correlationRule);
		}

		private static CorrelationRuleMeta GetCorrelationRule(Engine engine, Input input)
		{
			DMSMessage correlationRuleMessage = engine.SendSLNetSingleResponseMessage(new GetAvailableCorrelationRulesMessage());
			if (correlationRuleMessage == null)
			{
				throw new InvalidOperationException("No correlation rules found");
			}

			var availableRules = correlationRuleMessage as AvailableCorrelationRulesResponse;
			if (availableRules == null)
			{
				throw new InvalidCastException($"Received response was not of type {typeof(AvailableCorrelationRulesResponse).FullName}");
			}

			CorrelationRuleMeta correlationRule = availableRules.Rules.FirstOrDefault(rule => rule.Name.Equals(input.Name, StringComparison.InvariantCultureIgnoreCase));
			if (correlationRule == null)
			{
				throw new FileNotFoundException($"No correlation rule found on the system with name: {input.Name}");
			}

			return correlationRule;
		}

		private static void UpdateCorrelationRule(Engine engine, Input input, CorrelationRuleMeta correlationRule)
		{
			engine.SendSLNetSingleResponseMessage(
				new UpdateCorrelationRuleMessage
				{
					RuleDefinition = new CorrelationRuleDefinition
					{
						ID = correlationRule.ID,
					},
					Type = input.Enable ? UpdateCorrelationRuleMessage.UpdateType.Enable : UpdateCorrelationRuleMessage.UpdateType.Disable,
				});

			engine.GenerateInformation($"Correlation rule {input.Name} has been {(input.Enable ? "ENABLED" : "DISABLED")}");
		}

		private Input GetInput(Engine engine)
		{
			string ruleName = engine.GetScriptParam("Name").Value;
			string enableRaw = engine.GetScriptParam("Status(Enable/Disable)").Value;

			if (!enableRaw.Equals("Disable") && !enableRaw.Equals("Enable"))
			{
				throw new InvalidOperationException("Expected the value 'Enable' or 'Disable' as input to the Status parameter.");
			}

			return new Input
			{
				Enable = enableRaw.Equals("Enable"),
				Name = ruleName,
			};
		}
	}

	public sealed class Input
	{
		public bool Enable { get; set; }

		public string Name { get; set; }
	}
}