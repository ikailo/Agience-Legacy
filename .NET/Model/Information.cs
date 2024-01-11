using Agience.Model;
using System.Text.Json.Serialization;

namespace Agience.Client.MQTT.Model
{/*
    public enum InformationState
    {
        DRAFT = 0,
        OPEN = 1,
        CLOSED = 2
    }*/

    public class Information //: IComparable<Information>
    {
        /*
        [JsonIgnore]
        public Agent? Agent { get; set; }

        [JsonIgnore]
        public Template? Template { get; set; }
        */

        public string Id { get; }
        //public string CreatorId { get; }
        //public string? WorkerId { get; set; } // TODO: Rename to ???        
        //public InformationState InformationState { get; internal set; }
        //public TemplateState TemplateState { get; private set; }

        public Data? Input { get; private set; }
        public Data? Prompt { get; private set; }
        public Data? Output { get; private set; }
        public string? TemplateId { get; set; }        

        // TODO: History, Signatures, ReadOnly fields ?

        public Information(Data? input = null, Data? prompt = null, Data? output = null)
            : this(input, prompt, null, output) { }

        public Information(Data? input = null, Data? prompt = null, string? templateId = null, Data? output = null)
        {
            Id = MQTT.Id.Create("<fixme>"); // FIXME
            Input = input;
            Prompt = prompt;
            Output = output;
            TemplateId = templateId;
        }
        /*
        [JsonConstructor]
        public Information(string id, string creatorId, string? workerId, string templateId, InformationState informationState,
                            TemplateState templateState, Data? input = null, Data? output = null)
        {
            Id = id;
            CreatorId = creatorId;
            WorkerId = workerId;
            Template = new Template() { Id = templateId };
            InformationState = informationState;
            TemplateState = templateState;
            Input = input;
            Output = output;
        }

        public Information(Agent agent, string templateId, Data? input = null)
            : this(MQTT.Id.Create(agent.Id), agent.Id, null, templateId, InformationState.OPEN, TemplateState.RESTING, input, null)
        {
            Agent = agent;
            //WorkerId = Agent.Instance?.Catalog.GetTemplate(TemplateId).InstanceId;
        }

        public Information(Agent agent, Data? input = null, Data? instruction = null, Data? output = null)            
        {
            Id = MQTT.Id.Create(agent.Id);
            CreatorId = agent.Id;
            //Template = Agent?.Instance?.Catalog.GetTemplate(TemplateId);
            WorkerId = Template?.InstanceId;
            InformationState = InformationState.OPEN;
            TemplateState = TemplateState.RESTING;
            Input = input;
            Transform = instruction;
            Output = output;
            //WorkerId = Agent.Instance?.Catalog.GetTemplate(TemplateId).InstanceId;
        }

        public int CompareTo(Information? other)
        {
            return ReferenceEquals(other, null) ? 1 : ((Id)Id).CompareTo((Id)other.Id);
        }*/

        /*
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
        */
    }
}
