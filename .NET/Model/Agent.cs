using Agience.Model;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static IdentityModel.OidcConstants;
using Timer = System.Timers.Timer;

namespace Agience.Client.MQTT.Model
{
    public class Agent : Agience.Model.Agent
    {
        public event EventHandler<string>? LogMessage;
        public event EventHandler<Message>? MessageReceived;

        public delegate Task PromptCallback(Data? output);
        private ConcurrentDictionary<string, PromptCallback> _promptCallbacks = new();

        public new Agency? Agency { get; private set; }
        public new Instance? Instance { get; private set; }

        private Identity _identity;

        public Agent(Identity identity)
        {
            _identity = identity;
        }


        private async Task Receive(Information? information)
        {
            if (information == null) { return; }

            //information.Agent = this;

            Timeline.Add(information); // TODO? only update if newer

            // Closed and this agent is the creator
            if (information.InformationState == InformationState.CLOSED && information.CreatorId == _identity.AgentId)
            {
                // Invoke the callback.
                if (_promptCallbacks.Remove(information.Id, out PromptCallback? callback))
                {
                    await callback.Invoke(information.Output);
                }

                // Assess the publisher information
                var publisherInformation = Timeline.GetAncestor(information.Id);

                if (publisherInformation == null)
                {
                    // This is a root request. 
                    return;
                }

                information = publisherInformation;

                // Fall through to next if condition so the publisher can be assessed and processed               
            }

            // Open, and this agent is assigned
            if (information.InformationState == InformationState.OPEN && information.WorkerId == _identity.AgentId)
            {
                // TODO: Debounce

                if (await information.Assess())
                {
                    // TODO: Exception Handling
                    await information.Process();
                }
            }

            // Closed, and this agent is not the creator
            if (information.InformationState == InformationState.CLOSED && information.CreatorId != _identity.AgentId)
            {
                // TODO: Review. Add to Context.
            }
        }

        protected internal async Task<bool> Assess()
        {
            if (Agent == null || _assessmentQueued || TemplateState == TemplateState.PROCESSING || InformationState == InformationState.CLOSED) { return false; }

            // Assessments are debounced. Only one assessment can be queued at a time.
            // FIXME: Not Threadsafe

            if (TemplateState == TemplateState.ASSESSING)
            {
                _assessmentQueued = true;

                while (TemplateState == TemplateState.ASSESSING)
                {
                    await Task.Delay(10);
                }
            }

            if (Agent?.Instance?.Catalog.ContainsKey(TemplateId) ?? false)
            {
                TemplateState = TemplateState.ASSESSING;

                var result = await Agent.Instance.Catalog.GetTemplate(TemplateId).Assess(this);

                TemplateState = TemplateState.RESTING;

                _assessmentQueued = false;

                return result;
            }

            _assessmentQueued = false;

            return false;
        }

        protected internal async Task Process()
        {
            if (Agent == null) { return; }

            // Only one process can be in progress at a time. We don't queue up another one.
            // FIXME: Not Threadsafe

            if (TemplateState == TemplateState.RESTING && (Agent?.Instance?.Catalog.ContainsKey(TemplateId) ?? false))
            {
                TemplateState = TemplateState.PROCESSING;

                Output = await Agent.Instance.Catalog.GetTemplate(TemplateId).Process(this);

                InformationState = InformationState.CLOSED;

                TemplateState = TemplateState.RESTING;

                WorkerId = CreatorId;

                if (Agent?.Agency != null)
                {
                    await Agent.Agency.PublishAsync(this, null);
                }
            }
        }

        /*
        public async Task PublishAsync(Agent.OutputCallback? callback, string templateId, Data? input = null)
        {
            if (Agent == null) { return; }

            var information = new Information(Agent, templateId, input);
            Agent.Timeline.Add(information);
            Agent.Timeline.Spawn(information.Id, Id);
            await Agent.Agency.PublishAsync(information, callback);
        }

        
        public async Task<Data?> Publish(string templateId, Data? input = null)
        {
            if (Agent == null) { return null; }

            var information = new Information(Agent, templateId, input);
            Agent.Timeline.Add(information);
            Agent.Timeline.Spawn(information.Id, Id);
            return await Agent.Broker.Publish(information);
        }*/


        // These methods publish input and wait for the output to be returned. Better for short / finite processes.
        public Task<Data?> Prompt(Data? prompt)
        {
            return Prompt(null, prompt, string.Empty);
        }

        public Task<Data?> Prompt(Data? input, Data? prompt)
        {
            return Prompt(input, prompt, string.Empty);
        }

        public async Task<Data?> Prompt(Data? input, Data? prompt, string? templateId)
        {
            bool callbackComplete = false;
            Data? result = null;

            await Prompt(new Information(input, prompt, templateId, null),
                (output) =>
                {
                    result = output;
                    callbackComplete = true;
                    return Task.CompletedTask;
                 });

            // FIXME TODO: This can wait indefinitly if the information is never closed or template doesn't exist. Add timeout / decay / cancellation token.

            while (!callbackComplete)
            {
                await Task.Delay(10);
            }

            return result;
        }

        // Publishing input by itself doesn't require a callback. No output is expected.
        public void Publish(Data? input)
        {
            Prompt(input, null, (PromptCallback?)null);
        }

        // These methods publish input and store a callback to be invoked when the output is returned. Better for long / infinite processes.
        public void Prompt(Data? prompt, PromptCallback? callback)
        {
            Prompt(null, prompt, null, callback);
        }

        public void Prompt(Data? input, Data? prompt, PromptCallback? callback)
        {
            Prompt(input, prompt, null, callback);
        }

        public async void Prompt(Data? input, Data? prompt, string? templateId, PromptCallback? callback)
        {
            await Prompt(new Information(input, prompt, null, templateId), callback);
        }

        public async Task Prompt(Information information, PromptCallback? callback)
        {

            if (information.Output != null)
            {
                // TODO: Reject? This isn't really a prompt.
                // Use another method to publish completed information.
                // Ready to be recorded.  Publish to Creator and/or Agency
                throw new NotImplementedException();
            }

            if (callback != null)
            {
                _promptCallbacks[information.Id] = callback;
            }

            var targetId = string.Empty;

            if (information.TemplateId != null)
            {
                // Ready to Assess. Send to Template's Agent.
                targetId = "<information.Template.AgentId>";
                // Send via Instance Broker
            }

            else if (information.Prompt != null)
            {
                // Find a Template.  If there is none locally, ask the Agency.
                targetId = "<bestTemplate.AgentId";
                // Send via Agency Broker
            }

            else if (information.Input != null)
            {
                // Publish only. No prompt or callback is expected.
                // Add to the context.
                targetId = "<AgencyId>";
                // Send via Agency Broker
            }
            
            // Short circuit
            if (targetId == _identity.AgentId)
            {
                new Task(async () =>
                {
                    await Receive(information);
                }).Start();
                return;
            }

            await Send(information, Agency?.Agents?.Where(agent => agent.Id == targetId).FirstOrDefault());
        }

        private async Task Send(Information information, Agent? agent)
        {

            if (agent == null) { return; }

            throw new NotImplementedException();
        }

        /*
        public async Task WriteLog(string message)
        {
            
            if (_mqtt.IsConnected && Catalog.ContainsKey(LOG_MESSAGE_TEMPLATE_ID) && Catalog[LOG_MESSAGE_TEMPLATE_ID].AgentId != null && Catalog[LOG_MESSAGE_TEMPLATE_ID].AgentId != Id)
            {
                await PublishAsync(LOG_MESSAGE_TEMPLATE_ID, null, $"{Name?.PadRight(21)} | {message}");
            }
            else
            {
                LogMessage?.Invoke(this, message);
            }
        }*/

        // Startup



        /*
        public async void Kill(double delayMs = 0)
        {
            if (delayMs == 0)
            {
                await KillTimerCallback();
            }
            else
            {
                _killTimer = new Timer(delayMs);
                _killTimer.Elapsed += async (sender, e) => await KillTimerCallback();
                _killTimer.Start();
            }
        }

        private async Task KillTimerCallback()
        {
            await _mqtt.DisconnectAsync();
            Environment.Exit(0);
        }*/
    }
}