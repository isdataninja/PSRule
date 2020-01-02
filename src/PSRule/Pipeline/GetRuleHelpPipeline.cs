﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PSRule.Configuration;
using PSRule.Host;
using PSRule.Resources;
using PSRule.Rules;
using System;
using System.Diagnostics;
using System.Management.Automation;

namespace PSRule.Pipeline
{
    public interface IHelpPipelineBuilder : IPipelineBuilder
    {
        void Full();

        void Online();
    }

    internal sealed class GetRuleHelpPipelineBuilder : PipelineBuilderBase, IHelpPipelineBuilder
    {
        private bool _Full;
        private bool _Online;

        internal GetRuleHelpPipelineBuilder(Source[] source)
            : base(source) { }

        public override IPipelineBuilder Configure(PSRuleOption option)
        {
            if (option == null)
                return this;

            Option.Execution.LanguageMode = option.Execution.LanguageMode ?? ExecutionOption.Default.LanguageMode;
            Option.Output.Culture = GetCulture(option.Output.Culture);

            if (option.Rule != null)
                Option.Rule = new RuleOption(option.Rule);

            ConfigureLogger(Option);
            return this;
        }
        public void Full()
        {
            _Full = true;
        }

        public void Online()
        {
            _Online = true;
        }
        
        public override IPipeline Build()
        {
            return new GetRuleHelpPipeline(PrepareContext(null, null, null), Source, PrepareReader(), PrepareWriter());
        }

        private sealed class HelpWriter : PipelineWriter
        {
            private const string OUTPUT_TYPENAME_FULL = "PSRule.Rules.RuleHelpInfo+Full";
            private const string OUTPUT_TYPENAME_COLLECTION = "PSRule.Rules.RuleHelpInfo+Collection";

            private readonly PipelineLogger _Logger;
            private readonly LanguageMode _LanguageMode;
            private readonly bool _InSession;
            private readonly bool _ShouldOutput;
            private readonly string _TypeName;

            internal HelpWriter(WriteOutput output, LanguageMode languageMode, bool inSession, PipelineLogger logger, bool online, bool full)
                : base(output)
            {
                _Logger = logger;
                _LanguageMode = languageMode;
                _InSession = inSession;
                _ShouldOutput = !online;
                _TypeName = full ? OUTPUT_TYPENAME_FULL : null;
            }

            public override void Write(object o, bool enumerate)
            {
                if (!(o is RuleHelpInfo[] result))
                {
                    base.Write(o, enumerate);
                    return;
                }
                if (result.Length == 1)
                {
                    if (_ShouldOutput || !TryLaunchBrowser(result[0].GetOnlineHelpUri()))
                        WriteHelpInfo(result[0], _TypeName);

                    return;
                }

                for (var i = 0; i < result.Length; i++)
                    WriteHelpInfo(result[i], OUTPUT_TYPENAME_COLLECTION);
            }

            private bool TryLaunchBrowser(Uri uri)
            {
                return uri == null || TryProcess(uri.OriginalString) || TryConstrained(uri.OriginalString);
            }

            private bool TryConstrained(string uri)
            {
                _Logger.WriteObject(string.Format(PSRuleResources.LaunchBrowser, uri), false);
                return true;
            }

            private bool TryProcess(string uri)
            {
                if (_LanguageMode == LanguageMode.ConstrainedLanguage || _InSession)
                    return false;

                var browser = new Process();
                browser.StartInfo.FileName = uri;
                browser.StartInfo.UseShellExecute = true;
                return browser.Start();
            }

            private void WriteHelpInfo(object o, string typeName)
            {
                if (typeName == null)
                {
                    base.Write(o, false);
                    return;
                }
                var pso = PSObject.AsPSObject(o);
                pso.TypeNames.Insert(0, typeName);
                base.Write(pso, false);
            }
        }

        protected override PipelineWriter PrepareWriter()
        {
            return new HelpWriter(
                GetOutput(),
                Option.Execution.LanguageMode.GetValueOrDefault(ExecutionOption.Default.LanguageMode.Value),
                HostContext.InSession,
                Logger,
                _Online,
                _Full
            );
        }
    }

    internal sealed class GetRuleHelpPipeline : RulePipeline, IPipeline
    {
        internal GetRuleHelpPipeline(PipelineContext context, Source[] source, PipelineReader reader, PipelineWriter writer)
            : base(context, source, reader, writer)
        {
            // Do nothing
        }

        public override void End()
        {
            Writer.Write(HostHelper.GetRuleHelp(source: Source, context: Context), true);
        }
    }
}
