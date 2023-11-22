using System.Text.Json.Serialization;

namespace Agience
{
    public enum InformationState
    {
        DRAFT = 0,
        OPEN = 1,
        CLOSED = 2
    }

    public class Information : IComparable<Information>
    {
        private bool _assessmentQueued = false;

        [JsonIgnore]
        public Agent? Agent { get; set; }

        public string Id { get; }
        public string CreatorId { get; }
        public string? WorkerId { get; set; }
        public string TemplateId { get; }
        public InformationState InformationState { get; internal set; }
        public TemplateState TemplateState { get; private set; }


        public Data? Input { get; }
        public Data? Output { get; private set; }
        //public Data? Instruction { get; private set; }


        // TODO History, Signatures, ReadOnly fields ?        

        [JsonConstructor]
        public Information(string id, string creatorId, string? workerId, string templateId, InformationState informationState,
                            TemplateState templateState, Data? input = null, Data? output = null)
        {
            Id = id;
            CreatorId = creatorId;
            WorkerId = workerId;
            TemplateId = templateId;
            InformationState = informationState;
            TemplateState = templateState;
            Input = input;
            Output = output;
        }

        public Information(Agent agent, string templateId, Data? input = null)
            : this(Agience.Id.Create(agent.Id), agent.Id, null, templateId, InformationState.OPEN, TemplateState.RESTING, input, null)
        {
            Agent = agent;
            WorkerId = Agent.Catalog[TemplateId].MemberId;
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

            if (Agent.Catalog.ContainsKey(TemplateId))
            {
                TemplateState = TemplateState.ASSESSING;

                var result = await Agent.Catalog[TemplateId].Assess(this);

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

            if (TemplateState == TemplateState.RESTING && Agent.Catalog.ContainsKey(TemplateId))
            {
                TemplateState = TemplateState.PROCESSING;

                Output = await Agent.Catalog[TemplateId].Process(this);

                InformationState = InformationState.CLOSED;

                TemplateState = TemplateState.RESTING;

                WorkerId = CreatorId;

                await Agent.PublishAsync(this, null);
            }
        }

        public async Task PublishAsync(Agent.OutputCallback? callback, string templateId, Data? input = null)
        {
            if (Agent == null) { return; }

            var information = new Information(Agent, templateId, input);
            Agent.Context.Add(information);
            Agent.Context.Spawn(information.Id, Id);
            await Agent.PublishAsync(information, callback);
        }

        public async Task<Data?> Publish(string templateId, Data? input = null)
        {
            if (Agent == null) { return null; }

            var information = new Information(Agent, templateId, input);
            Agent.Context.Add(information);
            Agent.Context.Spawn(information.Id, this.Id);
            return await Agent.Publish(information);
        }

        public int CompareTo(Information? other)
        {
            return object.ReferenceEquals(other, null) ? 1 : ((Id)Id).CompareTo((Id)other.Id);
        }
    }
}
